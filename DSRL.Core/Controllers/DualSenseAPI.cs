using System.Runtime.InteropServices;
using DSRL.Core.Enums;
using DSRL.Core.Utilities;

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
        // DualSense VID/PID
        private const int SONY_VID = 0x054C;
        private const int DUALSENSE_PID = 0x0CE6;

        // --- REMOVED Hardcoded Output Report Lengths ---
        // private const int DUALSENSE_USB_OUTPUT_REPORT_LENGTH = 48; // Now fetched dynamically
        // private const int DUALSENSE_BT_OUTPUT_REPORT_LENGTH = 78; // Now fetched dynamically

        // HID API imports remain the same
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

        // ReadFile import if needed for ControllerInputReader (Assuming it exists)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);


        // Constants for SetupDiGetClassDevs remain the same
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;

        // Constants for CreateFile remain the same
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000; // Keep if ControllerInputReader uses overlapped I/O

        // Structures remain the same
        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        // Cache remains the same
        internal static Dictionary<string, ControllerInfo> controllerCache = new Dictionary<string, ControllerInfo>();

        // Modified GetTargetedControllers
        public static List<DualSenseController> GetTargetedControllers()
        {
            Logger.Log("Starting targeted DualSense controller detection...");
            List<DualSenseController> controllers = new List<DualSenseController>();

            try
            {
                var deviceList = HidSharp.DeviceList.Local;
                var hidDeviceList = deviceList.GetHidDevices().ToArray();
                Logger.Log($"Found {hidDeviceList.Length} HID devices");

                foreach (var device in hidDeviceList)
                {
                    if (device.VendorID == SONY_VID && device.ProductID == DUALSENSE_PID)
                    {
                        int maxOutputReportLength = 0;
                        try
                        {
                           maxOutputReportLength = device.GetMaxOutputReportLength();
                           // Ensure maxOutputReportLength is not zero, handle error if needed
                           if (maxOutputReportLength == 0)
                           {
                                Logger.Log($"Warning: MaxOutputReportLength reported as 0 for {device.DevicePath}. Skipping or using default.");
                                // Decide how to handle this - skip, use a default (e.g., 48), or log error.
                                // Let's assume we skip if it's 0, as we can't determine the correct size.
                                continue;
                           }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Error getting MaxOutputReportLength for {device.DevicePath}: {ex.Message}. Skipping device.");
                            continue; // Skip device if we can't get the essential length
                        }


                        Logger.Log($"Found potential DualSense: {device.DevicePath}");
                        Logger.Log($"Device details: VID={device.VendorID:X4}, PID={device.ProductID:X4}, MaxInputReportLength={device.GetMaxInputReportLength()}, MaxOutputReportLength={maxOutputReportLength}"); // Log the fetched length

                        // Using "mi_03" based on previous logs, adjust if needed
                        if (device.DevicePath.Contains("mi_03"))
                        {
                            Logger.Log("🎮 FOUND DUALSENSE CONTROLLER ON INTERFACE 3!");
                            string serialNumber = $"DS-{Math.Abs(device.DevicePath.GetHashCode() % 1000000):D6}";

                            // Create controller info INCLUDING MaxOutputReportLength
                            var controllerInfo = new ControllerInfo
                            {
                                DevicePath = device.DevicePath,
                                SerialNumber = serialNumber,
                                IsWireless = true, // Keep assumption or determine more reliably if possible
                                MaxOutputReportLength = (uint)maxOutputReportLength // Store the length
                            };

                            controllerCache[serialNumber] = controllerInfo;
                            DualSenseControllerExtensions.controllerInfoMap[serialNumber] = controllerInfo; // Ensure this map is also updated correctly

                            bool canOpen = false;
                            try
                            {
                                Logger.Log("Testing device access...");
                                // Use HidSharp's Open() which might be safer than CreateFile directly here
                                using (var stream = device.Open())
                                {
                                   canOpen = stream.CanWrite; // Check if we can actually write
                                   Logger.Log($"Successfully opened device for testing. CanWrite: {canOpen}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Cannot open device: {ex.Message}");
                                canOpen = false; // Explicitly set to false on error
                            }

                            // Only add controller if we could open it and confirm write access
                            if (canOpen)
                            {
                                Logger.Log($"Added controller info with serial: {serialNumber}, DevicePath: {device.DevicePath}, CanOpen: {canOpen}, MaxOutputReportLength: {controllerInfo.MaxOutputReportLength}");

                                DualSenseController controller = new DualSenseController
                                {
                                    SerialNumber = serialNumber,
                                    IsConnected = true
                                };

                                controller.SetControllerInfo(controllerInfo.ToDTO()); // Pass DTO
                                controllers.Add(controller);
                                Logger.Log($"Added controller with serial: {serialNumber}");
                            }
                            else
                            {
                                 Logger.Log($"Skipping controller {serialNumber} as device access test failed or write not permitted.");
                                 // Clean up potentially added map entries if skipping
                                 controllerCache.Remove(serialNumber);
                                 DualSenseControllerExtensions.controllerInfoMap.Remove(serialNumber);
                            }
                        }
                    }
                }
                Logger.Log($"Found {controllers.Count} usable DualSense controllers");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in targeted detection: {ex.Message}");
                Logger.Log($"Stack trace: {ex.StackTrace}");
            }
            return controllers;
        }

        // IsDualSenseController and GetSerialNumber remain the same

        // Modified internal ControllerInfo class
        internal class ControllerInfo
        {
            public string DevicePath { get; set; } = string.Empty;
            public string SerialNumber { get; set; } = string.Empty;
            public bool IsWireless { get; set; }
            public IntPtr DeviceHandle { get; set; } = IntPtr.Zero;
            public uint MaxOutputReportLength { get; set; } = 0; // <-- ADDED FIELD (initialize to 0 or a default)

            // Last known settings remain the same
            public DeadzoneShape DeadzoneShape { get; set; } = DeadzoneShape.Circle;
            public int DeadzoneRadius { get; set; } = 10;
            public int LeftTriggerRigidity { get; set; } = 50;
            public int RightTriggerRigidity { get; set; } = 50;

            // ToDTO needs to be adjusted if the DTO needs MaxOutputReportLength (likely not needed for public DTO)
            public ControllerInfoDTO ToDTO()
            {
                 return new ControllerInfoDTO
                 {
                     SerialNumber = this.SerialNumber,
                     IsWireless = this.IsWireless, // DTO might not need IsWireless if handled internally
                     DeadzoneShape = this.DeadzoneShape,
                     DeadzoneRadius = this.DeadzoneRadius,
                     LeftTriggerRigidity = this.LeftTriggerRigidity,
                     RightTriggerRigidity = this.RightTriggerRigidity
                     // MaxOutputReportLength is internal detail, typically not in DTO
                 };
            }


            // FromDTO needs to handle potentially missing MaxOutputReportLength if created from DTO
            public static ControllerInfo FromDTO(ControllerInfoDTO dto, string? devicePath = null, IntPtr handle = default, uint maxOutputLength = 0) // Added default for length
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
                    DeviceHandle = handle,
                    MaxOutputReportLength = maxOutputLength // Assign length if provided
                };
            }
        }
    }

    public static class DualSenseControllerExtensions
    {
        internal static Dictionary<string, DualSenseAPI.ControllerInfo> controllerInfoMap =
            new Dictionary<string, DualSenseAPI.ControllerInfo>();

        // Modified SetControllerInfo to preserve MaxOutputReportLength
       public static void SetControllerInfo(this DualSenseController controller, ControllerInfoDTO dto)
        {
            if (controllerInfoMap.TryGetValue(controller.SerialNumber, out var existingInfo))
            {
                // Update existing info, preserving DevicePath, Handle, and MaxOutputReportLength
                var updatedInfo = DualSenseAPI.ControllerInfo.FromDTO(
                    dto,
                    existingInfo.DevicePath,
                    existingInfo.DeviceHandle,
                    existingInfo.MaxOutputReportLength); // Preserve the length

                controllerInfoMap[controller.SerialNumber] = updatedInfo;

                 // Update controller's properties from DTO
                controller.DeadzoneShape = dto.DeadzoneShape;
                controller.DeadzoneRadius = dto.DeadzoneRadius;
                controller.LeftTriggerRigidity = dto.LeftTriggerRigidity;
                controller.RightTriggerRigidity = dto.RightTriggerRigidity;
            }
            else
            {
                // This case should ideally not happen if controller was found by GetTargetedControllers
                // If it can happen, we need a way to get the MaxOutputReportLength here,
                // or ensure ControllerInfo is always created via GetTargetedControllers first.
                Logger.Log($"Warning: Setting info for controller {controller.SerialNumber} not found in map. MaxOutputReportLength might be missing.");
                // Create new info, but MaxOutputReportLength will be default (e.g., 0)
                var newInfo = DualSenseAPI.ControllerInfo.FromDTO(dto);
                controllerInfoMap[controller.SerialNumber] = newInfo;

                 // Set controller properties
                controller.DeadzoneShape = dto.DeadzoneShape;
                controller.DeadzoneRadius = dto.DeadzoneRadius;
                controller.LeftTriggerRigidity = dto.LeftTriggerRigidity;
                controller.RightTriggerRigidity = dto.RightTriggerRigidity;
            }
        }


        public static ControllerInfoDTO? GetControllerInfo(this DualSenseController controller)
        {
            // This remains the same, returning the DTO representation
             if (controllerInfoMap.TryGetValue(controller.SerialNumber, out var info))
            {
                return info.ToDTO();
            }
            return null;
        }

        // Modified ApplySettingsToDevice
        public static bool ApplySettingsToDevice(this DualSenseController controller)
        {
            if (string.IsNullOrEmpty(controller.SerialNumber) ||
                !controllerInfoMap.TryGetValue(controller.SerialNumber, out var info))
            {
                Logger.Log("Failed to find controller info for serial: " + controller.SerialNumber);
                return false;
            }

            // Ensure MaxOutputReportLength is valid
            if (info.MaxOutputReportLength == 0)
            {
                 Logger.Log($"Error: MaxOutputReportLength is 0 for controller {controller.SerialNumber}. Cannot apply settings.");
                 return false;
            }


            // Update controller info with current settings from the controller object
            info.DeadzoneShape = controller.DeadzoneShape;
            info.DeadzoneRadius = controller.DeadzoneRadius;
            info.LeftTriggerRigidity = controller.LeftTriggerRigidity;
            info.RightTriggerRigidity = controller.RightTriggerRigidity;

            // Device path recovery logic remains the same (if needed)
            if (string.IsNullOrEmpty(info.DevicePath) &&
                DualSenseAPI.controllerCache.TryGetValue(controller.SerialNumber, out var cachedInfo))
            {
                info.DevicePath = cachedInfo.DevicePath;
                // CRITICAL: Ensure MaxOutputReportLength is also recovered if DevicePath was missing
                if (info.MaxOutputReportLength == 0 && cachedInfo.MaxOutputReportLength > 0)
                {
                    info.MaxOutputReportLength = cachedInfo.MaxOutputReportLength;
                     Logger.Log($"Recovered MaxOutputReportLength from cache: {info.MaxOutputReportLength}");
                }
                 Logger.Log($"Recovered device path from cache: {info.DevicePath}");
            }

            // Double check device path and length validity before proceeding
             if (string.IsNullOrEmpty(info.DevicePath) || info.MaxOutputReportLength == 0)
             {
                 Logger.Log($"Error: Missing DevicePath or MaxOutputReportLength after recovery attempt for {controller.SerialNumber}. Cannot apply settings.");
                 return false;
             }


            try
            {
                // Input reader logic remains the same for now
                bool hasActiveReader = controller.HasActiveInputReader();
                Logger.Log($"Controller has active reader: {hasActiveReader}");

                if (hasActiveReader)
                {
                     Logger.Log($"Attempting to apply settings using active input reader for {controller.SerialNumber}");
                     // Pass correct length to CreateTriggerEffectReport
                     byte[] report = CreateTriggerEffectReport(info, info.MaxOutputReportLength); // <-- Pass length
                     if (report.Length == 0) return false; // Check if report creation failed

                     if (!SendReportViaInputStream(controller, report)) // Placeholder logic
                     {
                         Logger.Log("Failed to send report via input stream. Falling back to direct handle.");
                         // Fall through to direct handle method below
                     }
                     else
                     {
                          Logger.Log("Settings applied successfully via input stream");
                          return true; // Success via input stream
                     }
                }


                // Direct device handle method
                Logger.Log($"Attempting to apply settings using direct device handle for {controller.SerialNumber}");
                Logger.Log($"DevicePath: {info.DevicePath}");
                Logger.Log($"Using MaxOutputReportLength: {info.MaxOutputReportLength}");


                // Open the device if handle is not valid
                bool handleNeedsOpening = (info.DeviceHandle == IntPtr.Zero || info.DeviceHandle == new IntPtr(-1));
                IntPtr handleToUse = info.DeviceHandle; // Use existing handle if valid

                 if (handleNeedsOpening)
                 {
                     Logger.Log("Opening device handle...");
                     handleToUse = OpenDevice(info.DevicePath); // Attempt to open
                     if (handleToUse == IntPtr.Zero || handleToUse == new IntPtr(-1))
                     {
                         int errorCode = Marshal.GetLastWin32Error();
                         Logger.Log($"Failed to open device handle. Error code: {errorCode}");
                         info.DeviceHandle = IntPtr.Zero; // Ensure handle is reset
                         return false; // Cannot proceed without handle
                     }
                     info.DeviceHandle = handleToUse; // Store the newly opened handle
                     Logger.Log($"Device handle opened: {info.DeviceHandle}");
                 }
                 else
                 {
                      Logger.Log($"Using existing device handle: {handleToUse}");
                 }


                // Create and send the report
                 Logger.Log("Creating trigger effect report...");
                 // Pass correct length to CreateTriggerEffectReport
                 byte[] directReport = CreateTriggerEffectReport(info, info.MaxOutputReportLength); // <-- Pass length
                 if (directReport.Length == 0)
                 {
                     Logger.Log("Failed to create trigger effect report.");
                     if (handleNeedsOpening) CloseDevice(info); // Close handle if we just opened it
                     return false;
                 }

                 Logger.Log($"Report length: {directReport.Length}, First bytes: {BitConverter.ToString(directReport, 0, Math.Min(10, directReport.Length))}");

                 Logger.Log("Sending report to device...");
                 // Pass correct length to SendReport
                 if (!SendReport(handleToUse, directReport, info.MaxOutputReportLength)) // <-- Pass length
                 {
                     int errorCode = Marshal.GetLastWin32Error();
                     Logger.Log($"Failed to send report. Error code: {errorCode}");
                     // Close the handle ONLY if we opened it in this attempt OR if the error suggests the handle is now invalid
                     if (handleNeedsOpening || errorCode == 6 /*ERROR_INVALID_HANDLE*/)
                     {
                          CloseDevice(info);
                     }
                     return false;
                 }


                Logger.Log("Settings applied successfully via direct handle");
                 // Close the handle ONLY if we specifically opened it for this operation
                 // Keep it open otherwise for potential reuse (e.g., by input reader or subsequent calls)
                 if (handleNeedsOpening)
                 {
                     CloseDevice(info);
                      Logger.Log("Closed device handle opened for this operation.");
                 }
                 return true; // Success


            }
            catch (Exception ex)
            {
                 Logger.Log($"Exception in ApplySettingsToDevice: {ex.Message}");
                 Logger.Log($"Stack trace: {ex.StackTrace}");
                 // Attempt to close the handle if it might be open and invalid
                 CloseDevice(info);
                 return false;
            }
        }


        // OpenDevice remains the same
        private static IntPtr OpenDevice(string devicePath)
        {
             // Consider adding FILE_FLAG_OVERLAPPED back if needed by ReadFile/WriteFile async operations
             return CreateFile(
                 devicePath,
                 GENERIC_READ | GENERIC_WRITE,
                 FILE_SHARE_READ | FILE_SHARE_WRITE,
                 IntPtr.Zero,
                 OPEN_EXISTING,
                 FILE_ATTRIBUTE_NORMAL, // Removed FILE_FLAG_OVERLAPPED if not using overlapped I/O
                 IntPtr.Zero);
        }


        // CloseDevice remains the same
        private static void CloseDevice(DualSenseAPI.ControllerInfo info)
        {
            if (info.DeviceHandle != IntPtr.Zero && info.DeviceHandle != new IntPtr(-1))
            {
                Logger.Log($"Closing device handle: {info.DeviceHandle}");
                CloseHandle(info.DeviceHandle);
                info.DeviceHandle = IntPtr.Zero;
            }
        }

        // Modified SendReport to accept length
        private static bool SendReport(IntPtr deviceHandle, byte[] report, uint reportLength) // <-- Added reportLength parameter
        {
            if (report == null || report.Length < reportLength)
            {
                Logger.Log($"Error: Report buffer is null or smaller ({report?.Length ?? -1}) than specified reportLength ({reportLength}).");
                return false;
            }
            if (deviceHandle == IntPtr.Zero || deviceHandle == new IntPtr(-1))
            {
                 Logger.Log($"Error: Invalid device handle passed to SendReport.");
                 return false;
            }


            uint bytesWritten = 0;
             // Use the provided reportLength in the WriteFile call
             bool success = WriteFile(deviceHandle, report, reportLength, out bytesWritten, IntPtr.Zero);
             bool lengthMatch = (bytesWritten == reportLength);

             if (!success || !lengthMatch)
             {
                int errorCode = Marshal.GetLastWin32Error(); // Get error code immediately
                 Logger.Log($"WriteFile failed or wrote incorrect number of bytes. Success: {success}, Expected Length: {reportLength}, Bytes Written: {bytesWritten}, Error Code: {errorCode}");
                 return false;
             }
             return true;
        }


        // SendReportViaInputStream placeholder remains the same
        private static bool SendReportViaInputStream(DualSenseController controller, byte[] report)
        {
            // Placeholder - requires implementation using the input reader's stream/handle
            return false;
        }

        // Modified CreateTriggerEffectReport to accept length and handle 48-byte format
         private static byte[] CreateTriggerEffectReport(DualSenseAPI.ControllerInfo info, uint maxReportLength) // <-- Added length parameter
         {
             // Validate the expected length - primarily handle the 48-byte case seen in logs
             if (maxReportLength != 48) // Adjust this check if other lengths are valid and need handling
             {
                  Logger.Log($"Warning: CreateTriggerEffectReport currently only supports MaxOutputReportLength of 48. Received {maxReportLength}. Report creation might be incorrect.");
                   // Decide: return empty array, throw exception, or attempt generic creation?
                   // For now, let's focus on the 48-byte case from the log. Return empty if not 48.
                   // You might need to add logic for other sizes (e.g., 78 for BT if that interface reports differently)
                  if (maxReportLength == 78)
                  {
                      Logger.Log("Detected request for 78-byte report (BT). Using BT format.");
                      // Fall through to BT logic below (needs re-adding if required)
                  }
                  else
                  {
                     Logger.Log("Unsupported report length requested. Returning empty report.");
                     return Array.Empty<byte>();
                  }
             }


             byte[] report = new byte[maxReportLength]; // Use the passed length

             // Report ID and Flags depend on the specific report format (USB vs BT)
             // Based on log MaxOutputReportLength=48, assume USB-style report format for this interface (mi_03)
             // even if controller is connected wirelessly.

             if (maxReportLength == 48)
             {
                 // --- USB Style Report (48 bytes) ---
                 report[0] = 0x02; // Report ID for USB trigger effect setting
                 report[1] = 0x01 | 0x02 | 0x04; // Operational flags: enable rumble (0x01), mic LED (0x02), trigger effects (0x04)
                 report[2] = 0x00; // Reset flags? (Check DS4Windows or ViGEm source if unsure)

                 // Left Trigger (L2) effect starts at index 11 for 48-byte report
                 int leftIndex = 11;
                 ApplyTriggerEffect(report, leftIndex, info.LeftTriggerRigidity);

                 // Right Trigger (R2) effect starts at index 22 for 48-byte report
                 int rightIndex = 22;
                 ApplyTriggerEffect(report, rightIndex, info.RightTriggerRigidity);
             }
             else if (maxReportLength == 78)
             {
                 // --- Bluetooth Style Report (78 bytes) ---
                 report[0] = 0x31; // Report ID for BT feature report
                 report[1] = 0x02; // Feature report type? (Confirm this)
                 report[2] = 0xFF; // Output report flags (Confirm this - old code used 0xFF at index 1)
                 report[3] = 0x04; // Enable trigger effects flag (Confirm this - old code used |= 0x04 at index 2)

                 // Left Trigger (L2) effect starts at index 22 for 78-byte BT report (old code)
                  int leftIndex = 22; // Old code offset
                 ApplyTriggerEffect(report, leftIndex, info.LeftTriggerRigidity);

                 // Right Trigger (R2) effect starts at index 29 (left + 7) for 78-byte BT report (old code)
                  int rightIndex = leftIndex + 7; // Old code offset
                 ApplyTriggerEffect(report, rightIndex, info.RightTriggerRigidity);
             }
             else
             {
                 Logger.Log("Cannot create report: Unsupported maxReportLength provided.");
                 return Array.Empty<byte>();
             }


             return report;
         }


        // Helper method to apply trigger effect data to the report buffer
        private static void ApplyTriggerEffect(byte[] report, int startIndex, int rigidityPercent)
        {
             // Ensure buffer is large enough
             if (report == null || startIndex + 3 > report.Length) // Need 3 bytes: mode, strength, startPos
             {
                 Logger.Log("Error: Report buffer too small or null in ApplyTriggerEffect.");
                 return;
             }


             const byte EffectMode_NoResistance = 0x00; // Mode 0
             const byte EffectMode_Continuous = 0x01; // Mode 1


             byte effectStrength = (byte)Math.Clamp(rigidityPercent * 2.55f, 0, 255);


             if (rigidityPercent <= 0)
             {
                 report[startIndex] = EffectMode_NoResistance; // Mode
                 report[startIndex + 1] = 0; // Param 1 (unused for NoResistance)
                 report[startIndex + 2] = 0; // Param 2 (unused for NoResistance)
             }
             else
             {
                 report[startIndex] = EffectMode_Continuous; // Mode
                 report[startIndex + 1] = effectStrength; // Param 1 (Strength for Continuous)
                 report[startIndex + 2] = 0; // Param 2 (Start position 0 = full range)
             }
             // Add logic for other parameters (3-8) if needed for different effects
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