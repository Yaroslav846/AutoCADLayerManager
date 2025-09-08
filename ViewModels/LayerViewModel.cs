using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using AutoCADLayerManager.Services;
using AutoCADLayerManager.Models;
using System.Collections.Generic;

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

        // НОВЫЙ СПИСОК: Набор готовых цветов для выпадающего меню
        public List<Color> PredefinedColors { get; } = new List<Color>
        {
            Colors.Red, Colors.Lime, Colors.Blue, Colors.Yellow, Colors.Cyan, Colors.Magenta,
            Colors.White, Colors.DarkGray, Colors.Orange, Colors.Purple, Colors.Brown, Colors.CornflowerBlue
        };
        
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

        // ИЗМЕНЕНА ЛОГИКА: Старая команда OpenColorPicker заменена на эту
        [RelayCommand]
        private void SetColor(Color newColor)
        {
            // Просто устанавливаем новый цвет. 
            // Свойство Color само вызовет обновление в AutoCAD.
            this.Color = newColor;
        }
    }
}

