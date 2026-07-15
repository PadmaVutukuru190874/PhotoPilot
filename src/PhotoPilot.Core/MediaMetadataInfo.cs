namespace PhotoPilot.Core;

public sealed record MediaMetadataInfo
{
    public DateTime? DateTaken { get; init; }

    public string? CameraMake { get; init; }

    public string? CameraModel { get; init; }

    public string? LensModel { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public int? Orientation { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public int? Iso { get; init; }

    public string? Aperture { get; init; }

    public string? ExposureTime { get; init; }

    public bool HasGps =>
        Latitude.HasValue &&
        Longitude.HasValue;
}