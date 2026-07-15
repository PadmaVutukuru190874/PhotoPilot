namespace PhotoPilot.Core;

public sealed record MediaItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string FilePath { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required MediaKind Kind { get; init; }

    public long FileSizeBytes { get; init; }

    public DateTime FileCreatedDate { get; init; }

    public DateTime FileModifiedDate { get; init; }

    public MediaMetadataInfo? Metadata { get; init; }

    public string? ThumbnailPath { get; init; }

    public MediaProcessingStatus ProcessingStatus { get; init; } =
        MediaProcessingStatus.Discovered;

    public IReadOnlyList<string> ProcessingErrors { get; init; } =
        Array.Empty<string>();
}