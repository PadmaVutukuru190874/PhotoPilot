namespace PhotoPilot.App.Models;

/// <summary>
/// Represents the complete cleanup plan created from a duplicate review session.
/// No file operation is performed by this model.
/// </summary>
public sealed class CleanupPlan
{
    public List<CleanupPlanItem> Items { get; } = [];

    public int FileCount =>
        Items.Count;

    public long TotalRecoverableBytes =>
        Items.Sum(item => item.FileSizeBytes);

    public bool IsEmpty =>
        Items.Count == 0;

    public static CleanupPlan FromSession(
        DuplicateReviewSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var plan = new CleanupPlan();

        foreach (DuplicateReviewGroup group in session.Groups)
        {
            foreach (
                DuplicateReviewItem item in
                group.Items.Where(
                    reviewItem =>
                        reviewItem.IsSelectedToDelete &&
                        !reviewItem.IsIgnored &&
                        !reviewItem.IsSelectedToKeep))
            {
                plan.Items.Add(
                    new CleanupPlanItem
                    {
                        MediaItemId = item.MediaItemId,
                        DuplicateGroupId = group.GroupId,
                        FilePath = item.FilePath,
                        FileName = item.FileName,
                        FileSizeBytes = item.FileSizeBytes,
                        FileHash = item.FileHash,
                        ThumbnailPath =
                            string.IsNullOrWhiteSpace(
                                item.ThumbnailPath)
                                ? null
                                : item.ThumbnailPath
                    });
            }
        }

        return plan;
    }
}