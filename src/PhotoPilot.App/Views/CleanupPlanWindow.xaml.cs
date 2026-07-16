using System.Windows;
using PhotoPilot.App.Models;

namespace PhotoPilot.App.Views;

public partial class CleanupPlanWindow : Window
{
    private readonly CleanupPlan _cleanupPlan;

    public CleanupPlanWindow(
        CleanupPlan cleanupPlan)
    {
        InitializeComponent();

        _cleanupPlan =
            cleanupPlan ??
            throw new ArgumentNullException(
                nameof(cleanupPlan));

        Loaded += CleanupPlanWindow_Loaded;
    }

    private void CleanupPlanWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        DisplayCleanupPlan();
    }

    private void DisplayCleanupPlan()
    {
        PlanFileCountText.Text =
            $"{_cleanupPlan.FileCount} file(s) selected";

        PlanRecoverableText.Text =
            $"{FormatFileSize(
                _cleanupPlan.TotalRecoverableBytes)} recoverable";

        if (_cleanupPlan.IsEmpty)
        {
            EmptyPlanPanel.Visibility =
                Visibility.Visible;

            PlanContentPanel.Visibility =
                Visibility.Collapsed;

            CleanupPlanGrid.ItemsSource = null;

            ProceedToQuarantineButton.IsEnabled =
                false;

            return;
        }

        EmptyPlanPanel.Visibility =
            Visibility.Collapsed;

        PlanContentPanel.Visibility =
            Visibility.Visible;

        CleanupPlanGrid.ItemsSource =
            _cleanupPlan.Items
                .Select(
                    item =>
                        new CleanupPlanPreviewRow
                        {
                            FileName = item.FileName,
                            FilePath = item.FilePath,
                            FileSizeBytes =
                                item.FileSizeBytes,
                            DisplayFileSize =
                                FormatFileSize(
                                    item.FileSizeBytes),
                            DuplicateGroupId =
                                item.DuplicateGroupId
                        })
                .OrderBy(item => item.FileName)
                .ToArray();

        // Enabled only as navigation to the future quarantine engine.
        ProceedToQuarantineButton.IsEnabled = true;
    }

    private void BackToReviewButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ProceedToQuarantineButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "The cleanup plan is ready. No files have been moved yet.\n\n" +
            "The next sprint will implement the safe quarantine engine.",
            "Cleanup Plan Ready",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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

    private sealed record CleanupPlanPreviewRow
    {
        public required string FileName { get; init; }

        public required string FilePath { get; init; }

        public long FileSizeBytes { get; init; }

        public required string DisplayFileSize { get; init; }

        public Guid DuplicateGroupId { get; init; }
    }
}