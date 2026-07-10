using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using PhotoPilot.Scanner;

namespace PhotoPilot.App.Views;

public partial class MainWindow : Window
{
    private static readonly HashSet<string> SupportedMediaExtensions =
        new(StringComparer.OrdinalIgnoreCase)
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

    private readonly IMediaScanner _mediaScanner;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private string? _selectedFolderPath;

    public MainWindow()
    {
        InitializeComponent();

        _mediaScanner = new FileSystemMediaScanner();
    }

    private void SelectPhotoFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Photo Folder",
                Multiselect = false
            };

            bool? result = dialog.ShowDialog(this);

            if (result != true ||
                string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                StatusText.Text = "Folder selection cancelled";
                return;
            }

            string selectedFolder = dialog.FolderName;

            bool containsSupportedMedia =
                ContainsSupportedMedia(selectedFolder);

            if (!containsSupportedMedia)
            {
                _selectedFolderPath = null;

                SelectedFolderText.Text =
                    "No valid folder selected";

                StartScanButton.IsEnabled = false;

                StatusText.Text =
                    "The selected folder contains no supported media";

                MessageBox.Show(
                    this,
                    "The selected folder does not contain any supported photo or video files.",
                    "No Supported Media Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _selectedFolderPath = selectedFolder;

            SelectedFolderText.Text = selectedFolder;
            StartScanButton.IsEnabled = true;
            StatusText.Text = "Valid media folder selected";

            ResetScanDisplay();
        }
        catch (UnauthorizedAccessException)
        {
            ShowAccessDeniedMessage();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(
                "Folder Selection Error",
                ex.Message);
        }
    }

    private async void StartScan_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFolderPath))
        {
            return;
        }

        SetScanningState(true);
        ResetScanDisplay();

        _scanCancellationTokenSource?.Dispose();
        _scanCancellationTokenSource =
            new CancellationTokenSource();

        var options = new ScanOptions
        {
            IncludeSubfolders = true,
            IgnoreHiddenFiles = true
        };

        var progress =
            new Progress<ScanProgress>(UpdateScanProgress);

        try
        {
            StatusText.Text = "Scanning media files...";
            ScanProgressBar.IsIndeterminate = true;

            ScanSummary summary =
                await _mediaScanner.ScanAsync(
                    _selectedFolderPath,
                    options,
                    progress,
                    _scanCancellationTokenSource.Token);

            ScanProgressBar.IsIndeterminate = false;

            DisplayScanSummary(summary);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Scan cancelled";
            CurrentFileText.Text = "Scan cancelled";
        }
        catch (UnauthorizedAccessException)
        {
            ShowAccessDeniedMessage();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(
                "Scan Error",
                ex.Message);
        }
        finally
        {
            ScanProgressBar.IsIndeterminate = false;
            SetScanningState(false);
        }
    }

    private void CancelScan_Click(
        object sender,
        RoutedEventArgs e)
    {
        CancelScanButton.IsEnabled = false;
        StatusText.Text = "Cancelling scan...";

        _scanCancellationTokenSource?.Cancel();
    }

    private void UpdateScanProgress(
        ScanProgress progress)
    {
        ProcessedCountText.Text =
            progress.FilesProcessed.ToString();

        PhotoCountText.Text =
            progress.PhotoCount.ToString();

        VideoCountText.Text =
            progress.VideoCount.ToString();

        UnsupportedCountText.Text =
            progress.UnsupportedCount.ToString();

        CurrentFileText.Text =
            progress.CurrentFile ?? string.Empty;
    }

    private void DisplayScanSummary(
        ScanSummary summary)
    {
        ProcessedCountText.Text =
            summary.FilesProcessed.ToString();

        PhotoCountText.Text =
            summary.PhotoCount.ToString();

        VideoCountText.Text =
            summary.VideoCount.ToString();

        UnsupportedCountText.Text =
            summary.UnsupportedCount.ToString();

        if (summary.WasCancelled)
        {
            StatusText.Text = "Scan cancelled";
            CurrentFileText.Text = "Scan cancelled";
            return;
        }

        StatusText.Text =
            $"Scan completed in {summary.Duration.TotalSeconds:F1} seconds";

        CurrentFileText.Text =
            $"Completed. Access denied folders: {summary.AccessDeniedCount}";

        MessageBox.Show(
            this,
            $"Scan completed successfully.\n\n" +
            $"Files processed: {summary.FilesProcessed}\n" +
            $"Photos: {summary.PhotoCount}\n" +
            $"Videos: {summary.VideoCount}\n" +
            $"Unsupported: {summary.UnsupportedCount}\n" +
            $"Access denied folders: {summary.AccessDeniedCount}\n" +
            $"Duration: {summary.Duration.TotalSeconds:F1} seconds",
            "Scan Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void SetScanningState(
        bool isScanning)
    {
        SelectFolderButton.IsEnabled = !isScanning;
        StartScanButton.IsEnabled =
            !isScanning &&
            !string.IsNullOrWhiteSpace(_selectedFolderPath);

        CancelScanButton.IsEnabled = isScanning;
    }

    private void ResetScanDisplay()
    {
        ProcessedCountText.Text = "0";
        PhotoCountText.Text = "0";
        VideoCountText.Text = "0";
        UnsupportedCountText.Text = "0";
        CurrentFileText.Text = "No scan running";
        ScanProgressBar.Value = 0;
        ScanProgressBar.IsIndeterminate = false;
    }

    private static bool ContainsSupportedMedia(
        string folderPath)
    {
        var pendingFolders = new Stack<string>();
        pendingFolders.Push(folderPath);

        while (pendingFolders.Count > 0)
        {
            string currentFolder = pendingFolders.Pop();

            try
            {
                bool mediaFound =
                    Directory
                        .EnumerateFiles(currentFolder)
                        .Any(
                            file =>
                                SupportedMediaExtensions.Contains(
                                    Path.GetExtension(file)));

                if (mediaFound)
                {
                    return true;
                }

                foreach (
                    string subfolder in
                    Directory.EnumerateDirectories(currentFolder))
                {
                    pendingFolders.Push(subfolder);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore inaccessible folders while validating.
            }
            catch (IOException)
            {
                // Ignore unavailable folders while validating.
            }
        }

        return false;
    }

    private void ShowAccessDeniedMessage()
    {
        StatusText.Text = "Access denied";

        MessageBox.Show(
            this,
            "PhotoPilot could not access the selected folder.",
            "Access Denied",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void ShowErrorMessage(
        string title,
        string message)
    {
        StatusText.Text = "An error occurred";

        MessageBox.Show(
            this,
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    protected override void OnClosed(
        EventArgs e)
    {
        _scanCancellationTokenSource?.Cancel();
        _scanCancellationTokenSource?.Dispose();

        base.OnClosed(e);
    }
}