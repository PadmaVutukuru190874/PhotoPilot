namespace PhotoPilot.App.Models;

/// <summary>
/// Represents one file selected for a future cleanup action.
/// </summary>
public sealed record CleanupPlanItem
{
    public required Guid MediaItemId { get; init; }

    public required Guid DuplicateGroupId { get; init; }

    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public long FileSizeBytes { get; init; }

    public required string FileHash { get; init; }

    public string? ThumbnailPath { get; init; }
}