using System;
using System.Drawing;
using System.Windows.Forms;
using DSRL.Core.Configuration;
using DSRL.Core.Enums;
using DSRL.Core.Utilities;

namespace DSRL.UI.Forms
{
    /// <summary>
    /// Settings dialog for the application
    /// </summary>
    public class SettingsForm : Form
    {
        private ComboBox defaultDeadzoneShapeComboBox;
        private TrackBar defaultDeadzoneRadiusTrackBar;
        private TrackBar defaultLeftTriggerRigidityTrackBar;
        private TrackBar defaultRightTriggerRigidityTrackBar;
        private CheckBox autoApplySettingsCheckBox;
        private CheckBox showControllerDebugInfoCheckBox;
        private Label defaultDeadzoneRadiusValue;
        private Label defaultLeftTriggerValue;
        private Label defaultRightTriggerValue;
        
        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            // Form settings
            this.Text = "DSRL Settings";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Default deadzone shape
            Label defaultDeadzoneShapeLabel = new Label
            {
                Text = "Default Deadzone Shape:",
                Location = new Point(20, 20),
                Size = new Size(150, 20)
            };
            
            defaultDeadzoneShapeComboBox = new ComboBox
            {
                Location = new Point(180, 20),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            defaultDeadzoneShapeComboBox.Items.AddRange(new object[] { "Circle", "Square", "Cross" });
            
            // Default deadzone radius
            Label defaultDeadzoneRadiusLabel = new Label
            {
                Text = "Default Deadzone Radius:",
                Location = new Point(20, 60),
                Size = new Size(150, 20)
            };
            
            defaultDeadzoneRadiusTrackBar = new TrackBar
            {
                Location = new Point(180, 60),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 5
            };
            defaultDeadzoneRadiusTrackBar.ValueChanged += (s, e) => 
                defaultDeadzoneRadiusValue.Text = $"{defaultDeadzoneRadiusTrackBar.Value}%";
            
            defaultDeadzoneRadiusValue = new Label
            {
                Location = new Point(390, 65),
                Size = new Size(40, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Default left trigger rigidity
            Label defaultLeftTriggerLabel = new Label
            {
                Text = "Default L2 Rigidity:",
                Location = new Point(20, 120),
                Size = new Size(150, 20)
            };
            
            defaultLeftTriggerRigidityTrackBar = new TrackBar
            {
                Location = new Point(180, 120),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 5
            };
            defaultLeftTriggerRigidityTrackBar.ValueChanged += (s, e) => 
                defaultLeftTriggerValue.Text = $"{defaultLeftTriggerRigidityTrackBar.Value}%";
            
            defaultLeftTriggerValue = new Label
            {
                Location = new Point(390, 125),
                Size = new Size(40, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Default right trigger rigidity
            Label defaultRightTriggerLabel = new Label
            {
                Text = "Default R2 Rigidity:",
                Location = new Point(20, 180),
                Size = new Size(150, 20)
            };
            
            defaultRightTriggerRigidityTrackBar = new TrackBar
            {
                Location = new Point(180, 180),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 5
            };
            defaultRightTriggerRigidityTrackBar.ValueChanged += (s, e) => 
                defaultRightTriggerValue.Text = $"{defaultRightTriggerRigidityTrackBar.Value}%";
            
            defaultRightTriggerValue = new Label
            {
                Location = new Point(390, 185),
                Size = new Size(40, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Auto-apply settings
            autoApplySettingsCheckBox = new CheckBox
            {
                Text = "Automatically apply settings when changed",
                Location = new Point(20, 240),
                Size = new Size(300, 20)
            };
            
            // Show debug info
            showControllerDebugInfoCheckBox = new CheckBox
            {
                Text = "Show controller debug information",
                Location = new Point(20, 270),
                Size = new Size(300, 20)
            };
            
            // Buttons
            Button saveButton = new Button
            {
                Text = "Save",
                Location = new Point(240, 320),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            saveButton.Click += SaveButton_Click;
            
            Button cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(340, 320),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };
            cancelButton.Click += (s, e) => this.Close();
            
            // Add controls to form
            this.Controls.Add(defaultDeadzoneShapeLabel);
            this.Controls.Add(defaultDeadzoneShapeComboBox);
            this.Controls.Add(defaultDeadzoneRadiusLabel);
            this.Controls.Add(defaultDeadzoneRadiusTrackBar);
            this.Controls.Add(defaultDeadzoneRadiusValue);
            this.Controls.Add(defaultLeftTriggerLabel);
            this.Controls.Add(defaultLeftTriggerRigidityTrackBar);
            this.Controls.Add(defaultLeftTriggerValue);
            this.Controls.Add(defaultRightTriggerLabel);
            this.Controls.Add(defaultRightTriggerRigidityTrackBar);
            this.Controls.Add(defaultRightTriggerValue);
            this.Controls.Add(autoApplySettingsCheckBox);
            this.Controls.Add(showControllerDebugInfoCheckBox);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);
            
            // Set accept and cancel buttons
            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }
        
        private void LoadSettings()
        {
            // Load settings from ConfigManager
            var config = ConfigManager.Instance.Config;
            
            defaultDeadzoneShapeComboBox.SelectedIndex = (int)config.DefaultDeadzoneShape;
            defaultDeadzoneRadiusTrackBar.Value = config.DefaultDeadzoneRadius;
            defaultDeadzoneRadiusValue.Text = $"{config.DefaultDeadzoneRadius}%";
            
            defaultLeftTriggerRigidityTrackBar.Value = config.DefaultLeftTriggerRigidity;
            defaultLeftTriggerValue.Text = $"{config.DefaultLeftTriggerRigidity}%";
            
            defaultRightTriggerRigidityTrackBar.Value = config.DefaultRightTriggerRigidity;
            defaultRightTriggerValue.Text = $"{config.DefaultRightTriggerRigidity}%";
            
            autoApplySettingsCheckBox.Checked = config.AutoApplySettings;
            showControllerDebugInfoCheckBox.Checked = config.ShowControllerDebugInfo;
        }
        
        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Save settings to ConfigManager
                var config = ConfigManager.Instance.Config;
                
                config.DefaultDeadzoneShape = (DeadzoneShape)defaultDeadzoneShapeComboBox.SelectedIndex;
                config.DefaultDeadzoneRadius = defaultDeadzoneRadiusTrackBar.Value;
                config.DefaultLeftTriggerRigidity = defaultLeftTriggerRigidityTrackBar.Value;
                config.DefaultRightTriggerRigidity = defaultRightTriggerRigidityTrackBar.Value;
                config.AutoApplySettings = autoApplySettingsCheckBox.Checked;
                config.ShowControllerDebugInfo = showControllerDebugInfoCheckBox.Checked;
                
                ConfigManager.Instance.SaveConfig();
                
                Logger.Log("Application settings saved");
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving settings", ex);
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}