namespace PhotoPilot.Imaging.Models;

public sealed record ThumbnailResult
{
    public required string SourceFilePath { get; init; }

    public required string ThumbnailFilePath { get; init; }

    public bool Success { get; init; }

    public bool ThumbnailCreated { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public string? ErrorMessage { get; init; }
}