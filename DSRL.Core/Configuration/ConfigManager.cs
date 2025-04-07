using System;
using System.IO;
using System.Xml.Serialization;
using DSRL.Core.Enums;

namespace DSRL.Core.Configuration
{
    /// <summary>
    /// Manages application configuration and settings
    /// </summary>
    public class ConfigManager
    {
        private static readonly string ConfigFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DSRL");
            
        private static readonly string ConfigFilePath = Path.Combine(ConfigFolderPath, "settings.xml");
        
        // Singleton instance
        private static ConfigManager? _instance;
        
        // Configuration data
        public AppConfig Config { get; private set; } = new AppConfig();
        
        // Get singleton instance
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigManager();
                }
                
                return _instance;
            }
        }
        
        // Private constructor
        private ConfigManager()
        {
            // Initialize config
            LoadConfig();
        }

        private AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                DefaultDeadzoneShape = DeadzoneShape.Circle,
                DefaultDeadzoneRadius = 10,
                DefaultLeftTriggerRigidity = 50,
                DefaultRightTriggerRigidity = 50,
                AutoApplySettings = true,
                ShowControllerDebugInfo = false,
                ControllerProfiles = Array.Empty<ControllerProfile>()
            };
        }
        
        /// <summary>
        /// Loads configuration from file or creates default config
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                // Create config directory if it doesn't exist
                if (!Directory.Exists(ConfigFolderPath))
                {
                    Directory.CreateDirectory(ConfigFolderPath);
                }
                
                // Load config from file if it exists
                if (File.Exists(ConfigFilePath))
                {
                    using (FileStream fs = new FileStream(ConfigFilePath, FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
                        // Fix for null warning with the safe cast pattern
                        var deserialized = serializer.Deserialize(fs);
                        if (deserialized is AppConfig config)
                        {
                            Config = config;
                        }
                        else
                        {
                            // Handle case where deserialization didn't produce an AppConfig
                            Config = CreateDefaultConfig();
                            Console.WriteLine("Config file exists but couldn't be deserialized properly. Using defaults.");
                        }
                    }
                }
                else
                {
                    // Create default config
                    Config = new AppConfig
                    {
                        DefaultDeadzoneShape = DeadzoneShape.Circle,
                        DefaultDeadzoneRadius = 10,
                        DefaultLeftTriggerRigidity = 50,
                        DefaultRightTriggerRigidity = 50,
                        AutoApplySettings = true,
                        ShowControllerDebugInfo = false,
                        ControllerProfiles = new ControllerProfile[0]
                    };
                    
                    // Save default config
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                // If loading fails, create default config
                Config = new AppConfig
                {
                    DefaultDeadzoneShape = DeadzoneShape.Circle,
                    DefaultDeadzoneRadius = 10,
                    DefaultLeftTriggerRigidity = 50,
                    DefaultRightTriggerRigidity = 50,
                    AutoApplySettings = true,
                    ShowControllerDebugInfo = false,
                    ControllerProfiles = new ControllerProfile[0]
                };
                
                Console.WriteLine($"Error loading config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves configuration to file
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                // Create config directory if it doesn't exist
                if (!Directory.Exists(ConfigFolderPath))
                {
                    Directory.CreateDirectory(ConfigFolderPath);
                }
                
                // Save config to file
                using (FileStream fs = new FileStream(ConfigFilePath, FileMode.Create))
                {
                    // No need to check serializer for null since constructor doesn't return null
                    var serializer = new XmlSerializer(typeof(AppConfig));
                    serializer.Serialize(fs, Config);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Adds or updates a controller profile
        /// </summary>
        public void SaveControllerProfile(string serialNumber, string profileName, DeadzoneShape deadzoneShape, 
            int deadzoneRadius, int leftTriggerRigidity, int rightTriggerRigidity)
        {
            // Look for an existing profile for this controller
            bool found = false;
            
            for (int i = 0; i < Config.ControllerProfiles.Length; i++)
            {
                if (Config.ControllerProfiles[i].SerialNumber == serialNumber)
                {
                    // Update existing profile
                    Config.ControllerProfiles[i].ProfileName = profileName;
                    Config.ControllerProfiles[i].DeadzoneShape = deadzoneShape;
                    Config.ControllerProfiles[i].DeadzoneRadius = deadzoneRadius;
                    Config.ControllerProfiles[i].LeftTriggerRigidity = leftTriggerRigidity;
                    Config.ControllerProfiles[i].RightTriggerRigidity = rightTriggerRigidity;
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                // Add new profile - Create a temporary variable that we can modify with ref
                var profiles = Config.ControllerProfiles;
                Array.Resize(ref profiles, profiles.Length + 1);
                profiles[profiles.Length - 1] = new ControllerProfile
                {
                    SerialNumber = serialNumber,
                    ProfileName = profileName,
                    DeadzoneShape = deadzoneShape,
                    DeadzoneRadius = deadzoneRadius,
                    LeftTriggerRigidity = leftTriggerRigidity,
                    RightTriggerRigidity = rightTriggerRigidity
                };
                Config.ControllerProfiles = profiles;
            }
            // Save config
            SaveConfig();
        }
        
        /// <summary>
        /// Gets a controller profile by serial number
        /// </summary>
        public ControllerProfile? GetControllerProfile(string serialNumber)
        {
            foreach (var profile in Config.ControllerProfiles)
            {
                if (profile.SerialNumber == serialNumber)
                {
                    return profile;
                }
            }
            
            return null;
        }
    }
}