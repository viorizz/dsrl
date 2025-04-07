using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DSRL.Core.Controllers
{
    /// <summary>
    /// Alternative controller detection using direct Windows API calls
    /// </summary>
    public static class WindowsControllerDetection
    {
        // Windows API constants
        private const int DIGCF_PRESENT = 0x00000002;
        private const int DIGCF_DEVICEINTERFACE = 0x00000010;
        private const int SPDRP_HARDWAREID = 0x00000001;
        private const int SPDRP_FRIENDLYNAME = 0x0000000C;
        private const int SPDRP_DEVICEDESC = 0x00000000;
        private const int BUFFER_SIZE = 1024;

        // Windows API structures
        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BUFFER_SIZE)]
            public string DevicePath;
        }

        // Windows API functions
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, int MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, int Property, IntPtr PropertyRegDataType, IntPtr PropertyBuffer, int PropertyBufferSize, ref int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetHidGuid(out Guid HidGuid);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool HidD_GetProductString(IntPtr HidDeviceObject, IntPtr Buffer, int BufferLength);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool HidD_GetManufacturerString(IntPtr HidDeviceObject, IntPtr Buffer, int BufferLength);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool HidD_GetSerialNumberString(IntPtr HidDeviceObject, IntPtr Buffer, int BufferLength);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Constants for CreateFile
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        /// <summary>
        /// Get all HID device paths with their properties
        /// </summary>
        public static List<(string devicePath, string hardwareId, string deviceName, string description)> GetAllHidDevicePaths()
        {
            List<(string, string, string, string)> result = new List<(string, string, string, string)>();
            
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);
            
            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            
            if (deviceInfoSet == IntPtr.Zero || deviceInfoSet.ToInt64() == -1)
            {
                Console.WriteLine("Error getting device info set: " + Marshal.GetLastWin32Error());
                return result;
            }
            
            try
            {
                int deviceIndex = 0;
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
                
                // Enumerate all HID interfaces
                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, deviceIndex, ref deviceInterfaceData))
                {
                    int requiredSize = 0;
                    
                    // Get required size for the detail structure
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, ref requiredSize, IntPtr.Zero);
                    
                    // Allocate memory for the detail structure
                    IntPtr detailDataBuffer = Marshal.AllocHGlobal(requiredSize);
                    
                    try
                    {
                        // Set the cbSize field (different size for 32 and 64 bit)
                        Marshal.WriteInt32(detailDataBuffer, IntPtr.Size == 8 ? 8 : 5);
                        
                        // Get the detailed information
                        if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer, requiredSize, ref requiredSize, IntPtr.Zero))
                        {
                            // Extract device path - offset depends on pointer size (32/64 bit)
                            string devicePath = Marshal.PtrToStringAuto(IntPtr.Add(detailDataBuffer, IntPtr.Size == 8 ? 8 : 5)) ?? string.Empty;
                            
                            // Now get the device instance for this interface to query more properties
                            SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
                            
                            if (SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData))
                            {
                                string hardwareId = GetDeviceProperty(deviceInfoSet, deviceInfoData, SPDRP_HARDWAREID);
                                string deviceName = GetDeviceProperty(deviceInfoSet, deviceInfoData, SPDRP_FRIENDLYNAME);
                                string description = GetDeviceProperty(deviceInfoSet, deviceInfoData, SPDRP_DEVICEDESC);
                                
                                if (!string.IsNullOrEmpty(devicePath))
                                {
                                    result.Add((devicePath, hardwareId, deviceName, description));
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailDataBuffer);
                    }
                    
                    deviceIndex++;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
            
            return result;
        }
        
        /// <summary>
        /// Get device property string
        /// </summary>
        private static string GetDeviceProperty(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData, int property)
        {
            int requiredSize = 0;
            
            // Get required size
            SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property, IntPtr.Zero, IntPtr.Zero, 0, ref requiredSize);
            
            if (requiredSize <= 0)
                return string.Empty;
            
            // Allocate buffer
            IntPtr propertyBuffer = Marshal.AllocHGlobal(requiredSize);
            
            try
            {
                // Get the property
                if (SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property, IntPtr.Zero, propertyBuffer, requiredSize, ref requiredSize))
                {
                    return Marshal.PtrToStringAuto(propertyBuffer) ?? string.Empty;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(propertyBuffer);
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Detect DualSense controllers using Windows API
        /// </summary>
        public static List<DualSenseController> DetectDualSenseControllers()
        {
            List<DualSenseController> controllers = new List<DualSenseController>();
            List<(string devicePath, string hardwareId, string deviceName, string description)> allDevices = GetAllHidDevicePaths();
            
            Console.WriteLine($"Found {allDevices.Count} HID devices with Windows API");
            
            foreach (var device in allDevices)
            {
                Console.WriteLine($"Device: {device.deviceName} | HW ID: {device.hardwareId} | Path: {device.devicePath}");
                
                // Check if this could be a DualSense controller
                bool isSonyDevice = 
                    (device.hardwareId != null && device.hardwareId.Contains("VID_054C")) ||
                    (device.deviceName != null && device.deviceName.Contains("Sony"));
                    
                bool isDualSense = 
                    isSonyDevice && (
                    (device.hardwareId != null && (
                        device.hardwareId.Contains("PID_0CE6") ||
                        device.hardwareId.Contains("PID_0DF2") ||
                        device.hardwareId.Contains("PID_0CE7") ||
                        device.hardwareId.Contains("PID_0CE9"))) ||
                    (device.deviceName != null && (
                        device.deviceName.Contains("DualSense") || 
                        device.deviceName.Contains("Wireless Controller"))) ||
                    (device.description != null && device.description.Contains("DualSense")));
                
                if (isDualSense)
                {
                    Console.WriteLine("DETECTED DUALSENSE CONTROLLER!");
                    
                    // Generate a unique serial number if not available
                    string serialNumber = $"DS-{Math.Abs(device.devicePath.GetHashCode() % 1000000):D6}";
                    
                    // Try to get serial number from device
                    var actualSerialNumber = GetDeviceSerialNumber(device.devicePath);
                    if (!string.IsNullOrEmpty(actualSerialNumber))
                    {
                        serialNumber = actualSerialNumber;
                    }
                    
                    // Create controller info
                    var controllerInfo = new DualSenseAPI.ControllerInfo
                    {
                        DevicePath = device.devicePath,
                        SerialNumber = serialNumber,
                        IsWireless = (device.deviceName != null && device.deviceName.ToLowerInvariant().Contains("wireless")) ||
                                     (device.hardwareId != null && device.hardwareId.ToLowerInvariant().Contains("bluetooth"))
                    };
                    
                    // Create a controller object
                    DualSenseController controller = new DualSenseController
                    {
                        SerialNumber = serialNumber,
                        IsConnected = true
                    };
                    
                    // Set the controller's info using the extension method
                    controller.SetControllerInfo(controllerInfo.ToDTO());
                    controllers.Add(controller);
                }
            }
            
            return controllers;
        }
        
        /// <summary>
        /// Try to get the device serial number
        /// </summary>
        private static string GetDeviceSerialNumber(string devicePath)
        {
            IntPtr deviceHandle = CreateFile(
                devicePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);
                
            if (deviceHandle == IntPtr.Zero || deviceHandle.ToInt64() == -1)
            {
                return string.Empty;
            }
            
            try
            {
                IntPtr buffer = Marshal.AllocHGlobal(BUFFER_SIZE);
                try
                {
                    if (HidD_GetSerialNumberString(deviceHandle, buffer, BUFFER_SIZE))
                    {
                        return Marshal.PtrToStringUni(buffer)?.Trim() ?? string.Empty;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                CloseHandle(deviceHandle);
            }
            
            return string.Empty;
        }
    }
}