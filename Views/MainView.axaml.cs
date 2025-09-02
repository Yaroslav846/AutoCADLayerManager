// =================================================================================
// File: Views/MainView.axaml.cs
// Description: Code-behind для главного окна. Реализует скрытие вместо закрытия.
// =================================================================================
using Avalonia.Controls;
using AutoCADLayerManager.ViewModels;
using System;

namespace AutoCADLayerManager.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

        // Переопределяем метод OnClosing, который вызывается ПЕРЕД закрытием окна
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ:
            // 1. Отменяем стандартное событие закрытия
            e.Cancel = true;

            // 2. Вместо этого просто скрываем окно
            this.Hide();

            // Мы не вызываем vm.Cleanup(), так как хотим, чтобы ViewModel
            // продолжал отслеживать события AutoCAD, пока окно скрыто.
            base.OnClosing(e);
        }

        // Этот метод будет вызван только тогда, когда приложение Avalonia
        // будет полностью завершено (при выгрузке плагина).
        protected override void OnClosed(EventArgs e)
        {
            // Здесь происходит финальная очистка ресурсов ViewModel
            if (this.DataContext is MainViewModel vm)
            {
                vm.Cleanup();
            }
            base.OnClosed(e);
        }
    }
}
