namespace PhotoPilot.App.Models;

/// <summary>
/// Represents one duplicate group shown in the review screen.
/// </summary>
public sealed class DuplicateReviewGroup
{
    public Guid GroupId { get; init; }

    public string FileHash { get; init; } = string.Empty;

    public List<DuplicateReviewItem> Items { get; } = [];

    public long RecoverableBytes { get; init; }

    public int DuplicateCount =>
        Items.Count;

    public DuplicateReviewItem? RecommendedKeep =>
        Items.FirstOrDefault(
            x => x.IsRecommendedKeep);

    public DuplicateReviewItem? SelectedKeep =>
        Items.FirstOrDefault(
            x => x.IsSelectedToKeep);

    public IEnumerable<DuplicateReviewItem> SelectedDeletes =>
        Items.Where(
            x => x.IsSelectedToDelete);
}