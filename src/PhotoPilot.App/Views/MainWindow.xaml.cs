using Microsoft.Win32;
using PhotoPilot.App.Models;
using PhotoPilot.Core;
using PhotoPilot.Imaging;
using PhotoPilot.Imaging.Models;
using PhotoPilot.Imaging.Services;
using PhotoPilot.Metadata;
using PhotoPilot.Scanner;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PhotoPilot.Infrastructure;

namespace PhotoPilot.App.Views;

public partial class MainWindow : Window
{
    private readonly IExactDuplicateDetector _exactDuplicateDetector;

    private CancellationTokenSource?
        _duplicateCancellationTokenSource;

    private DuplicateDetectionResult?
        _lastDuplicateDetectionResult;

    private List<LibraryCardItem> _libraryCards = new();
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
    private readonly MediaCatalogMetadataEnricher _metadataEnricher;
    private readonly MediaCatalog _mediaCatalog;
    private readonly ThumbnailCacheManager _thumbnailCacheManager;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private string? _selectedFolderPath;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => RefreshLibrary();

        _mediaScanner = new FileSystemMediaScanner();

        _metadataEnricher =
        new MediaCatalogMetadataEnricher(
        new ImageMetadataExtractor());

        _mediaCatalog = new MediaCatalog();

        _exactDuplicateDetector =
        new Sha256DuplicateDetector();

        _thumbnailCacheManager =
        new ThumbnailCacheManager(
        new FileSystemThumbnailGenerator());
    }

    private async void FindDuplicates_Click(
    object sender,
    RoutedEventArgs e)
    {
        IReadOnlyList<MediaItem> catalogItems =
            _mediaCatalog.Items;

        if (catalogItems.Count < 2)
        {
            MessageBox.Show(
                this,
                "At least two media files are required before duplicate detection can run.",
                "Not Enough Media",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        _duplicateCancellationTokenSource?.Cancel();
        _duplicateCancellationTokenSource?.Dispose();

        _duplicateCancellationTokenSource =
            new CancellationTokenSource();

        SetDuplicateDetectionState(true);
        ResetDuplicateSummary();

        ScanProgressBar.IsIndeterminate = false;
        ScanProgressBar.Minimum = 0;
        ScanProgressBar.Maximum = 100;
        ScanProgressBar.Value = 0;

        var progress =
            new Progress<DuplicateDetectionProgress>(
                UpdateDuplicateDetectionProgress);

        try
        {
            StatusText.Text =
                "Finding exact duplicate files...";

            DuplicateStatusText.Text =
                "Grouping files by size and calculating SHA-256 hashes.";

            DuplicateDetectionResult result =
                await _exactDuplicateDetector.DetectAsync(
                    catalogItems,
                    progress,
                    _duplicateCancellationTokenSource.Token);

            _lastDuplicateDetectionResult =
                result;

            DisplayDuplicateDetectionResult(result);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text =
                "Duplicate detection cancelled";

            DuplicateStatusText.Text =
                "Duplicate detection was cancelled by the user.";
        }
        catch (Exception ex)
        {
            StatusText.Text =
                "Duplicate detection failed";

            DuplicateStatusText.Text =
                ex.Message;

            MessageBox.Show(
                this,
                ex.Message,
                "Duplicate Detection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ScanProgressBar.IsIndeterminate = false;

            SetDuplicateDetectionState(false);
        }
    }

    private void UpdateDuplicateDetectionProgress(
    DuplicateDetectionProgress progress)
    {
        ScanProgressBar.IsIndeterminate = false;
        ScanProgressBar.Value = progress.Percentage;

        StatusText.Text =
            $"Finding duplicates: " +
            $"{progress.FilesProcessed} of " +
            $"{progress.TotalFiles} candidate files";

        CurrentFileText.Text =
            progress.CurrentFile;

        DuplicateFilesHashedText.Text =
            progress.FilesHashed.ToString();

        DuplicateStatusText.Text =
            $"{progress.FilesSkipped} unique-size file(s) skipped; " +
            $"{progress.FailedFiles} failure(s).";
    }

    private void DisplayDuplicateDetectionResult(
    DuplicateDetectionResult result)
    {
        DuplicateGroupCountText.Text =
            result.DuplicateGroupCount.ToString();

        DuplicateFileCountText.Text =
            result.DuplicateFileCount.ToString();

        DuplicateFilesHashedText.Text =
            result.FilesHashed.ToString();

        RecoverableSpaceText.Text =
            FormatFileSize(
                result.TotalRecoverableBytes);

        ScanProgressBar.Value = 100;

        if (result.WasCancelled)
        {
            StatusText.Text =
                "Duplicate detection cancelled";

            DuplicateStatusText.Text =
                "Partial duplicate results were not applied.";

            return;
        }

        if (result.DuplicateGroupCount == 0)
        {
            StatusText.Text =
                "No exact duplicates found";

            DuplicateStatusText.Text =
                $"Examined {result.FilesExamined} file(s) in " +
                $"{result.Duration.TotalSeconds:F1} seconds.";

            MessageBox.Show(
                this,
                "No exact duplicate files were found.",
                "Duplicate Detection Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        StatusText.Text =
            $"{result.DuplicateGroupCount} exact duplicate group(s) found";

        DuplicateStatusText.Text =
            $"{result.DuplicateFileCount} duplicate file(s); " +
            $"{FormatFileSize(result.TotalRecoverableBytes)} " +
            $"potentially recoverable.";

        MessageBox.Show(
            this,
            $"Exact duplicate detection completed.\n\n" +
            $"Files examined: {result.FilesExamined}\n" +
            $"Files hashed: {result.FilesHashed}\n" +
            $"Duplicate groups: {result.DuplicateGroupCount}\n" +
            $"Duplicate files: {result.DuplicateFileCount}\n" +
            $"Potentially recoverable: " +
            $"{FormatFileSize(result.TotalRecoverableBytes)}\n" +
            $"Failed files: {result.FailedFiles}\n" +
            $"Duration: {result.Duration.TotalSeconds:F1} seconds",
            "Duplicates Found",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void SetDuplicateDetectionState(
    bool isRunning)
    {
        SelectFolderButton.IsEnabled =
            !isRunning;

        StartScanButton.IsEnabled =
            !isRunning &&
            !string.IsNullOrWhiteSpace(
                _selectedFolderPath);

        FindDuplicatesButton.IsEnabled =
            !isRunning &&
            _mediaCatalog.Count > 1;

        CancelScanButton.IsEnabled =
            isRunning;
    }

    private void ResetDuplicateSummary()
    {
        _lastDuplicateDetectionResult = null;

        DuplicateGroupCountText.Text = "0";
        DuplicateFileCountText.Text = "0";
        DuplicateFilesHashedText.Text = "0";
        RecoverableSpaceText.Text = "0 bytes";

        DuplicateStatusText.Text =
            "No duplicate-detection result available.";
    }


    private void LibraryItemsControl_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (LibraryItemsControl.SelectedItem
            is not LibraryCardItem selectedItem)
        {
            ClearSelectedItemDetails();
            return;
        }

        ShowSelectedItemDetails(selectedItem);
    }


    private void ShowSelectedItemDetails(
    LibraryCardItem selectedItem)
    {
        NoSelectionPanel.Visibility =
            Visibility.Collapsed;

        SelectedItemDetailsPanel.Visibility =
            Visibility.Visible;

        SelectedFileNameText.Text =
            selectedItem.FileName;

        SelectedFilePathText.Text =
            selectedItem.FilePath;

        SelectedMediaTypeText.Text =
            selectedItem.MediaType;

        SelectedDateTakenText.Text =
            selectedItem.DateTaken;

        SelectedCameraText.Text =
            selectedItem.Camera;

        SelectedResolutionText.Text =
            selectedItem.Resolution;

        SelectedFileSizeText.Text =
            selectedItem.FileSize;

        SelectedLocationText.Text =
            selectedItem.Location;

        SelectedStatusText.Text =
            selectedItem.ProcessingStatus;

        SelectedMediaIdText.Text =
            selectedItem.MediaItemId.ToString();

        LoadSelectedThumbnail(
            selectedItem.ThumbnailPath);
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

            ScanResult result =
                await _mediaScanner.ScanAsync(
                    _selectedFolderPath,
                    options,
                    progress,
                    _scanCancellationTokenSource.Token);

            _mediaCatalog.Replace(result.MediaItems);

            if (result.Summary.WasCancelled)
            {
                DisplayScanSummary(result.Summary);
                return;
            }

            await EnrichCatalogMetadataAsync(
                _scanCancellationTokenSource.Token);

            await GenerateCatalogThumbnailsAsync(
            _scanCancellationTokenSource.Token);

            PopulateMetadataPreview();

            PopulateLibraryCards();

            FindDuplicatesButton.IsEnabled =
            
            _mediaCatalog.Count > 1;
            
            RefreshLibrary();

            ScanProgressBar.IsIndeterminate = false;

            DisplayScanSummary(result.Summary);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Scan cancelled";
            CurrentFileText.Text =
                "The operation was cancelled by the user.";
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

        _duplicateCancellationTokenSource?.Cancel();
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

    private async Task EnrichCatalogMetadataAsync(
    CancellationToken cancellationToken)
    {
        StatusText.Text = "Reading photo metadata...";
        CurrentFileText.Text = "Preparing metadata extraction";
        ScanProgressBar.IsIndeterminate = true;

        var progress =
            new Progress<MetadataEnrichmentProgress>(
                metadataProgress =>
                {
                    StatusText.Text =
                        $"Reading metadata: " +
                        $"{metadataProgress.ProcessedPhotos} of " +
                        $"{metadataProgress.TotalPhotos} photos";

                    CurrentFileText.Text =
                        metadataProgress.CurrentFile;
                });

        await _metadataEnricher.EnrichAsync(
            _mediaCatalog,
            progress,
            cancellationToken);

        IReadOnlyList<MediaItem> catalogItems =
            _mediaCatalog.Items;

        int metadataExtractedCount =
            catalogItems.Count(
                item =>
                    item.ProcessingStatus ==
                    MediaProcessingStatus.MetadataExtracted);

        int metadataFailedCount =
            catalogItems.Count(
                item =>
                    item.ProcessingStatus ==
                    MediaProcessingStatus.ProcessingFailed);

        StatusText.Text =
            $"Metadata completed: " +
            $"{metadataExtractedCount} successful, " +
            $"{metadataFailedCount} with errors";

        CurrentFileText.Text =
            $"Catalog contains {_mediaCatalog.Count} media items";
    }

    private void PopulateMetadataPreview()
    {
        IReadOnlyList<MediaItem> catalogItems =
            _mediaCatalog.Items;

        MetadataPreviewRow[] previewRows =
            catalogItems
                .OrderBy(item => item.FileName)
                .Take(100)
                .Select(
                    item =>
                    {
                        MediaMetadataInfo? metadata =
                            item.Metadata;

                        string camera =
                            string.Join(
                                " ",
                                new[]
                                {
                                metadata?.CameraMake,
                                metadata?.CameraModel
                                }
                                .Where(
                                    value =>
                                        !string.IsNullOrWhiteSpace(value)));

                        if (string.IsNullOrWhiteSpace(camera))
                        {
                            camera = "Unknown";
                        }

                        string resolution =
                            metadata?.Width is int width &&
                            metadata?.Height is int height
                                ? $"{width} × {height}"
                                : "Unknown";

                        return new MetadataPreviewRow
                        {
                            FileName = item.FileName,

                            MediaType =
                                item.Kind.ToString(),

                            DateTaken =
                                metadata?.DateTaken?
                                    .ToString("yyyy-MM-dd HH:mm")
                                ?? "Unknown",

                            Camera = camera,

                            Resolution = resolution,

                            Gps =
                                metadata?.HasGps == true
                                    ? "Yes"
                                    : "No",

                            Status =
                                item.ProcessingStatus.ToString()
                        };
                    })
                .ToArray();

        MetadataPreviewGrid.ItemsSource =
            previewRows;

        MetadataPreviewCountText.Text =
            catalogItems.Count > previewRows.Length
                ? $"Showing {previewRows.Length} of {catalogItems.Count}"
                : $"{previewRows.Length} media item(s)";
    }

    private void PopulateLibraryCards()
    {
        IReadOnlyList<MediaItem> catalogItems =
            _mediaCatalog.Items;

        _libraryCards  =
            catalogItems
                .OrderBy(
                    item =>
                        item.Metadata?.DateTaken ??
                        item.FileCreatedDate)
                .ThenBy(item => item.FileName)
                .Select(CreateLibraryCardItem)
                .ToList();

        LibraryItemsControl.ItemsSource =
            _libraryCards; 

        int photoCount =
            catalogItems.Count(
                item => item.Kind == MediaKind.Photo);

        int videoCount =
            catalogItems.Count(
                item => item.Kind == MediaKind.Video);

        LibraryCountText.Text =
            $"{_libraryCards.Count} item(s) — " +
            $"{photoCount} photo(s), " +
            $"{videoCount} video(s)";
    }

    private void RefreshLibrary()
    {
        if (!IsLoaded ||
        SearchTextBox is null ||
        MediaFilterComboBox is null ||
        SortComboBox is null ||
        LibraryItemsControl is null ||
        LibraryCountText is null)
        {
            return;
        }

        IEnumerable<LibraryCardItem> query =
            _libraryCards;

        string search =
            SearchTextBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query =
                query.Where(
                    x =>
                        x.FileName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase));
        }

        string filter =
            ((ComboBoxItem)
                MediaFilterComboBox.SelectedItem)
            ?.Content
            ?.ToString() ?? "All";

        switch (filter)
        {
            case "Photos":

                query =
                    query.Where(x => x.IsPhoto);

                break;

            case "Videos":

                query =
                    query.Where(x => x.IsVideo);

                break;
        }

        string sort =
            ((ComboBoxItem)
                SortComboBox.SelectedItem)
            ?.Content
            ?.ToString() ?? "";

        query = sort switch
        {
            "File Name" =>
                query.OrderBy(item => item.FileName),

            "File Size" =>
                query.OrderBy(item => item.FileSize),

            _ =>
                query.OrderByDescending(item => item.DateTaken)
        };

        List<LibraryCardItem> visibleItems =
        query.ToList();

        LibraryItemsControl.ItemsSource =
            visibleItems;

        LibraryCountText.Text =
            $"{visibleItems.Count} item(s)";

        LibraryItemsControl.SelectedItem = null;
        ClearSelectedItemDetails();

    }

    private void LoadSelectedThumbnail(
    string? thumbnailPath)
    {
        SelectedThumbnailImage.Source = null;
        SelectedThumbnailPlaceholder.Visibility =
            Visibility.Visible;

        if (string.IsNullOrWhiteSpace(thumbnailPath) ||
            !File.Exists(thumbnailPath))
        {
            return;
        }

        try
        {
            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption =
                BitmapCacheOption.OnLoad;

            bitmap.UriSource =
                new System.Uri(
                    thumbnailPath,
                    System.UriKind.Absolute);

            bitmap.EndInit();
            bitmap.Freeze();

            SelectedThumbnailImage.Source =
                bitmap;

            SelectedThumbnailPlaceholder.Visibility =
                Visibility.Collapsed;
        }
        catch
        {
            SelectedThumbnailImage.Source = null;

            SelectedThumbnailPlaceholder.Visibility =
                Visibility.Visible;
        }
    }

    private void ClearSelectedItemDetails()
    {
        if (NoSelectionPanel is null ||
            SelectedItemDetailsPanel is null)
        {
            return;
        }

        NoSelectionPanel.Visibility =
            Visibility.Visible;

        SelectedItemDetailsPanel.Visibility =
            Visibility.Collapsed;

        SelectedThumbnailImage.Source = null;

        SelectedFileNameText.Text =
            string.Empty;

        SelectedFilePathText.Text =
            string.Empty;

        SelectedMediaTypeText.Text =
            string.Empty;

        SelectedDateTakenText.Text =
            string.Empty;

        SelectedCameraText.Text =
            string.Empty;

        SelectedResolutionText.Text =
            string.Empty;

        SelectedFileSizeText.Text =
            string.Empty;

        SelectedLocationText.Text =
            string.Empty;

        SelectedStatusText.Text =
            string.Empty;

        SelectedMediaIdText.Text =
            string.Empty;
    }

    private void SearchTextBox_TextChanged(
    object sender,
    TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshLibrary();
        }
    }

    private void MediaFilterComboBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshLibrary();
        }
    }

    private void SortComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshLibrary();
        }
    }


    private static LibraryCardItem CreateLibraryCardItem(
    MediaItem item)
    {
        MediaMetadataInfo? metadata =
            item.Metadata;

        string camera =
            string.Join(
                " ",
                new[]
                {
                metadata?.CameraMake,
                metadata?.CameraModel
                }
                .Where(
                    value =>
                        !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrWhiteSpace(camera))
        {
            camera = "Unknown";
        }

        string resolution =
            metadata?.Width is int width &&
            metadata?.Height is int height
                ? $"{width} × {height}"
                : "Unknown";

        bool hasLocation =
            metadata?.HasGps == true;

        string location =
            hasLocation &&
            metadata?.Latitude is double latitude &&
            metadata?.Longitude is double longitude
                ? $"{latitude:F5}, {longitude:F5}"
                : "Location unavailable";

        DateTime displayDate =
            metadata?.DateTaken ??
            item.FileCreatedDate;

        return new LibraryCardItem
        {
            MediaItemId = item.Id,
            FileName = item.FileName,
            FilePath = item.FilePath,
            MediaType = item.Kind.ToString(),
            ThumbnailPath = item.ThumbnailPath,
            DateTaken = displayDate.ToString("yyyy-MM-dd HH:mm"),
            Camera = camera,
            Resolution = resolution,
            FileSize = FormatFileSize(item.FileSizeBytes),
            Location = location,
            HasLocation = hasLocation,
            ProcessingStatus =
                item.ProcessingStatus.ToString()
        };
    }

    private static string FormatFileSize(
    long fileSizeBytes)
    {
        const double kiloByte = 1024;
        const double megaByte = kiloByte * 1024;
        const double gigaByte = megaByte * 1024;

        return fileSizeBytes switch
        {
            >= (long)gigaByte =>
                $"{fileSizeBytes / gigaByte:F2} GB",

            >= (long)megaByte =>
                $"{fileSizeBytes / megaByte:F2} MB",

            >= (long)kiloByte =>
                $"{fileSizeBytes / kiloByte:F1} KB",

            _ =>
                $"{fileSizeBytes} bytes"
        };
    }

    private async Task GenerateCatalogThumbnailsAsync(
    CancellationToken cancellationToken)
    {
        IReadOnlyList<MediaItem> photoItems =
            _mediaCatalog.Items
                .Where(item => item.Kind == MediaKind.Photo)
                .ToArray();

        int totalPhotos = photoItems.Count;
        int processedPhotos = 0;
        int successfulThumbnails = 0;
        int failedThumbnails = 0;

        StatusText.Text = "Generating thumbnails...";
        CurrentFileText.Text = "Preparing thumbnail cache";
        ScanProgressBar.IsIndeterminate = true;

        foreach (MediaItem item in photoItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StatusText.Text =
                $"Generating thumbnails: " +
                $"{processedPhotos + 1} of {totalPhotos}";

            CurrentFileText.Text = item.FilePath;

            ThumbnailResult result =
                await _thumbnailCacheManager.GetOrCreateAsync(
                    sourceFilePath: item.FilePath,
                    maxWidth: 320,
                    maxHeight: 240,
                    jpegQuality: 85,
                    overwriteExisting: false,
                    cancellationToken: cancellationToken);

            MediaItem updatedItem;

            if (result.Success)
            {
                successfulThumbnails++;

                updatedItem = item with
                {
                    ThumbnailPath = result.ThumbnailFilePath,
                    ProcessingStatus =
                        MediaProcessingStatus.ThumbnailGenerated
                };
            }
            else
            {
                failedThumbnails++;

                string errorMessage =
                    string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Thumbnail generation failed."
                        : result.ErrorMessage;

                IReadOnlyList<string> updatedErrors =
                    item.ProcessingErrors
                        .Append(errorMessage)
                        .ToArray();

                updatedItem = item with
                {
                    ProcessingStatus =
                        MediaProcessingStatus.ProcessingFailed,

                    ProcessingErrors = updatedErrors
                };
            }

            _mediaCatalog.TryUpdate(updatedItem);

            processedPhotos++;
        }

        ScanProgressBar.IsIndeterminate = false;

        StatusText.Text =
            $"Thumbnails completed: " +
            $"{successfulThumbnails} successful, " +
            $"{failedThumbnails} failed";

        CurrentFileText.Text =
            $"Thumbnail cache: " +
            $"{_thumbnailCacheManager.CacheRootPath}";
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

        MetadataPreviewGrid.ItemsSource = null;
        MetadataPreviewCountText.Text = "No metadata loaded";

        LibraryItemsControl.ItemsSource = null;
        LibraryCountText.Text = "No media loaded";

        LibraryItemsControl.SelectedItem = null;
        ClearSelectedItemDetails();

        FindDuplicatesButton.IsEnabled = false;
        ResetDuplicateSummary();

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

        _duplicateCancellationTokenSource?.Cancel();
        _duplicateCancellationTokenSource?.Dispose();

        base.OnClosed(e);
    }
}