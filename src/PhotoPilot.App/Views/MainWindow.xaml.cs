using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace PhotoPilot.App.Views;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> SupportedMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".gif",
        ".tif",
        ".tiff",
        ".webp",
        ".heic",
        ".mp4",
        ".mov",
        ".avi",
        ".mkv",
        ".wmv"
    };

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

            if (result != true || string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                StatusText.Text = "Folder selection cancelled";
                return;
            }

            string selectedFolder = dialog.FolderName;
            int mediaFileCount = CountSupportedMediaFiles(selectedFolder);

            if (mediaFileCount == 0)
            {
                SelectedFolderText.Text = "No valid folder selected";
                MediaCountText.Text = "Media files found: 0";
                StatusText.Text = "Selected folder does not contain supported media files";

                MessageBox.Show(
                    this,
                    "The selected folder does not contain any supported photo or video files.",
                    "No Supported Media Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            SelectedFolderText.Text = selectedFolder;
            MediaCountText.Text = $"Media files found: {mediaFileCount}";
            StatusText.Text = "Valid photo folder selected";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText.Text = "Access denied to selected folder";

            MessageBox.Show(
                this,
                "PhotoPilot could not access one or more folders because permission was denied.",
                "Access Denied",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
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

    private static int CountSupportedMediaFiles(string folderPath)
    {
        return Directory
            .EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Count(file => SupportedMediaExtensions.Contains(Path.GetExtension(file)));
    }
}