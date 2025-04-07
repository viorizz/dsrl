using System;
using DSRL.Core.Enums;

namespace DSRL.Core.Configuration
{
    /// <summary>
    /// Application configuration class
    /// </summary>
    [Serializable]
    public class AppConfig
    {
        public DeadzoneShape DefaultDeadzoneShape { get; set; }
        public int DefaultDeadzoneRadius { get; set; }
        public int DefaultLeftTriggerRigidity { get; set; }
        public int DefaultRightTriggerRigidity { get; set; }
        public bool AutoApplySettings { get; set; }
        public bool ShowControllerDebugInfo { get; set; }
        public ControllerProfile[] ControllerProfiles { get; set; }
    }
}