using System;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using HidSharp;

namespace DSRL.Core.Controllers
{
    /// <summary>
    /// Handles continuous reading of input from DualSense controllers
    /// </summary>
    public class ControllerInputReader
    {
        // Input report byte offsets for DualSense controller
        private const int LEFT_STICK_X_OFFSET = 0;
        private const int LEFT_STICK_Y_OFFSET = 1;
        private const int RIGHT_STICK_X_OFFSET = 2;
        private const int RIGHT_STICK_Y_OFFSET = 3;
        private const int LEFT_TRIGGER_OFFSET = 4;
        private const int RIGHT_TRIGGER_OFFSET = 5;

        // Reference to the controller
        private readonly DualSenseController _controller;
        
        // Device variables
        private HidDevice? _hidDevice;
        private HidStream? _hidStream;
        
        // Thread for continuous reading
        private Thread? _readThread;
        private bool _isRunning;
        
        // Last read values
        private Point _leftStickPosition = new Point(0, 0);  // Normalized to -100 to 100 range
        private Point _rightStickPosition = new Point(0, 0); // Normalized to -100 to 100 range
        private int _leftTriggerValue = 0;                   // 0-255 range
        private int _rightTriggerValue = 0;                  // 0-255 range
        
        /// <summary>
        /// Creates a new controller input reader for the specified controller
        /// </summary>
        public ControllerInputReader(DualSenseController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }
        
        /// <summary>
        /// Starts reading input from the controller
        /// </summary>
        // In ControllerInputReader.cs
        public bool Start()
        {
            if (_isRunning) return true;
            
            try
            {
                // Get the device path from the controller info
                var controllerInfo = _controller.GetControllerInfo();
                if (controllerInfo == null) return false;
                
                // Find the device
                var deviceList = DeviceList.Local;
                foreach (var device in deviceList.GetHidDevices())
                {
                    if (device.DevicePath.Contains("vid_054c") && 
                        device.DevicePath.Contains("pid_0ce6"))
                    {
                        _hidDevice = device;
                        break;
                    }
                }
                
                if (_hidDevice == null) return false;
                
                // Open the device for reading with low latency options
                var openParameters = new OpenConfiguration();
                openParameters.SetOption(OpenOption.Priority, OpenPriority.High);
                openParameters.SetOption(OpenOption.Interruptible, false);
                _hidStream = _hidDevice.Open(openParameters);
                
                // Start the reading thread
                _isRunning = true;
                _readThread = new Thread(ReadThreadProc)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };
                _readThread.Start();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting controller input reader: {ex.Message}");
                Stop();
                return false;
            }
        }
        
        /// <summary>
        /// Stops reading input from the controller
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            
            if (_readThread != null && _readThread.IsAlive)
            {
                // Wait for thread to exit (with timeout)
                _readThread.Join(500);
                _readThread = null;
            }
            
            if (_hidStream != null)
            {
                _hidStream.Dispose();
                _hidStream = null;
            }
            
            _hidDevice = null;
        }
        
        /// <summary>
        /// Thread procedure for continuous reading
        /// </summary>
        // In ControllerInputReader.cs, modify the ReadThreadProc method
        private void ReadThreadProc()
        {
            byte[] inputReport = new byte[64];
            
            // Set thread priority to give it more CPU time
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            
            while (_isRunning)
            {
                try
                {
                    if (_hidStream == null || !_hidStream.CanRead)
                    {
                        Thread.Sleep(1); // Shorter sleep when stream not available
                        continue;
                    }
                    
                    // Try to read the report with a very short timeout
                    int bytesRead = _hidStream.Read(inputReport);
                    
                    if (bytesRead > 0)
                    {
                        // Process immediately
                        ProcessInputReport(inputReport);
                        
                        // Update the controller without delay
                        _controller.UpdateInputState(
                            _leftStickPosition,
                            _rightStickPosition,
                            _leftTriggerValue,
                            _rightTriggerValue);
                    }
                    
                    // No sleep here - run as fast as possible
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading input: {ex.Message}");
                    Thread.Sleep(10); // Shorter sleep on error
                }
            }
        }
                
        /// <summary>
        /// Processes an input report from the controller
        /// </summary>
        // In ControllerInputReader.cs, modify the ProcessInputReport method
        // In ControllerInputReader.cs
        private void ProcessInputReport(byte[] report)
        {
            // Log the first 10 bytes of the report to understand structure
            string reportBytes = "";
            for (int i = 0; i < Math.Min(10, report.Length); i++)
            {
                reportBytes += $"{report[i]:X2} ";
            }
            Console.WriteLine($"Report bytes: {reportBytes}");
            // DualSense has different byte offsets from what we initially assumed
            // These values need to be adjusted based on the actual DualSense input report format
            const int LX_OFFSET = 1;  // Adjust these offsets based on actual DualSense report format
            const int LY_OFFSET = 2;
            const int L2_OFFSET = 5;
            const int R2_OFFSET = 6;
            
            // Ensure we have enough data
            if (report.Length < 8) return;
            
            // Get raw values - DualSense uses 0-255 range where 128 is center
            byte rawLX = report[LX_OFFSET];
            byte rawLY = report[LY_OFFSET];
            byte rawL2 = report[L2_OFFSET];
            byte rawR2 = report[R2_OFFSET];
            
            // Print raw values for debugging
            Console.WriteLine($"Raw LX: {rawLX}, LY: {rawLY}, L2: {rawL2}, R2: {rawR2}");
            
            // Convert to -100 to 100 range with proper centering
            int lx = (int)((rawLX - 128) / 127.0 * 100);
            int ly = (int)((rawLY - 128) / 127.0 * 100);
            
            // Ensure dead center when no input
            if (Math.Abs(lx) < 5) lx = 0;
            if (Math.Abs(ly) < 5) ly = 0;
            
            // Invert Y if needed (depends on DualSense mapping)
            ly = -ly;
            
            _leftStickPosition = new Point(lx, ly);
            _leftTriggerValue = rawL2;
            _rightTriggerValue = rawR2;
        }
        public bool IsRunning
        {
            get { return _isRunning; }
        }
    }
}