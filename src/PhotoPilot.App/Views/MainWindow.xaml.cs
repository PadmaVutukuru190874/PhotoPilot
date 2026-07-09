using System;
using System.Windows;
using Microsoft.Win32;

namespace PhotoPilot.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SelectPhotoFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Photo Folder",
                Multiselect = false
            };

            bool? result = dialog.ShowDialog(this);

            if (result == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                SelectedFolderText.Text = dialog.FolderName;
                StatusText.Text = "Folder selected";
            }
            else
            {
                StatusText.Text = "Folder selection cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error selecting folder";
            MessageBox.Show(
                this,
                ex.Message,
                "Folder Selection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}