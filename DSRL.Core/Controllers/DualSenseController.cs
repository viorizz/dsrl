using System;
using System.Drawing;
using DSRL.Core.Enums;

namespace DSRL.Core.Controllers
{
    /// <summary>
    /// Event arguments for controller input changes
    /// </summary>
    public class ControllerInputEventArgs : EventArgs
    {
        public Point LeftStickPosition { get; }
        public Point RightStickPosition { get; }
        public int LeftTriggerValue { get; }
        public int RightTriggerValue { get; }
        
        public ControllerInputEventArgs(
            Point leftStick, 
            Point rightStick, 
            int leftTrigger, 
            int rightTrigger)
        {
            LeftStickPosition = leftStick;
            RightStickPosition = rightStick;
            LeftTriggerValue = leftTrigger;
            RightTriggerValue = rightTrigger;
        }
    }
    
    /// <summary>
    /// Represents a DualSense controller with configurable settings
    /// </summary>
    public class DualSenseController
    {
        public string SerialNumber { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DeadzoneShape DeadzoneShape { get; set; } = DeadzoneShape.Circle;
        public int DeadzoneRadius { get; set; } = 10; // Percentage (0-100)
        public int LeftTriggerRigidity { get; set; } = 50; // Percentage (0-100)
        public int RightTriggerRigidity { get; set; } = 50; // Percentage (0-100)
        
        // Input state properties
        public Point LeftStickPosition { get; private set; } = new Point(0, 0);
        public Point RightStickPosition { get; private set; } = new Point(0, 0);
        public int LeftTriggerValue { get; private set; } = 0;
        public int RightTriggerValue { get; private set; } = 0;
        
        // Input reader
        private ControllerInputReader _inputReader;
        
        // Events
        public event EventHandler<ControllerInputEventArgs> InputChanged;
        
        /// <summary>
        /// Method to apply settings to the actual controller hardware
        /// </summary>
        /// <returns>True if settings were successfully applied</returns>
        public bool ApplySettings()
        {
            try
            {
                // Apply using the extension method from DualSenseAPI
                return this.ApplySettingsToDevice();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying settings: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Starts reading input from the controller
        /// </summary>
        public bool StartInputReading()
        {
            if (_inputReader == null)
            {
                _inputReader = new ControllerInputReader(this);
            }
            
            return _inputReader.Start();
        }
        
        /// <summary>
        /// Stops reading input from the controller
        /// </summary>
        public void StopInputReading()
        {
            _inputReader?.Stop();
        }
        
        /// <summary>
        /// Updates the input state with new values from the reader
        /// </summary>
        internal void UpdateInputState(
            Point leftStick, 
            Point rightStick, 
            int leftTrigger, 
            int rightTrigger)
        {
            // Only raise event if values have changed
            bool hasChanged = 
                LeftStickPosition != leftStick ||
                RightStickPosition != rightStick ||
                LeftTriggerValue != leftTrigger ||
                RightTriggerValue != rightTrigger;
                
            if (hasChanged)
            {
                // Update properties
                LeftStickPosition = leftStick;
                RightStickPosition = rightStick;
                LeftTriggerValue = leftTrigger;
                RightTriggerValue = rightTrigger;
                
                // Raise event
                InputChanged?.Invoke(this, new ControllerInputEventArgs(
                    leftStick, rightStick, leftTrigger, rightTrigger));
            }
        }
    }
}