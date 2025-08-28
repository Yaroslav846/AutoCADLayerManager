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

        protected override void OnClosed(EventArgs e)
        {
            // �����! ������� �������� ��� �������� ����.
            if (this.DataContext is MainViewModel vm)
            {
                vm.Cleanup();
            }
            base.OnClosed(e);
            PluginEntry.ResetWindow(); // �������� �������� ������, ��� ���� �������
        }
    }
}