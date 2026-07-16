namespace PhotoPilot.Core;

/// <summary>
/// Represents one catalog item that belongs to an exact-duplicate group.
/// </summary>
public sealed record DuplicateFileItem
{
    public required Guid MediaItemId { get; init; }

    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public required string FileHash { get; init; }

    public long FileSizeBytes { get; init; }

    public DateTime FileCreatedDate { get; init; }

    public DateTime FileModifiedDate { get; init; }

    public string? ThumbnailPath { get; init; }
}