using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using AutoCADLayerManager.Services;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace AutoCADLayerManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AutoCADService _service;

        public ObservableCollection<LayerViewModel> Layers { get; } = new ObservableCollection<LayerViewModel>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteLayerCommand))]
        private LayerViewModel _selectedLayer;

        [ObservableProperty]
        private string _newLayerName = "НовыйСлой";

        public MainViewModel(AutoCADService service)
        {
            _service = service;
            LoadLayers();

            // Подписка на события из сервиса
            _service.LayerChanged += OnAutoCADLayerChanged;
        }

        private void OnAutoCADLayerChanged(object sender, LayerChangedEventArgs e)
        {
            // Изменения из AutoCAD приходят в фоновом потоке.
            // Нужно обновить UI в основном потоке Avalonia.
            Dispatcher.UIThread.Post(() =>
            {
                switch (e.ChangeType)
                {
                    case LayerChangeType.Added:
                        if (!Layers.Any(l => l.Name == e.LayerName))
                        {
                            Layers.Add(new LayerViewModel(e.NewData, _service));
                        }
                        break;
                    case LayerChangeType.Erased:
                        var layerToRemove = Layers.FirstOrDefault(l => l.Name == e.LayerName);
                        if (layerToRemove != null)
                        {
                            Layers.Remove(layerToRemove);
                        }
                        break;
                    case LayerChangeType.Modified:
                        var layerToUpdate = Layers.FirstOrDefault(l => l.Name == e.LayerName);
                        layerToUpdate?.Update(e.NewData);
                        break;
                }
            });
        }

        partial void OnSelectedLayerChanged(LayerViewModel value)
        {
            if (value != null)
            {
                _service.HighlightObjectsOnLayer(value.Name);
            }
        }

        private void LoadLayers()
        {
            Layers.Clear();
            var layerData = _service.GetLayers();
            foreach (var data in layerData)
            {
                Layers.Add(new LayerViewModel(data, _service));
            }
        }

        [RelayCommand]
        private void CreateLayer()
        {
            if (string.IsNullOrWhiteSpace(NewLayerName) || Layers.Any(l => l.Name.Equals(NewLayerName, System.StringComparison.OrdinalIgnoreCase)))
            {
                // Тут можно показать сообщение пользователю
                return;
            }
            var random = new System.Random();
            var color = Autodesk.AutoCAD.Colors.Color.FromRgb((byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
            _service.CreateLayer(NewLayerName, color);
            NewLayerName = "НовыйСлой"; // Сброс
        }

        private bool CanDeleteLayer() => SelectedLayer != null && SelectedLayer.Name != "0";

        [RelayCommand(CanExecute = nameof(CanDeleteLayer))]
        private void DeleteLayer()
        {
            if (SelectedLayer != null)
            {
                _service.DeleteLayer(SelectedLayer.Name);
            }
        }

        public void Cleanup()
        {
            _service.LayerChanged -= OnAutoCADLayerChanged;
            _service.Cleanup();
        }
    }
}