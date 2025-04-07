using System;
using DSRL.Core.Enums;

namespace DSRL.Core.Configuration
{
    /// <summary>
    /// Controller profile class
    /// </summary>
    [Serializable]
    public class ControllerProfile
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public DeadzoneShape DeadzoneShape { get; set; }
        public int DeadzoneRadius { get; set; }
        public int LeftTriggerRigidity { get; set; }
        public int RightTriggerRigidity { get; set; }
    }
}