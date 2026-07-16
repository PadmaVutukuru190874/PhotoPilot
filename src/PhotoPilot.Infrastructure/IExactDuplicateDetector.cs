using PhotoPilot.Core;

namespace PhotoPilot.Infrastructure;

/// <summary>
/// Detects files whose binary content is exactly identical.
/// </summary>
public interface IExactDuplicateDetector
{
    Task<DuplicateDetectionResult> DetectAsync(
        IReadOnlyList<MediaItem> mediaItems,
        IProgress<DuplicateDetectionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}