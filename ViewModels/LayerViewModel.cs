using AutoCADLayerManager.Models;
using AutoCADLayerManager.Services;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Linq;
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace AutoCADLayerManager.ViewModels
{
    public partial class LayerViewModel : ObservableObject
    {
        private readonly AutoCADService _service;

        [ObservableProperty]
        private string _name;

        private bool _isOff;
        public bool IsOff
        {
            get => _isOff;
            set
            {
                if (SetProperty(ref _isOff, value))
                {
                    _service.UpdateLayerProperty(Name, ltr => ltr.IsOff = value);
                }
            }
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set
            {
                if (SetProperty(ref _color, value))
                {
                    _service.UpdateLayerProperty(Name, ltr => ltr.Color = Converters.ColorConverter.ToAutoCADColor(value));
                }
            }
        }

        public LayerViewModel(LayerData layerData, AutoCADService service)
        {
            _name = layerData.Name;
            _isOff = layerData.IsOff;
            _color = Converters.ColorConverter.ToAvaloniaColor(layerData.Color);
            _service = service;
        }

        // Метод для обновления свойств из AutoCAD
        public void Update(LayerData data)
        {
            SetProperty(ref _isOff, data.IsOff, nameof(IsOff));

            var newColor = Converters.ColorConverter.ToAvaloniaColor(data.Color);
            SetProperty(ref _color, newColor, nameof(Color));
        }
    }
}