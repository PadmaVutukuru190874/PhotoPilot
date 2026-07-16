using System.Windows;
using PhotoPilot.App.Models;

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

        FooterStatusText.Text =
            $"Reviewing duplicate group " +
            $"{displayGroupNumber}";

        PreviousGroupButton.IsEnabled =
            _reviewSession.CurrentGroupIndex > 0;

        NextGroupButton.IsEnabled =
            _reviewSession.CurrentGroupIndex <
            _reviewSession.Groups.Count - 1;
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