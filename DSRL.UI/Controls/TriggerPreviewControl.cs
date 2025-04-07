using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace DSRL.UI.Controls
{
    /// <summary>
    /// Custom control for visualizing trigger resistance
    /// </summary>
    public class TriggerPreviewControl : Control
    {
        [Category("Trigger")]
        [Description("The rigidity percentage of the trigger (0-100)")]
        [DefaultValue(50)]
        public int RigidityPercentage { get; set; } = 50;
        
        [Category("Trigger")]
        [Description("Indicates whether this is a left trigger (L2) or right trigger (R2)")]
        [DefaultValue(true)]
        public bool IsLeftTrigger { get; set; } = true;
        
        public TriggerPreviewControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint, true);
            
            BackColor = Color.White;
            Size = new Size(30, 100);
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            
            int width = Width;
            int height = Height;
            
            // Draw trigger outline
            g.DrawRectangle(Pens.Gray, 0, 0, width - 1, height - 1);
            
            // Convert rigidity (0-100) to resistance visualization
            int filledHeight = (int)(height * (RigidityPercentage / 100.0));
            
            // Create gradient brush for resistance visualization
            using (LinearGradientBrush brush = new LinearGradientBrush(
                new Point(0, height),
                new Point(0, height - filledHeight),
                Color.LightBlue,
                Color.Blue))
            {
                g.FillRectangle(brush, 1, height - filledHeight, width - 2, filledHeight - 1);
            }
            
            // Draw trigger label
            string label = IsLeftTrigger ? "L2" : "R2";
            using (StringFormat format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(label, Font, Brushes.Black, new RectangleF(0, 0, width, 20), format);
            }
            
            // Draw rigidity percentage
            using (StringFormat format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Far
            })
            {
                g.DrawString($"{RigidityPercentage}%", Font, Brushes.Black, new RectangleF(0, 0, width, height - 5), format);
            }
        }
    }
}