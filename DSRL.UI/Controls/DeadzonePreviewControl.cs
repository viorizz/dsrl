using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using DSRL.Core.Enums;

namespace DSRL.UI.Controls
{
    /// <summary>
    /// Custom control for visualizing controller deadzone
    /// </summary>
    public class DeadzonePreviewControl : Control
    {
        [Category("Deadzone")]
        [Description("The shape of the deadzone")]
        [DefaultValue(DeadzoneShape.Circle)]
        public DeadzoneShape Shape { get; set; } = DeadzoneShape.Circle;
        
        [Category("Deadzone")]
        [Description("The radius of the deadzone as a percentage (0-100)")]
        [DefaultValue(10)]
        public int RadiusPercentage { get; set; } = 10;
        
        [Category("Joystick")]
        [Description("The current position of the joystick")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Point JoystickPosition { get; set; }
        
        public DeadzonePreviewControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint, true);
            
            BackColor = Color.White;
            Size = new Size(200, 200);
            JoystickPosition = new Point(100, 100); // Center
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            
            int width = Width;
            int height = Height;
            int centerX = width / 2;
            int centerY = height / 2;
            
            // Full joystick range
            g.DrawEllipse(Pens.LightGray, 0, 0, width - 1, height - 1);
            
            // Deadzone visualization
            float radiusPercentage = RadiusPercentage / 100.0f;
            int deadzoneSize = (int)(Math.Min(width, height) * radiusPercentage);
            
            Brush deadzoneBrush = new SolidBrush(Color.FromArgb(80, Color.Red));
            
            switch (Shape)
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
            
            // Joystick position
            g.FillEllipse(Brushes.Blue, JoystickPosition.X - 5, JoystickPosition.Y - 5, 10, 10);
        }
        
        public void UpdateJoystickPosition(int x, int y)
        {
            // Convert from -100,100 range to control coordinates
            int scaledX = (int)(Width * (x + 100) / 200.0);
            int scaledY = (int)(Height * (y + 100) / 200.0);
            
            JoystickPosition = new Point(scaledX, scaledY);
            Invalidate();
        }
    }
}