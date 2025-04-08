# DSRL - DualSense Response Loader

DSRL is an application designed to enhance your PlayStation DualSense controller experience on PC by allowing you to customize the controller's behavior. With DSRL, you can adjust deadzone shapes and sizes for joysticks, as well as customize trigger rigidity to match your play style across different games.

## 🎮 Features

- **Controller Detection**: Automatically detects connected DualSense controllers via USB or Bluetooth
- **Custom Deadzones**: Configure deadzone shapes (Circle, Square, Cross) and sizes for precise joystick control
- **Adaptive Trigger Settings**: Adjust rigidity for left and right triggers independently
- **Profile Management**: Create and save multiple profiles for different games or users
- **Auto Apply Settings**: Option to automatically apply saved settings when a controller is connected
- **Diagnostic Tools**: Built-in tools to troubleshoot controller connectivity issues

## 🛠️ System Requirements

- Windows operating system
- .NET 9.0 or higher
- DualSense controller (PS5 controller)
- USB or Bluetooth connection capability

## 📋 Installation

1. Download the latest release from the [Releases](https://github.com/yourusername/dsrl/releases) page
2. Extract the ZIP file to a location of your choice
3. Run `DSRL.exe` to launch the application

## 🚀 Quick Start

1. Connect your DualSense controller via USB or Bluetooth
2. Launch DSRL
3. The application will detect your controller automatically
4. Adjust deadzone and trigger settings to your preference
5. Click "Apply Settings" to apply changes to your controller
6. (Optional) Save your settings as a profile for future use

## ⚙️ Configuration

### Deadzone Settings

- **Shape**: Choose between Circle (default), Square, or Cross
- **Radius**: Adjust the size of the deadzone (0-100%)

### Trigger Settings

- **Left Trigger Rigidity**: Adjust resistance for the left trigger (0-100%)
- **Right Trigger Rigidity**: Adjust resistance for the right trigger (0-100%)

### Application Settings

- **Auto Apply Settings**: When enabled, automatically applies saved settings when a controller is connected
- **Show Controller Debug Info**: Displays technical information about your controller

## 🔧 Troubleshooting

If your controller is not being detected:

1. Make sure your controller is properly connected via USB or Bluetooth
2. Check if your controller is recognized in Windows device manager
3. Try using a different USB port or reconnecting via Bluetooth
4. Run the built-in diagnostic tool by clicking "Generate Diagnostic Report" in the settings menu
5. Check the log file located at `%AppData%\DSRL\dsrl_log.txt`

## 🧩 Technical Information

DSRL communicates with your DualSense controller through HID (Human Interface Device) protocols, using the HidSharp library to access low-level controller functions. The application stores its configuration in:

```
%AppData%\DSRL\settings.xml
```

## 📝 License

[MIT License](LICENSE)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 🙏 Acknowledgments

- [HidSharp](https://github.com/IntergatedCircuits/HidSharp) - HID library for .NET
- PlayStation and DualSense are trademarks of Sony Interactive Entertainment Inc.

---

*This software is not affiliated with or endorsed by Sony Interactive Entertainment Inc.*
