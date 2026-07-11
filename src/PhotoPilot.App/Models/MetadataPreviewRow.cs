namespace PhotoPilot.App.Models;

public sealed record MetadataPreviewRow
{
    public required string FileName { get; init; }

    public required string MediaType { get; init; }

    public required string DateTaken { get; init; }

    public required string Camera { get; init; }

    public required string Resolution { get; init; }

    public required string Gps { get; init; }

    public required string Status { get; init; }
}