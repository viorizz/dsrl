using System;
using System.IO;
using System.Linq;
using HidSharp;

namespace DSRL.Core.Diagnostics
{
    public static class HIDDiagnostic
    {
        public static string GenerateFullReport()
        {
            try
            {
                StringWriter writer = new StringWriter();
                writer.WriteLine("============== HID DEVICE DIAGNOSTIC REPORT ==============");
                writer.WriteLine($"Date/Time: {DateTime.Now}");
                writer.WriteLine($"OS Version: {Environment.OSVersion}");
                writer.WriteLine($"Process Elevated: {IsProcessElevated()}");
                writer.WriteLine();
                writer.WriteLine("============== ALL HID DEVICES ==============");

                var deviceList = DeviceList.Local;
                var hidDevices = deviceList.GetHidDevices().ToArray();
                
                writer.WriteLine($"Total HID devices found: {hidDevices.Length}");
                
                int deviceCounter = 0;
                foreach (var device in hidDevices)
                {
                    deviceCounter++;
                    writer.WriteLine();
                    writer.WriteLine($"--- DEVICE #{deviceCounter} ---");
                    writer.WriteLine($"Manufacturer: {GetSafeProperty(() => device.GetManufacturer())}");
                    writer.WriteLine($"Product Name: {GetSafeProperty(() => device.GetProductName())}");
                    writer.WriteLine($"Serial Number: {GetSafeProperty(() => device.GetSerialNumber())}");
                    writer.WriteLine($"VID: 0x{device.VendorID:X4} ({device.VendorID})");
                    writer.WriteLine($"PID: 0x{device.ProductID:X4} ({device.ProductID})");
                    // Remove unavailable methods
                    // writer.WriteLine($"Usage Page: 0x{device.GetUsagePage():X4}");
                    // writer.WriteLine($"Usage: 0x{device.GetUsage():X4}");
                    writer.WriteLine($"Version Number: {device.ReleaseNumber}");
                    writer.WriteLine($"Device Path: {device.DevicePath}");
                    writer.WriteLine($"Max Input Report Length: {device.GetMaxInputReportLength()}");
                    writer.WriteLine($"Max Output Report Length: {device.GetMaxOutputReportLength()}");
                    writer.WriteLine($"Max Feature Report Length: {device.GetMaxFeatureReportLength()}");

                    // Check if this could be a DualSense controller
                    bool isSonyVID = device.VendorID == 0x054C;
                    bool isDualSensePID = new[] { 0x0CE6, 0x0DF2, 0x0CE7, 0x0CE9 }.Contains(device.ProductID);
                    
                    // Fix the bool to string conversion issues
                    string nameContainsDualSense = "false";
                    string nameContainsWirelessController = "false";
                    try {
                        nameContainsDualSense = device.GetProductName()?.ToLowerInvariant().Contains("dualsense").ToString() ?? "false";
                        nameContainsWirelessController = device.GetProductName()?.ToLowerInvariant().Contains("wireless controller").ToString() ?? "false";
                    } catch {}
                    
                    bool nameMatch = nameContainsDualSense == "True" || nameContainsWirelessController == "True";
                    
                    writer.WriteLine($"COULD BE DUALSENSE: {isSonyVID && (isDualSensePID || nameMatch)}");
                    
                    // Try to open device
                    try
                    {
                        using (var stream = device.Open())
                        {
                            writer.WriteLine("DEVICE CAN BE OPENED: YES");
                        }
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine($"DEVICE CAN BE OPENED: NO - {ex.Message}");
                    }
                }
                
                writer.WriteLine();
                writer.WriteLine("============== KNOWN DEVICE TYPES ==============");
                
                // Get serial devices
                var serialDevices = deviceList.GetSerialDevices();
                writer.WriteLine($"Serial Devices: {serialDevices.Count()}");
                
                // Remove unavailable method
                // var streamableDevices = deviceList.GetStreamableDevices();
                // writer.WriteLine($"Streamable Devices: {streamableDevices.Count()}");
                
                return writer.ToString();
            }
            catch (Exception ex)
            {
                return $"Error generating HID diagnostic report: {ex.Message}\n{ex.StackTrace}";
            }
        }

        private static string GetSafeProperty(Func<string> propertyGetter)
        {
            try
            {
                return propertyGetter() ?? "[NULL]";
            }
            catch (Exception ex)
            {
                return $"[ERROR: {ex.Message}]";
            }
        }
        
        private static bool IsProcessElevated()
        {
            // Only check for Windows platforms
            if (OperatingSystem.IsWindows())
            {
                // Check if process is running with admin rights
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            return false; // Default for non-Windows
        }

        // Method to save report to file
        public static void SaveReportToFile(string path)
        {
            try
            {
                string report = GenerateFullReport();
                File.WriteAllText(path, report);
                Console.WriteLine($"HID Diagnostic report saved to: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving diagnostic report: {ex.Message}");
            }
        }
    }
}