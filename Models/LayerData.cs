// =================================================================================
// File: Models/LayerData.cs
// Description: Простая модель данных (POCO) для передачи информации о слое.
// =================================================================================
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace AutoCADLayerManager.Models
{
    // Этот класс используется для передачи данных между сервисом и ViewModel,
    // чтобы избежать прямой зависимости ViewModel от объектов AutoCAD API.
    public class LayerData
    {
        public string Name { get; set; }
        public bool IsOff { get; set; }
        public AcColor Color { get; set; }
    }
}
