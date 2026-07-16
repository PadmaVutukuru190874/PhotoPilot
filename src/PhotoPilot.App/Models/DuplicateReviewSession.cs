namespace PhotoPilot.App.Models;

/// <summary>
/// Represents the complete duplicate review session.
/// </summary>
public sealed class DuplicateReviewSession
{
    public List<DuplicateReviewGroup> Groups { get; } = [];

    public int CurrentGroupIndex { get; set; }

    public DuplicateReviewGroup? CurrentGroup =>
        CurrentGroupIndex >= 0 &&
        CurrentGroupIndex < Groups.Count
            ? Groups[CurrentGroupIndex]
            : null;

    public int TotalGroups =>
        Groups.Count;

    public int TotalDuplicateFiles =>
        Groups.Sum(
            g => g.DuplicateCount);

    public long TotalRecoverableBytes =>
        Groups.Sum(
            g => g.RecoverableBytes);
}