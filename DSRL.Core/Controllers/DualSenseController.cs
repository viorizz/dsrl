using System;
using DSRL.Core.Enums;

namespace DSRL.Core.Controllers
{
    /// <summary>
    /// Represents a DualSense controller with configurable settings
    /// </summary>
    public class DualSenseController
    {
        public string SerialNumber { get; set; }
        public bool IsConnected { get; set; }
        public DeadzoneShape DeadzoneShape { get; set; } = DeadzoneShape.Circle;
        public int DeadzoneRadius { get; set; } = 10; // Percentage (0-100)
        public int LeftTriggerRigidity { get; set; } = 50; // Percentage (0-100)
        public int RightTriggerRigidity { get; set; } = 50; // Percentage (0-100)
        
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
    }
}