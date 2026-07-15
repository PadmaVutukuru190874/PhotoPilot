namespace PhotoPilot.Imaging;

public sealed record ThumbnailRequest
{
    public required string SourceFilePath { get; init; }

    public required string DestinationFilePath { get; init; }

    public int MaxWidth { get; init; } = 320;

    public int MaxHeight { get; init; } = 240;

    public int JpegQuality { get; init; } = 85;

    public bool OverwriteExisting { get; init; }
}