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
        private HidDevice _hidDevice;
        private HidStream _hidStream;
        
        // Thread for continuous reading
        private Thread _readThread;
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
        public bool Start()
        {
            if (_isRunning) return true; // Already running
            
            try
            {
                // Get the device path from the controller info
                var controllerInfo = _controller.GetControllerInfo();
                if (controllerInfo == null) return false;
                
                // Find the device
                var deviceList = DeviceList.Local;
                foreach (var device in deviceList.GetHidDevices())
                {
                    if (device.DevicePath.Contains("vid_054c") && // Sony VID
                        device.DevicePath.Contains("pid_0ce6") && // DualSense PID
                        device.DevicePath.Contains("mi_03"))      // Interface 3 for input
                    {
                        _hidDevice = device;
                        break;
                    }
                }
                
                if (_hidDevice == null) return false;
                
                // Open the device for reading
                _hidStream = _hidDevice.Open();
                
                // Start the reading thread
                _isRunning = true;
                _readThread = new Thread(ReadThreadProc)
                {
                    IsBackground = true
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
        private void ReadThreadProc()
        {
            byte[] inputReport = new byte[64]; // DualSense input report is typically 64 bytes
            
            while (_isRunning)
            {
                try
                {
                    if (_hidStream == null || !_hidStream.CanRead)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    
                    // Read input report
                    int bytesRead = _hidStream.Read(inputReport);
                    
                    if (bytesRead > 0)
                    {
                        // Process the input report
                        ProcessInputReport(inputReport);
                        
                        // Update the controller state
                        _controller.UpdateInputState(
                            _leftStickPosition,
                            _rightStickPosition,
                            _leftTriggerValue,
                            _rightTriggerValue);
                    }
                    
                    // Small delay to avoid hammering the CPU
                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading input: {ex.Message}");
                    Thread.Sleep(500); // Longer delay after error
                }
            }
        }
        
        /// <summary>
        /// Processes an input report from the controller
        /// </summary>
        private void ProcessInputReport(byte[] report)
        {
            // Ensure we have enough data
            if (report.Length < 6) return;
            
            // Parse stick positions (0-255 range) and convert to -100 to 100 range
            int leftX = report[LEFT_STICK_X_OFFSET];
            int leftY = report[LEFT_STICK_Y_OFFSET];
            int rightX = report[RIGHT_STICK_X_OFFSET];
            int rightY = report[RIGHT_STICK_Y_OFFSET];
            
            // Convert 0-255 range to -100 to 100 range
            _leftStickPosition = new Point(
                (int)((leftX / 255.0 * 200) - 100),
                (int)((leftY / 255.0 * 200) - 100));
                
            _rightStickPosition = new Point(
                (int)((rightX / 255.0 * 200) - 100),
                (int)((rightY / 255.0 * 200) - 100));
                
            // Parse trigger values (0-255 range)
            _leftTriggerValue = report[LEFT_TRIGGER_OFFSET];
            _rightTriggerValue = report[RIGHT_TRIGGER_OFFSET];
        }
    }
}