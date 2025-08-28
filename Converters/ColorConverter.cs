using Avalonia.Media;
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace AutoCADLayerManager.Converters
{
    public static class ColorConverter
    {
        public static Color ToAvaloniaColor(AcColor acColor)
        {
            if (acColor.IsByAci) // Indexed color
            {
                var systemColor = System.Drawing.Color.FromArgb(acColor.ColorValue.R, acColor.ColorValue.G, acColor.ColorValue.B);
                return Color.FromRgb(systemColor.R, systemColor.G, systemColor.B);
            }
            return Color.FromRgb(acColor.Red, acColor.Green, acColor.Blue);
        }

        public static AcColor ToAutoCADColor(Color avaloniaColor)
        {
            return AcColor.FromRgb(avaloniaColor.R, avaloniaColor.G, avaloniaColor.B);
        }
    }
}