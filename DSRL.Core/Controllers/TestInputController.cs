using System;
using System.Drawing;
using System.Threading;

namespace DSRL.Core.Controllers
{
    /// <summary>
    /// Simulates controller input for testing purposes
    /// </summary>
    public class TestInputController
    {
        private readonly DualSenseController _controller;
        private Thread _simulationThread;
        private bool _isRunning;
        
        // Simulation parameters
        private const int SIMULATION_INTERVAL_MS = 50;  // How often to update values (milliseconds)
        private const int JOYSTICK_CYCLE_MS = 4000;     // Time for a full joystick circle (milliseconds)
        private const int TRIGGER_CYCLE_MS = 2000;      // Time for triggers to go up and down (milliseconds)
        
        public TestInputController(DualSenseController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }
        
        /// <summary>
        /// Starts simulating controller input
        /// </summary>
        public bool Start()
        {
            if (_isRunning) return true;
            
            try
            {
                _isRunning = true;
                _simulationThread = new Thread(SimulationThreadProc)
                {
                    IsBackground = true
                };
                _simulationThread.Start();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting test input controller: {ex.Message}");
                Stop();
                return false;
            }
        }
        
        /// <summary>
        /// Stops simulating controller input
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            
            if (_simulationThread != null && _simulationThread.IsAlive)
            {
                _simulationThread.Join(500);
                _simulationThread = null;
            }
        }
        
        /// <summary>
        /// Simulation thread procedure
        /// </summary>
        private void SimulationThreadProc()
        {
            long startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            
            while (_isRunning)
            {
                try
                {
                    // Calculate current time in milliseconds
                    long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    long elapsedTime = currentTime - startTime;
                    
                    // Calculate joystick position - move in a circle
                    double joystickAngle = (elapsedTime % JOYSTICK_CYCLE_MS) * 2 * Math.PI / JOYSTICK_CYCLE_MS;
                    int joystickX = (int)(Math.Cos(joystickAngle) * 80); // -80 to 80 range
                    int joystickY = (int)(Math.Sin(joystickAngle) * 80); // -80 to 80 range
                    
                    // Calculate trigger values - oscillate up and down
                    double triggerPhase = (elapsedTime % TRIGGER_CYCLE_MS) * 2 * Math.PI / TRIGGER_CYCLE_MS;
                    int leftTriggerValue = (int)((Math.Sin(triggerPhase) + 1) / 2 * 255); // 0-255 range
                    int rightTriggerValue = (int)((Math.Sin(triggerPhase + Math.PI) + 1) / 2 * 255); // 0-255 range, opposite phase
                    
                    // Update controller state
                    _controller.UpdateInputState(
                        new Point(joystickX, joystickY),   // Left stick
                        new Point(0, 0),                   // Right stick (not moving)
                        leftTriggerValue,
                        rightTriggerValue);
                    
                    // Small delay
                    Thread.Sleep(SIMULATION_INTERVAL_MS);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in test input simulation: {ex.Message}");
                    Thread.Sleep(500);
                }
            }
        }
    }
}