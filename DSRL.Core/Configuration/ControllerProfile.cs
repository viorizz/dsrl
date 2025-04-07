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
        public string SerialNumber { get; set; }
        public string ProfileName { get; set; }
        public DeadzoneShape DeadzoneShape { get; set; }
        public int DeadzoneRadius { get; set; }
        public int LeftTriggerRigidity { get; set; }
        public int RightTriggerRigidity { get; set; }
    }
}