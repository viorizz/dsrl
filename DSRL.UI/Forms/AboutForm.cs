using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace DSRL.UI.Forms
{
    /// <summary>
    /// About dialog for the application
    /// </summary>
    public class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            // Form settings
            this.Text = "About DSRL";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Get application version
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = $"Version {version.Major}.{version.Minor}.{version.Build}";
            
            // Application logo/icon (if available)
            PictureBox logoBox = new PictureBox
            {
                Size = new Size(64, 64),
                Location = new Point(20, 20),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            
            // Try to load app icon
            try
            {
                logoBox.Image = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location).ToBitmap();
            }
            catch
            {
                // Fallback if icon loading fails
                logoBox.BackColor = Color.LightGray;
            }
            
            // Title label
            Label titleLabel = new Label
            {
                Text = "DSRL - DualSense Controller Manager",
                Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
                Location = new Point(100, 20),
                Size = new Size(280, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Version label
            Label versionLabel = new Label
            {
                Text = versionText,
                Location = new Point(100, 50),
                Size = new Size(280, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Description
            Label descriptionLabel = new Label
            {
                Text = "A lightweight application for Windows which allows you to easily manipulate your DualSense controller deadzone and trigger rigidity.",
                Location = new Point(20, 100),
                Size = new Size(360, 60),
                TextAlign = ContentAlignment.TopLeft
            };
            
            // Copyright info
            Label copyrightLabel = new Label
            {
                Text = "© " + DateTime.Now.Year.ToString() + " Your Name",
                Location = new Point(20, 170),
                Size = new Size(360, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Close button
            Button closeButton = new Button
            {
                Text = "OK",
                Location = new Point(300, 230),
                Size = new Size(80, 25),
                DialogResult = DialogResult.OK
            };
            closeButton.Click += (sender, e) => this.Close();
            
            // Add controls to form
            this.Controls.Add(logoBox);
            this.Controls.Add(titleLabel);
            this.Controls.Add(versionLabel);
            this.Controls.Add(descriptionLabel);
            this.Controls.Add(copyrightLabel);
            this.Controls.Add(closeButton);
            
            // Set as the accept button
            this.AcceptButton = closeButton;
        }
    }
}