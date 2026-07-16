namespace PhotoPilot.App.Models;

/// <summary>
/// Represents one file displayed in the Duplicate Review Center.
/// </summary>
public sealed class DuplicateReviewItem
{
    public Guid MediaItemId { get; init; }

    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ThumbnailPath { get; init; } = string.Empty;

    public string FileHash { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }

    public DateTime CreatedDate { get; init; }

    public DateTime ModifiedDate { get; init; }

    public bool IsRecommendedKeep { get; set; }

    public bool IsSelectedToKeep { get; set; }

    public bool IsSelectedToDelete { get; set; }

    public bool IsIgnored { get; set; }

    public bool HasThumbnail =>
        !string.IsNullOrWhiteSpace(ThumbnailPath);
}