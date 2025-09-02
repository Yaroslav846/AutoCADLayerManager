// =================================================================================
// File: PluginEntry.cs
// Description: Точка входа плагина, регистрация команды AutoCAD и запуск UI.
// =================================================================================
using Autodesk.AutoCAD.Runtime;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using AutoCADLayerManager.Services;
using AutoCADLayerManager.ViewModels;
using AutoCADLayerManager.Views;
using System;
using System.Threading;

// Регистрация сборки для AutoCAD и определение класса с командами
[assembly: CommandClass(typeof(AutoCADLayerManager.PluginEntry))]
[assembly: ExtensionApplication(typeof(AutoCADLayerManager.PluginEntry))]

namespace AutoCADLayerManager
{
    // Класс App теперь отвечает только за инициализацию темы и создание окна
    class App : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            this.Styles.Add(new FluentTheme());

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Сохраняем ссылку на жизненный цикл приложения для дальнейшего использования
                PluginEntry.SetLifetime(desktop);

                var service = new AutoCADService();
                var viewModel = new MainViewModel(service);

                desktop.MainWindow = new MainView
                {
                    DataContext = viewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }

    public class PluginEntry : IExtensionApplication
    {
        private static IClassicDesktopStyleApplicationLifetime _appLifetime;
        private static Thread _avaloniaThread;

        // Вызывается при загрузке DLL в AutoCAD
        public void Initialize() { }

        // Вызывается при выгрузке DLL из AutoCAD. Здесь мы корректно завершаем Avalonia.
        public void Terminate()
        {
            _appLifetime?.Shutdown(0);
        }

        [CommandMethod("LAYERUI")]
        public static void ShowLayerUI()
        {
            // Если окно уже существует (даже если скрыто), просто показываем его
            if (_appLifetime?.MainWindow != null)
            {
                _appLifetime.MainWindow.Show();
                _appLifetime.MainWindow.Activate();
                return;
            }

            // Предотвращаем запуск нового потока, если старый еще не завершился
            if (_avaloniaThread != null && _avaloniaThread.IsAlive)
            {
                return;
            }

            // Первый запуск: создаем поток и запускаем приложение Avalonia
            _avaloniaThread = new Thread(() =>
            {
                try
                {
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(null);
                }
                catch (System.Exception ex)
                {
                   Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Editor
                        .WriteMessage($"\nОшибка запуска Avalonia: {ex.Message}");
                }
            });
            _avaloniaThread.SetApartmentState(ApartmentState.STA);
            _avaloniaThread.Start();
        }

        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        // Статический метод для сохранения ссылки на жизненный цикл из класса App
        public static void SetLifetime(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            _appLifetime = lifetime;
        }
    }
}

