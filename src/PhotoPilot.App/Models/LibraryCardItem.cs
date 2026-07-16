namespace PhotoPilot.App.Models;

/// <summary>
/// Represents the information displayed on one media card
/// in the PhotoPilot Library screen.
/// </summary>
public sealed record LibraryCardItem
{
    public required Guid MediaItemId { get; init; }

    public required string FileName { get; init; }

    public required string FilePath { get; init; }

    public required string MediaType { get; init; }

    public string? ThumbnailPath { get; init; }

    public string DateTaken { get; init; } = "Unknown";

    public string Camera { get; init; } = "Unknown";

    public string Resolution { get; init; } = "Unknown";

    public string FileSize { get; init; } = "Unknown";

    public string Location { get; init; } = "Location unavailable";

    public string ProcessingStatus { get; init; } = "Unknown";

    public bool HasThumbnail =>
        !string.IsNullOrWhiteSpace(ThumbnailPath);

    public bool HasLocation { get; init; }

    public bool IsPhoto =>
        string.Equals(
            MediaType,
            "Photo",
            StringComparison.OrdinalIgnoreCase);

    public bool IsVideo =>
        string.Equals(
            MediaType,
            "Video",
            StringComparison.OrdinalIgnoreCase);
}