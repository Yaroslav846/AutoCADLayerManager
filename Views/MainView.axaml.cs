// =================================================================================
// File: Views/MainView.axaml.cs
// Description: Code-behind ��� �������� ����. ��������� ������� ������ ��������.
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

        // �������������� ����� OnClosing, ������� ���������� ����� ��������� ����
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // �������� ���������:
            // 1. �������� ����������� ������� ��������
            e.Cancel = true;

            // 2. ������ ����� ������ �������� ����
            this.Hide();

            // �� �� �������� vm.Cleanup(), ��� ��� �����, ����� ViewModel
            // ��������� ����������� ������� AutoCAD, ���� ���� ������.
            base.OnClosing(e);
        }

        // ���� ����� ����� ������ ������ �����, ����� ���������� Avalonia
        // ����� ��������� ��������� (��� �������� �������).
        protected override void OnClosed(EventArgs e)
        {
            // ����� ���������� ��������� ������� �������� ViewModel
            if (this.DataContext is MainViewModel vm)
            {
                vm.Cleanup();
            }
            base.OnClosed(e);
        }
    }
}
