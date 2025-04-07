using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DSRL.Core.Enums;

namespace DSRL.Core.Controllers
{
    /// <summary>
    /// DTO for controller information that can be passed to and from the public API
    /// </summary>
    public class ControllerInfoDTO
    {
        // Initialize non-nullable string properties with empty string
        public string SerialNumber { get; set; } = string.Empty;
        public bool IsWireless { get; set; }
        public DeadzoneShape DeadzoneShape { get; set; } = DeadzoneShape.Circle;
        public int DeadzoneRadius { get; set; } = 10;
        public int LeftTriggerRigidity { get; set; } = 50;
        public int RightTriggerRigidity { get; set; } = 50;
}
    /// <summary>
    /// Handles low-level communication with DualSense controllers
    /// </summary>
    public class DualSenseAPI
    {
        // DualSense VID/PID for USB connection
        private const int SONY_VID = 0x054C;
        private const int DUALSENSE_PID = 0x0CE6;
        
        // Output report lengths
        private const int DUALSENSE_USB_OUTPUT_REPORT_LENGTH = 48;
        private const int DUALSENSE_BT_OUTPUT_REPORT_LENGTH = 78;
        
        // HID API imports for device communication
        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetHidGuid(out Guid HidGuid);
        
        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);
        
        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);
        
        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData);
        
        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);
        
        // Constants for SetupDiGetClassDevs
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        
        // Constants for CreateFile
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        
        // Structure for device interface data
        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }
        
        // Structure for device interface detail data
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }
        
        // Cache of identified controllers
        private static Dictionary<string, ControllerInfo> controllerCache = new Dictionary<string, ControllerInfo>();
        
        
        /// <returns>List of DualSenseController objects</returns>
        // A more reliable approach using HidSharp directly

        // Add this method to DualSenseAPI.cs

        /// <summary>
        /// Detects controllers based on exact patterns found in diagnostic
        /// </summary>
        public static List<DualSenseController> GetTargetedControllers()
        {
            Console.WriteLine("Starting targeted DualSense controller detection...");
            List<DualSenseController> controllers = new List<DualSenseController>();
            
            try
            {
                // Get all connected devices
                var deviceList = HidSharp.DeviceList.Local;
                var hidDeviceList = deviceList.GetHidDevices().ToArray();
                
                Console.WriteLine($"Found {hidDeviceList.Length} HID devices");
                
                foreach (var device in hidDeviceList)
                {
                    // Look specifically for DualSense pattern from diagnostic
                    if (device.VendorID == 0x054C && device.ProductID == 0x0CE6)
                    {
                        Console.WriteLine($"Found potential DualSense: {device.DevicePath}");
                        
                        // Check if path contains mi_03 which seems to be the interface we need
                        if (device.DevicePath.Contains("mi_03"))
                        {
                            Console.WriteLine("🎮 FOUND DUALSENSE CONTROLLER ON INTERFACE 3!");
                            
                            // Generate a unique serial number based on the device path
                            string serialNumber = $"DS-{Math.Abs(device.DevicePath.GetHashCode() % 1000000):D6}";
                            
                            // Create controller info
                            var controllerInfo = new ControllerInfo
                            {
                                DevicePath = device.DevicePath,
                                SerialNumber = serialNumber,
                                IsWireless = true // Most likely wireless
                            };
                            
                            controllerCache[device.DevicePath] = controllerInfo;
                            
                            // Create a controller object
                            DualSenseController controller = new DualSenseController
                            {
                                SerialNumber = serialNumber,
                                IsConnected = true
                            };
                            
                            // Set the controller's info using DTO
                            controller.SetControllerInfo(controllerInfo.ToDTO());
                            controllers.Add(controller);
                            
                            Console.WriteLine($"Added controller with serial: {serialNumber}");
                        }
                    }
                }
                
                Console.WriteLine($"Found {controllers.Count} DualSense controllers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in targeted detection: {ex.Message}");
            }
            
            return controllers;
        }
        
        /// <summary>
        /// Checks if a device is a DualSense controller with improved detection
        /// </summary>
        private static bool IsDualSenseController(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
                return false;
                
            string path = devicePath.ToLowerInvariant();
            
            // Based on diagnostic report, check for interface 3 specifically
            if (path.Contains("vid_054c") && path.Contains("pid_0ce6") && path.Contains("mi_03"))
            {
                Console.WriteLine($"Found DualSense controller: {devicePath}");
                return true;
            }
            
            // Also check for other potential matches
            bool hasSonyVID = path.Contains("vid_054c");
            bool hasDualSensePID = path.Contains("pid_0ce6") || path.Contains("pid_0df2") || 
                                path.Contains("pid_0ce7") || path.Contains("pid_0ce9");
            bool hasControllerName = path.Contains("dualsense") || path.Contains("wireless controller");
            
            // More verbose logging to help with debugging
            if (hasSonyVID && (hasDualSensePID || hasControllerName))
            {
                Console.WriteLine($"Found potential DualSense with alternative pattern: {devicePath}");
                return true;
            }
            
            return false;
        }
        /// <summary>
        /// Gets the serial number of a device
        /// </summary>
        private static string GetSerialNumber(string? devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
                return "UNKNOWN";
                
            return $"DS-{Math.Abs(devicePath.GetHashCode() % 1000000):D6}";
        }
        
        /// <summary>
        /// Internal class to store controller info
        /// </summary>
        internal class ControllerInfo
        {
            // Initialize string properties with empty strings
            public string DevicePath { get; set; } = string.Empty;
            public string SerialNumber { get; set; } = string.Empty;
            public bool IsWireless { get; set; }
            public IntPtr DeviceHandle { get; set; } = IntPtr.Zero;
            
            // Last known settings
            public DeadzoneShape DeadzoneShape { get; set; } = DeadzoneShape.Circle;
            public int DeadzoneRadius { get; set; } = 10;
            public int LeftTriggerRigidity { get; set; } = 50;
            public int RightTriggerRigidity { get; set; } = 50;

            /// <summary>
            /// Converts the internal ControllerInfo to a public DTO
            /// </summary>
            public ControllerInfoDTO ToDTO()
            {
                return new ControllerInfoDTO
                {
                    SerialNumber = this.SerialNumber,
                    IsWireless = this.IsWireless,
                    DeadzoneShape = this.DeadzoneShape,
                    DeadzoneRadius = this.DeadzoneRadius,
                    LeftTriggerRigidity = this.LeftTriggerRigidity,
                    RightTriggerRigidity = this.RightTriggerRigidity
                };
            }

            /// <summary>
            /// Creates an internal ControllerInfo from a public DTO
            /// </summary>
            public static ControllerInfo FromDTO(ControllerInfoDTO dto, string? devicePath = null, IntPtr handle = default)
            {
                return new ControllerInfo
                {
                    DevicePath = devicePath ?? string.Empty,
                    SerialNumber = dto.SerialNumber,
                    IsWireless = dto.IsWireless,
                    DeadzoneShape = dto.DeadzoneShape,
                    DeadzoneRadius = dto.DeadzoneRadius,
                    LeftTriggerRigidity = dto.LeftTriggerRigidity,
                    RightTriggerRigidity = dto.RightTriggerRigidity,
                    DeviceHandle = handle
                };
            }
        }
    }
    
    /// <summary>
    /// Extension methods for DualSenseController
    /// </summary>
    public static class DualSenseControllerExtensions
    {
        // Dictionary to store controller info
        private static Dictionary<string, DualSenseAPI.ControllerInfo> controllerInfoMap = 
            new Dictionary<string, DualSenseAPI.ControllerInfo>();
        
        /// <summary>
        /// Sets the controller info for a DualSenseController
        /// </summary>
        public static void SetControllerInfo(this DualSenseController controller, ControllerInfoDTO dto)
        {
            // Check if we already have info for this controller
            if (controllerInfoMap.TryGetValue(controller.SerialNumber, out var existingInfo))
            {
                // Update existing info with new values, preserving device path and handle
                var updatedInfo = DualSenseAPI.ControllerInfo.FromDTO(
                    dto, 
                    existingInfo.DevicePath, 
                    existingInfo.DeviceHandle);
                    
                controllerInfoMap[controller.SerialNumber] = updatedInfo;
            }
            else
            {
                // Create new info
                var newInfo = DualSenseAPI.ControllerInfo.FromDTO(dto);
                controllerInfoMap[controller.SerialNumber] = newInfo;
            }
            
            // Set initial values from the DTO
            controller.DeadzoneShape = dto.DeadzoneShape;
            controller.DeadzoneRadius = dto.DeadzoneRadius;
            controller.LeftTriggerRigidity = dto.LeftTriggerRigidity;
            controller.RightTriggerRigidity = dto.RightTriggerRigidity;
        }
        
        /// <summary>
        /// Gets the controller info for a DualSenseController
        /// </summary>
        public static ControllerInfoDTO? GetControllerInfo(this DualSenseController controller)
        {
            if (controllerInfoMap.TryGetValue(controller.SerialNumber, out var info))
            {
                return info.ToDTO();
            }
            
            return null;
        }

        // And fix other potential null reference issues:

        /// <summary>
        /// Apply settings to the controller
        /// </summary>
        public static bool ApplySettingsToDevice(this DualSenseController controller)
        {
            // Get the internal controller info
            if (string.IsNullOrEmpty(controller.SerialNumber) || 
                !controllerInfoMap.TryGetValue(controller.SerialNumber, out var info))
            {
                return false;
            }
            
            // Update controller info with current settings
            info.DeadzoneShape = controller.DeadzoneShape;
            info.DeadzoneRadius = controller.DeadzoneRadius;
            info.LeftTriggerRigidity = controller.LeftTriggerRigidity;
            info.RightTriggerRigidity = controller.RightTriggerRigidity;
            
            try
            {
                // Open the device if not already open
                if (info.DeviceHandle == IntPtr.Zero || info.DeviceHandle == new IntPtr(-1))
                {
                    info.DeviceHandle = OpenDevice(info.DevicePath);
                    if (info.DeviceHandle == IntPtr.Zero || info.DeviceHandle == new IntPtr(-1))
                    {
                        return false;
                    }
                }
                
                // Create and send the report to set trigger effects
                byte[] report = CreateTriggerEffectReport(info);
                if (!SendReport(info.DeviceHandle, report))
                {
                    CloseDevice(info);
                    return false;
                }
                
                // Create and send the report to set deadzone
                // This would typically be handled by the game or system, not through the controller directly
                // For demonstration purposes, we'll just return success
                
                return true;
            }
            catch
            {
                CloseDevice(info);
                return false;
            }
        }
        
        /// <summary>
        /// Open a device for communication
        /// </summary>
        private static IntPtr OpenDevice(string devicePath)
        {
            return CreateFile(
                devicePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);
        }
        
        /// <summary>
        /// Close a device
        /// </summary>
        private static void CloseDevice(DualSenseAPI.ControllerInfo info)
        {
            if (info.DeviceHandle != IntPtr.Zero && info.DeviceHandle != new IntPtr(-1))
            {
                CloseHandle(info.DeviceHandle);
                info.DeviceHandle = IntPtr.Zero;
            }
        }
        
        /// <summary>
        /// Send a report to the device
        /// </summary>
        private static bool SendReport(IntPtr deviceHandle, byte[] report)
        {
            uint bytesWritten = 0;
            return WriteFile(deviceHandle, report, (uint)report.Length, out bytesWritten, IntPtr.Zero) && 
                   bytesWritten == report.Length;
        }
        
        /// <summary>
        /// Create a report to set trigger effects
        /// </summary>
        private static byte[] CreateTriggerEffectReport(DualSenseAPI.ControllerInfo info)
        {
            // DualSense trigger effect modes
            const byte EffectMode_NoResistance = 0;
            const byte EffectMode_Continuous = 1;
            
            // Report size depends on connection type
            int reportSize = info.IsWireless ? 78 : 48;
            byte[] report = new byte[reportSize];
            
            // Set report ID
            report[0] = info.IsWireless ? (byte)0x31 : (byte)0x02;
            
            // Output report flags
            report[1] = 0xFF; // enable rumble, audio, lightbar
            
            // Set flag for trigger effect
            report[2] |= 0x04; // Enable trigger effects
            
            // Left trigger (L2) effect - index 22
            int leftIndex = info.IsWireless ? 22 : 11;
            
            // Convert rigidity (0-100) to effect strength (0-255)
            byte leftStrength = (byte)(info.LeftTriggerRigidity * 2.55f);
            
            // Choose effect mode based on rigidity
            if (info.LeftTriggerRigidity == 0)
            {
                report[leftIndex] = EffectMode_NoResistance; // No resistance
            }
            else
            {
                report[leftIndex] = EffectMode_Continuous; // Continuous resistance
                report[leftIndex + 1] = leftStrength; // Effect strength
                report[leftIndex + 2] = 0; // Start position (0 = all the way up)
            }
            
            // Right trigger (R2) effect - offset by 7 from left trigger
            int rightIndex = leftIndex + 7;
            
            // Convert rigidity (0-100) to effect strength (0-255)
            byte rightStrength = (byte)(info.RightTriggerRigidity * 2.55f);
            
            // Choose effect mode based on rigidity
            if (info.RightTriggerRigidity == 0)
            {
                report[rightIndex] = EffectMode_NoResistance; // No resistance
            }
            else
            {
                report[rightIndex] = EffectMode_Continuous; // Continuous resistance
                report[rightIndex + 1] = rightStrength; // Effect strength
                report[rightIndex + 2] = 0; // Start position (0 = all the way up)
            }
            
            return report;
        }
        
        // Constants for CreateFile
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        
        // Win32 API imports for device communication
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
    }
}