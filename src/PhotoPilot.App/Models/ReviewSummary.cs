namespace PhotoPilot.App.Models;

/// <summary>
/// Represents the current review progress for all duplicate groups.
/// </summary>
public sealed class ReviewSummary
{
    public int DuplicateGroups { get; init; }

    public int FilesToKeep { get; init; }

    public int FilesToQuarantine { get; init; }

    public int IgnoredFiles { get; init; }

    public long RecoverableBytes { get; init; }

    public int ReviewedGroups { get; init; }

    public int TotalGroups { get; init; }

    public double ReviewPercentage =>
        TotalGroups == 0
            ? 0
            : ReviewedGroups * 100.0 / TotalGroups;

    public bool ReadyForCleanup =>
        ReviewedGroups == TotalGroups &&
        FilesToKeep > 0;

    public static ReviewSummary FromSession(
        DuplicateReviewSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        int reviewedGroups = 0;

        int keep = 0;
        int quarantine = 0;
        int ignored = 0;

        long recoverable = 0;

        foreach (DuplicateReviewGroup group in session.Groups)
        {
            bool hasKeep =
                group.Items.Any(
                    x => x.IsSelectedToKeep);

            if (hasKeep)
                reviewedGroups++;

            keep +=
                group.Items.Count(
                    x => x.IsSelectedToKeep);

            quarantine +=
                group.Items.Count(
                    x => x.IsSelectedToDelete);

            ignored +=
                group.Items.Count(
                    x => x.IsIgnored);

            recoverable +=
                group.RecoverableBytes;
        }

        return new ReviewSummary
        {
            DuplicateGroups =
                session.TotalGroups,

            FilesToKeep =
                keep,

            FilesToQuarantine =
                quarantine,

            IgnoredFiles =
                ignored,

            RecoverableBytes =
                recoverable,

            ReviewedGroups =
                reviewedGroups,

            TotalGroups =
                session.TotalGroups
        };
    }
}