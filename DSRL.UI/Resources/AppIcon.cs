using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace DSRL.UI.Resources
{
    /// <summary>
    /// Helper class for accessing embedded application resources
    /// </summary>
    internal static class AppResources
    {
        /// <summary>
        /// Gets the application's main icon
        /// </summary>
        public static Icon GetAppIcon()
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Gets a resource stream from the embedded resources
        /// </summary>
        public static Stream GetResourceStream(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream($"DSRL.UI.Resources.{name}");
        }
        
        /// <summary>
        /// Gets a bitmap from the embedded resources
        /// </summary>
        public static Bitmap GetBitmap(string name)
        {
            using (Stream stream = GetResourceStream(name))
            {
                if (stream == null) return null;
                return new Bitmap(stream);
            }
        }
    }
}