// =================================================================================
// File: PluginEntry.cs
// Description: Точка входа плагина, регистрация команды AutoCAD и запуск UI.
// =================================================================================
using AutoCADLayerManager.Services;
using AutoCADLayerManager.ViewModels;
using AutoCADLayerManager.Views;
using Autodesk.AutoCAD.Runtime;
using Avalonia;
using Avalonia.Controls;
using System.Threading;

// Регистрация сборки для AutoCAD и определение класса с командами
[assembly: CommandClass(typeof(AutoCADLayerManager.PluginEntry))]
[assembly: ExtensionApplication(typeof(AutoCADLayerManager.PluginEntry))]

namespace AutoCADLayerManager
{
    public class PluginEntry : IExtensionApplication
    {
        private static MainView _mainWindow;
        private static Thread _avaloniaThread;

        // Метод, вызываемый при загрузке плагина в AutoCAD
        public void Initialize()
        {
            // Здесь можно добавить логику инициализации, если она потребуется
        }

        // Метод, вызываемый при выгрузке плагина из AutoCAD
        public void Terminate()
        {
            // Здесь можно добавить логику очистки ресурсов
        }

        // Регистрация команды "LAYERUI" для вызова из командной строки AutoCAD
        [CommandMethod("LAYERUI")]
        public static void ShowLayerUI()
        {
            // Если окно уже открыто, просто активируем его
            if (_mainWindow != null)
            {
                _mainWindow.Activate();
                return;
            }

            // Avalonia UI должен работать в отдельном потоке с состоянием STA (Single-Threaded Apartment),
            // чтобы не блокировать основной поток AutoCAD и корректно обрабатывать события UI.
            _avaloniaThread = new Thread(() =>
            {
                try
                {
                    var builder = BuildAvaloniaApp().SetupWithoutStarting();
                    var app = builder.Instance;

                    AppMain(app, null);
                }
                catch (System.Exception ex)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor
                         .WriteMessage($"\nОшибка запуска Avalonia: {ex.Message}");
                }
            });
            _avaloniaThread.SetApartmentState(ApartmentState.STA);
            _avaloniaThread.Start();
        }

        // Основной метод для запуска приложения Avalonia
        private static void AppMain(Application app, string[] args)
        {
            // Создаем зависимости: сервис для работы с AutoCAD и ViewModel
            var service = new AutoCADService();
            var viewModel = new MainViewModel(service);

            _mainWindow = new MainView
            {
                DataContext = viewModel
            };

            // Запускаем цикл обработки сообщений для окна Avalonia
            app.Run(_mainWindow);

            // Когда окно закрывается, поток завершается.
            // Ссылка _mainWindow будет сброшена в null в событии OnClosed окна.
        }

        // Конфигурация и сборка приложения Avalonia
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace(); // Убран вызов .WithInterFont()

        // Статический метод, который вызывается из окна при его закрытии,
        // чтобы сбросить статическую ссылку и позволить GC собрать ресурсы.
        public static void ResetWindow()
        {
            _mainWindow = null;
        }
    }

    // Минимально необходимый класс App для инициализации Avalonia
    class App : Avalonia.Application { }
}
