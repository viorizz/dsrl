using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using DSRL.Core.Controllers;
using DSRL.Core.Configuration;
using DSRL.Core.Enums;
using DSRL.Core.Utilities;

namespace DSRL.UI.Forms
{
    public partial class MainForm : Form
    {
        // List to store connected controllers
        private List<DualSenseController> connectedControllers = new List<DualSenseController>();
        private DualSenseController selectedController = null;
        
        // UI Controls
        private ComboBox controllerSelector;
        private GroupBox deadzoneGroup;
        private GroupBox triggerGroup;
        private Panel deadzonePreview;
        private Button refreshButton;
        private TrackBar deadzoneRadiusTrackBar;
        private ComboBox deadzoneShapeComboBox;
        private TrackBar leftTriggerRigidityTrackBar;
        private TrackBar rightTriggerRigidityTrackBar;
        private Label deadzoneRadiusValue;
        private Label leftTriggerValue;
        private Label rightTriggerValue;
        private Label statusLabel;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeControllerWatcher();
            RefreshControllerList();
        }
        
        private void InitializeComponent()
        {
            // Menu bar
            MenuStrip menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;

            // File menu
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem settingsMenuItem = new ToolStripMenuItem("Settings");
            settingsMenuItem.Click += (s, e) => new SettingsForm().ShowDialog(this);
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += (s, e) => Application.Exit();
            fileMenu.DropDownItems.Add(settingsMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenuItem);

            // Help menu
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            ToolStripMenuItem helpTopicsMenuItem = new ToolStripMenuItem("Help Topics");
            helpTopicsMenuItem.Click += (s, e) => MessageBox.Show("Help documentation is not yet available.", "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ToolStripMenuItem aboutMenuItem = new ToolStripMenuItem("About");
            aboutMenuItem.Click += (s, e) => new AboutForm().ShowDialog(this);
            helpMenu.DropDownItems.Add(helpTopicsMenuItem);
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add(aboutMenuItem);

            // Add menus to menu strip
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(helpMenu);

            // Add menu strip to form
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // Form settings
            this.Text = "DSRL - DualSense Controller Manager";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // Status label
            statusLabel = new Label
            {
                Location = new Point(10, 435),
                Size = new Size(580, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Ready."
            };
            
            // Controller selector section
            Label controllerLabel = new Label
            {
                Location = new Point(10, 15),
                Size = new Size(150, 20),
                Text = "Select Controller:"
            };
            
            controllerSelector = new ComboBox
            {
                Location = new Point(160, 15),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            controllerSelector.SelectedIndexChanged += ControllerSelector_SelectedIndexChanged;
            
            refreshButton = new Button
            {
                Location = new Point(470, 15),
                Size = new Size(100, 25),
                Text = "Refresh"
            };
            refreshButton.Click += RefreshButton_Click;
            
            // Deadzone settings group
            deadzoneGroup = new GroupBox
            {
                Location = new Point(10, 50),
                Size = new Size(280, 375),
                Text = "Deadzone Settings"
            };
            
            Label deadzoneShapeLabel = new Label
            {
                Location = new Point(10, 30),
                Size = new Size(100, 20),
                Text = "Shape:"
            };
            
            deadzoneShapeComboBox = new ComboBox
            {
                Location = new Point(120, 30),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            deadzoneShapeComboBox.Items.AddRange(new object[] { "Circle", "Square", "Cross" });
            deadzoneShapeComboBox.SelectedIndex = 0; // Default to Circle
            deadzoneShapeComboBox.SelectedIndexChanged += DeadzoneShape_Changed;
            
            Label deadzoneRadiusLabel = new Label
            {
                Location = new Point(10, 65),
                Size = new Size(100, 20),
                Text = "Radius:"
            };
            
            deadzoneRadiusTrackBar = new TrackBar
            {
                Location = new Point(40, 90),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 10,
                TickFrequency = 5
            };
            deadzoneRadiusTrackBar.ValueChanged += DeadzoneRadius_Changed;
            
            deadzoneRadiusValue = new Label
            {
                Location = new Point(240, 95),
                Size = new Size(30, 20),
                Text = "10%"
            };
            
            deadzonePreview = new Panel
            {
                Location = new Point(40, 140),
                Size = new Size(200, 200),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            deadzonePreview.Paint += DeadzonePreview_Paint;
            
            // Trigger settings group
            triggerGroup = new GroupBox
            {
                Location = new Point(300, 50),
                Size = new Size(280, 375),
                Text = "Trigger Settings"
            };
            
            Label leftTriggerLabel = new Label
            {
                Location = new Point(10, 30),
                Size = new Size(150, 20),
                Text = "Left Trigger (L2) Rigidity:"
            };
            
            leftTriggerRigidityTrackBar = new TrackBar
            {
                Location = new Point(40, 60),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 5
            };
            leftTriggerRigidityTrackBar.ValueChanged += LeftTriggerRigidity_Changed;
            
            leftTriggerValue = new Label
            {
                Location = new Point(240, 65),
                Size = new Size(30, 20),
                Text = "50%"
            };
            
            Label rightTriggerLabel = new Label
            {
                Location = new Point(10, 120),
                Size = new Size(150, 20),
                Text = "Right Trigger (R2) Rigidity:"
            };
            
            rightTriggerRigidityTrackBar = new TrackBar
            {
                Location = new Point(40, 150),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 5
            };
            rightTriggerRigidityTrackBar.ValueChanged += RightTriggerRigidity_Changed;
            
            rightTriggerValue = new Label
            {
                Location = new Point(240, 155),
                Size = new Size(30, 20),
                Text = "50%"
            };
            
            // Visual feedback panel for triggers
            Panel leftTriggerPreview = new Panel
            {
                Location = new Point(40, 210),
                Size = new Size(30, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGray
            };
            
            Panel rightTriggerPreview = new Panel
            {
                Location = new Point(180, 210),
                Size = new Size(30, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGray
            };
            
            // Add controls to form
            this.Controls.Add(controllerLabel);
            this.Controls.Add(controllerSelector);
            this.Controls.Add(refreshButton);
            this.Controls.Add(deadzoneGroup);
            this.Controls.Add(triggerGroup);
            this.Controls.Add(statusLabel);
            
            // Add controls to deadzone group
            deadzoneGroup.Controls.Add(deadzoneShapeLabel);
            deadzoneGroup.Controls.Add(deadzoneShapeComboBox);
            deadzoneGroup.Controls.Add(deadzoneRadiusLabel);
            deadzoneGroup.Controls.Add(deadzoneRadiusTrackBar);
            deadzoneGroup.Controls.Add(deadzoneRadiusValue);
            deadzoneGroup.Controls.Add(deadzonePreview);
            
            // Add controls to trigger group
            triggerGroup.Controls.Add(leftTriggerLabel);
            triggerGroup.Controls.Add(leftTriggerRigidityTrackBar);
            triggerGroup.Controls.Add(leftTriggerValue);
            triggerGroup.Controls.Add(rightTriggerLabel);
            triggerGroup.Controls.Add(rightTriggerRigidityTrackBar);
            triggerGroup.Controls.Add(rightTriggerValue);
            triggerGroup.Controls.Add(leftTriggerPreview);
            triggerGroup.Controls.Add(rightTriggerPreview);
            
            // Set initial UI state
            UpdateUIState(false);
        }
        
        private void InitializeControllerWatcher()
        {
            // Start a thread to watch for controller connections/disconnections
            Thread watcherThread = new Thread(() =>
            {
                while (true)
                {
                    // Check for controller changes every second
                    Thread.Sleep(1000);
                    
                    // Logic to detect controller changes
                    // This would normally use a controller API
                    try
                    {
                        // Only refresh if there's a change to avoid UI flicker
                        bool changes = false;
                        
                        // Advanced implementation would detect actual changes here
                        
                        if (changes)
                        {
                            this.Invoke(new Action(() =>
                            {
                                RefreshControllerList();
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error in controller watcher", ex);
                    }
                }
            });
            
            watcherThread.IsBackground = true;
            watcherThread.Start();
        }
        
        private void RefreshControllerList()
        {
            controllerSelector.Items.Clear();
            connectedControllers.Clear();
            
            try
            {
                // Detect connected DualSense controllers
                Logger.Log("Refreshing controller list...");
                
                // Use the DualSenseAPI to detect controllers
                connectedControllers = DualSenseAPI.GetConnectedControllers();
                
                // Load saved profiles for each controller
                foreach (var controller in connectedControllers)
                {
                    var profile = ConfigManager.Instance.GetControllerProfile(controller.SerialNumber);
                    if (profile != null)
                    {
                        // Apply saved profile settings
                        controller.DeadzoneShape = profile.DeadzoneShape;
                        controller.DeadzoneRadius = profile.DeadzoneRadius;
                        controller.LeftTriggerRigidity = profile.LeftTriggerRigidity;
                        controller.RightTriggerRigidity = profile.RightTriggerRigidity;
                    }
                    else
                    {
                        // Apply default settings
                        controller.DeadzoneShape = ConfigManager.Instance.Config.DefaultDeadzoneShape;
                        controller.DeadzoneRadius = ConfigManager.Instance.Config.DefaultDeadzoneRadius;
                        controller.LeftTriggerRigidity = ConfigManager.Instance.Config.DefaultLeftTriggerRigidity;
                        controller.RightTriggerRigidity = ConfigManager.Instance.Config.DefaultRightTriggerRigidity;
                    }
                }
                
                if (connectedControllers.Count > 0)
                {
                    foreach (var controller in connectedControllers)
                    {
                        controllerSelector.Items.Add($"DualSense Controller ({controller.SerialNumber})");
                    }
                    
                    controllerSelector.SelectedIndex = 0;
                    statusLabel.Text = $"Found {connectedControllers.Count} controller(s).";
                    Logger.Log($"Found {connectedControllers.Count} controller(s)");
                }
                else
                {
                    statusLabel.Text = "No DualSense controllers detected. Connect a controller and click Refresh.";
                    UpdateUIState(false);
                    Logger.Log("No controllers detected");
                    
                    // If we're debugging, add a mock controller
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        connectedControllers.Add(new DualSenseController
                        {
                            SerialNumber = "DEMO123456",
                            IsConnected = true,
                            DeadzoneShape = DeadzoneShape.Circle,
                            DeadzoneRadius = 10,
                            LeftTriggerRigidity = 50,
                            RightTriggerRigidity = 50
                        });
                        
                        controllerSelector.Items.Add($"DualSense Controller (DEMO123456) [DEBUG]");
                        controllerSelector.SelectedIndex = 0;
                        statusLabel.Text = "Debug mode: Using mock controller.";
                        Logger.Log("Added debug mock controller");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error detecting controllers", ex);
                MessageBox.Show($"Error detecting controllers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error detecting controllers.";
            }
        }
        
        private void UpdateUIState(bool enabled)
        {
            deadzoneGroup.Enabled = enabled;
            triggerGroup.Enabled = enabled;
            
            if (enabled && selectedController != null)
            {
                // Update UI to reflect current controller settings
                deadzoneShapeComboBox.SelectedIndex = (int)selectedController.DeadzoneShape;
                deadzoneRadiusTrackBar.Value = selectedController.DeadzoneRadius;
                deadzoneRadiusValue.Text = $"{selectedController.DeadzoneRadius}%";
                
                leftTriggerRigidityTrackBar.Value = selectedController.LeftTriggerRigidity;
                leftTriggerValue.Text = $"{selectedController.LeftTriggerRigidity}%";
                
                rightTriggerRigidityTrackBar.Value = selectedController.RightTriggerRigidity;
                rightTriggerValue.Text = $"{selectedController.RightTriggerRigidity}%";
                
                deadzonePreview.Invalidate(); // Redraw the deadzone preview
            }
        }
        
        private void ControllerSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (controllerSelector.SelectedIndex >= 0)
            {
                selectedController = connectedControllers[controllerSelector.SelectedIndex];
                UpdateUIState(true);
                statusLabel.Text = $"Controller '{selectedController.SerialNumber}' selected.";
            }
            else
            {
                selectedController = null;
                UpdateUIState(false);
            }
        }
        
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "Refreshing controller list...";
            RefreshControllerList();
        }
        
        private void DeadzoneShape_Changed(object sender, EventArgs e)
        {
            if (selectedController != null)
            {
                selectedController.DeadzoneShape = (DeadzoneShape)deadzoneShapeComboBox.SelectedIndex;
                ApplySettings();
                deadzonePreview.Invalidate(); // Redraw the deadzone preview
            }
        }
        
        private void DeadzoneRadius_Changed(object sender, EventArgs e)
        {
            if (selectedController != null)
            {
                selectedController.DeadzoneRadius = deadzoneRadiusTrackBar.Value;
                deadzoneRadiusValue.Text = $"{selectedController.DeadzoneRadius}%";
                ApplySettings();
                deadzonePreview.Invalidate(); // Redraw the deadzone preview
            }
        }
        
        private void LeftTriggerRigidity_Changed(object sender, EventArgs e)
        {
            if (selectedController != null)
            {
                selectedController.LeftTriggerRigidity = leftTriggerRigidityTrackBar.Value;
                leftTriggerValue.Text = $"{selectedController.LeftTriggerRigidity}%";
                ApplySettings();
            }
        }
        
        private void RightTriggerRigidity_Changed(object sender, EventArgs e)
        {
            if (selectedController != null)
            {
                selectedController.RightTriggerRigidity = rightTriggerRigidityTrackBar.Value;
                rightTriggerValue.Text = $"{selectedController.RightTriggerRigidity}%";
                ApplySettings();
            }
        }
        
        private void ApplySettings()
        {
            if (selectedController != null)
            {
                try
                {
                    // Apply settings to the actual controller
                    if (selectedController.ApplySettings())
                    {
                        // Save the settings to the controller's profile
                        ConfigManager.Instance.SaveControllerProfile(
                            selectedController.SerialNumber,
                            $"Profile for {selectedController.SerialNumber}",
                            selectedController.DeadzoneShape,
                            selectedController.DeadzoneRadius,
                            selectedController.LeftTriggerRigidity,
                            selectedController.RightTriggerRigidity
                        );
                        
                        statusLabel.Text = $"Applied settings to controller '{selectedController.SerialNumber}'.";
                        Logger.Log($"Applied settings to controller {selectedController.SerialNumber}");
                    }
                    else
                    {
                        statusLabel.Text = "Failed to apply settings to controller.";
                        Logger.Log($"Failed to apply settings to controller {selectedController.SerialNumber}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error applying settings", ex);
                    MessageBox.Show($"Error applying settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Error applying settings to controller.";
                }
            }
        }
        
        private void DeadzonePreview_Paint(object sender, PaintEventArgs e)
        {
            if (selectedController == null) return;
            
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            
            int width = deadzonePreview.Width;
            int height = deadzonePreview.Height;
            int centerX = width / 2;
            int centerY = height / 2;
            
            // Full joystick range
            g.DrawEllipse(Pens.LightGray, 0, 0, width - 1, height - 1);
            
            // Deadzone visualization
            float radiusPercentage = selectedController.DeadzoneRadius / 100.0f;
            int deadzoneSize = (int)(Math.Min(width, height) * radiusPercentage);
            
            Brush deadzoneBrush = new SolidBrush(Color.FromArgb(80, Color.Red));
            
            switch (selectedController.DeadzoneShape)
            {
                case DeadzoneShape.Circle:
                    g.FillEllipse(deadzoneBrush, centerX - deadzoneSize / 2, centerY - deadzoneSize / 2, deadzoneSize, deadzoneSize);
                    break;
                case DeadzoneShape.Square:
                    g.FillRectangle(deadzoneBrush, centerX - deadzoneSize / 2, centerY - deadzoneSize / 2, deadzoneSize, deadzoneSize);
                    break;
                case DeadzoneShape.Cross:
                    int crossWidth = deadzoneSize / 3;
                    g.FillRectangle(deadzoneBrush, centerX - crossWidth / 2, centerY - deadzoneSize / 2, crossWidth, deadzoneSize);
                    g.FillRectangle(deadzoneBrush, centerX - deadzoneSize / 2, centerY - crossWidth / 2, deadzoneSize, crossWidth);
                    break;
            }
            
            // Current joystick position (center for now)
            g.FillEllipse(Brushes.Blue, centerX - 5, centerY - 5, 10, 10);
        }
    }
}