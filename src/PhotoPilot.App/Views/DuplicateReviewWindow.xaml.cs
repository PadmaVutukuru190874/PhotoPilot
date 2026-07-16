using System.Windows;
using PhotoPilot.App.Models;
using System.Windows.Controls;

namespace PhotoPilot.App.Views;

/// <summary>
/// Displays exact duplicate groups one group at a time.
/// File-selection actions are added in the next package.
/// </summary>
public partial class DuplicateReviewWindow : Window
{
    private readonly DuplicateReviewSession _reviewSession;

    public DuplicateReviewWindow()
        : this(new DuplicateReviewSession())
    {
    }

    public DuplicateReviewWindow(
        DuplicateReviewSession reviewSession)
    {
        InitializeComponent();

        _reviewSession =
            reviewSession ??
            throw new ArgumentNullException(
                nameof(reviewSession));

        Loaded += DuplicateReviewWindow_Loaded;
    }

    private void DuplicateReviewWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_reviewSession.Groups.Count == 0)
        {
            ShowEmptyState();
            return;
        }

        if (_reviewSession.CurrentGroupIndex < 0 ||
            _reviewSession.CurrentGroupIndex >=
            _reviewSession.Groups.Count)
        {
            _reviewSession.CurrentGroupIndex = 0;
        }

        DisplayCurrentGroup();
    }

    private void KeepItemButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (!TryGetReviewItem(
                sender,
                out DuplicateReviewItem? selectedItem))
        {
            return;
        }

        DuplicateReviewGroup? currentGroup =
            _reviewSession.CurrentGroup;

        if (currentGroup is null)
        {
            return;
        }

        foreach (DuplicateReviewItem item in currentGroup.Items)
        {
            bool isSelectedItem =
                item.MediaItemId ==
                selectedItem.MediaItemId;

            item.IsSelectedToKeep =
                isSelectedItem;

            item.IsSelectedToDelete =
                !isSelectedItem &&
                !item.IsIgnored;

            if (isSelectedItem)
            {
                item.IsIgnored = false;
            }
        }

        RefreshCurrentGroupDisplay();
    }

    private void QuarantineItemButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (!TryGetReviewItem(
                sender,
                out DuplicateReviewItem? selectedItem))
        {
            return;
        }

        DuplicateReviewGroup? currentGroup =
            _reviewSession.CurrentGroup;

        if (currentGroup is null)
        {
            return;
        }

        selectedItem.IsSelectedToKeep = false;
        selectedItem.IsSelectedToDelete = true;
        selectedItem.IsIgnored = false;

        EnsureSelectedKeep(currentGroup);

        RefreshCurrentGroupDisplay();
    }

    private void IgnoreItemButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (!TryGetReviewItem(
                sender,
                out DuplicateReviewItem? selectedItem))
        {
            return;
        }

        DuplicateReviewGroup? currentGroup =
            _reviewSession.CurrentGroup;

        if (currentGroup is null)
        {
            return;
        }

        selectedItem.IsSelectedToKeep = false;
        selectedItem.IsSelectedToDelete = false;
        selectedItem.IsIgnored = true;

        EnsureSelectedKeep(currentGroup);

        RefreshCurrentGroupDisplay();
    }

    private static bool TryGetReviewItem(
    object sender,
    out DuplicateReviewItem? reviewItem)
    {
        reviewItem = null;

        if (sender is not Button button ||
            button.Tag is not DuplicateReviewItem item)
        {
            return false;
        }

        reviewItem = item;
        return true;
    }

    private static void EnsureSelectedKeep(
    DuplicateReviewGroup group)
    {
        bool hasSelectedKeep =
            group.Items.Any(
                item =>
                    item.IsSelectedToKeep &&
                    !item.IsIgnored &&
                    !item.IsSelectedToDelete);

        if (hasSelectedKeep)
        {
            return;
        }

        DuplicateReviewItem? replacement =
            group.Items
                .Where(
                    item =>
                        !item.IsIgnored &&
                        !item.IsSelectedToDelete)
                .OrderByDescending(
                    item =>
                        item.IsRecommendedKeep)
                .ThenBy(
                    item =>
                        item.CreatedDate)
                .FirstOrDefault();

        if (replacement is null)
        {
            replacement =
                group.Items
                    .Where(item => !item.IsIgnored)
                    .OrderByDescending(
                        item =>
                            item.IsRecommendedKeep)
                    .ThenBy(
                        item =>
                            item.CreatedDate)
                    .FirstOrDefault();
        }

        if (replacement is null)
        {
            return;
        }

        replacement.IsSelectedToKeep = true;
        replacement.IsSelectedToDelete = false;
    }

    private void RefreshCurrentGroupDisplay()
    {
        DuplicateReviewGroup? currentGroup =
            _reviewSession.CurrentGroup;

        if (currentGroup is null)
        {
            ShowEmptyState();
            return;
        }

        DuplicateItemsControl.ItemsSource = null;
        DuplicateItemsControl.ItemsSource =
            currentGroup.Items;

        UpdateReviewStatus(currentGroup);
        UpdateReviewSummary();
    }

    private void UpdateReviewSummary()
    {
        ReviewSummary summary =
            ReviewSummary.FromSession(
                _reviewSession);

        SummaryGroupCountText.Text =
            summary.DuplicateGroups.ToString();

        SummaryKeepCountText.Text =
            summary.FilesToKeep.ToString();

        SummaryQuarantineCountText.Text =
            summary.FilesToQuarantine.ToString();

        SummaryIgnoredCountText.Text =
            summary.IgnoredFiles.ToString();

        SummaryRecoverableText.Text =
            FormatFileSize(
                CalculateSelectedRecoverableBytes());

        ReviewCompletionText.Text =
            $"{summary.ReviewedGroups} of " +
            $"{summary.TotalGroups} groups reviewed";

        ReviewProgressBar.Value =
            summary.ReviewPercentage;

        if (summary.ReadyForCleanup)
        {
            CleanupReadinessText.Text =
                "Ready for cleanup planning. No files will be moved until you confirm.";

            CleanupReadinessText.Foreground =
                System.Windows.Media.Brushes.ForestGreen;
        }
        else
        {
            CleanupReadinessText.Text =
                "Review all groups and keep at least one file in each group.";

            CleanupReadinessText.Foreground =
                System.Windows.Media.Brushes.DarkOrange;
        }
    }

    private long CalculateSelectedRecoverableBytes()
    {
        return _reviewSession.Groups
            .SelectMany(group => group.Items)
            .Where(item => item.IsSelectedToDelete)
            .Sum(item => item.FileSizeBytes);
    }


    private void UpdateReviewStatus(
    DuplicateReviewGroup currentGroup)
    {
        int keepCount =
            currentGroup.Items.Count(
                item => item.IsSelectedToKeep);

        int quarantineCount =
            currentGroup.Items.Count(
                item => item.IsSelectedToDelete);

        int ignoredCount =
            currentGroup.Items.Count(
                item => item.IsIgnored);

        FooterStatusText.Text =
            $"Keep: {keepCount}   " +
            $"Quarantine: {quarantineCount}   " +
            $"Ignored: {ignoredCount}";
    }

    private void PreviousGroupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_reviewSession.CurrentGroupIndex <= 0)
        {
            return;
        }

        _reviewSession.CurrentGroupIndex--;

        DisplayCurrentGroup();
    }

    private void NextGroupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_reviewSession.CurrentGroupIndex >=
            _reviewSession.Groups.Count - 1)
        {
            return;
        }

        _reviewSession.CurrentGroupIndex++;

        DisplayCurrentGroup();
    }

    private void DisplayCurrentGroup()
    {
        DuplicateReviewGroup? currentGroup =
            _reviewSession.CurrentGroup;

        if (currentGroup is null)
        {
            ShowEmptyState();
            return;
        }

        EmptyStatePanel.Visibility =
            Visibility.Collapsed;

        ReviewContentPanel.Visibility =
            Visibility.Visible;

        DuplicateItemsControl.ItemsSource =
            currentGroup.Items;

        int displayGroupNumber =
            _reviewSession.CurrentGroupIndex + 1;

        GroupPositionText.Text =
            $"Group {displayGroupNumber} of " +
            $"{_reviewSession.TotalGroups}";

        RecoverableSpaceText.Text =
            $"Total recoverable: " +
            $"{FormatFileSize(
                _reviewSession.TotalRecoverableBytes)}";

        GroupHashText.Text =
            $"SHA-256: {currentGroup.FileHash}";

        GroupFileCountText.Text =
            $"{currentGroup.DuplicateCount} file(s)";

        GroupRecoverableText.Text =
            $"{FormatFileSize(
                currentGroup.RecoverableBytes)} recoverable";

        UpdateReviewStatus(currentGroup);

        PreviousGroupButton.IsEnabled =
            _reviewSession.CurrentGroupIndex > 0;

        NextGroupButton.IsEnabled =
            _reviewSession.CurrentGroupIndex <
            _reviewSession.Groups.Count - 1;

        UpdateReviewSummary();
    }

    private void ShowEmptyState()
    {
        EmptyStatePanel.Visibility =
            Visibility.Visible;

        ReviewContentPanel.Visibility =
            Visibility.Collapsed;

        DuplicateItemsControl.ItemsSource = null;

        GroupPositionText.Text =
            "Group 0 of 0";

        RecoverableSpaceText.Text =
            "Recoverable space: 0 bytes";

        FooterStatusText.Text =
            "No duplicate groups available";

        PreviousGroupButton.IsEnabled = false;
        NextGroupButton.IsEnabled = false;

        SummaryGroupCountText.Text = "0";
        SummaryKeepCountText.Text = "0";
        SummaryQuarantineCountText.Text = "0";
        SummaryIgnoredCountText.Text = "0";
        SummaryRecoverableText.Text = "0 bytes";

        ReviewCompletionText.Text =
            "0 of 0 groups reviewed";

        ReviewProgressBar.Value = 0;

        CleanupReadinessText.Text =
            "No duplicate groups are available.";

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
}