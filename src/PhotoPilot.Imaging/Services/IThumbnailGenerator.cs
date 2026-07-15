using System.Windows.Media.Imaging;
using PhotoPilot.Imaging.Models;

namespace PhotoPilot.Imaging.Services;

/// <summary>
/// Generates thumbnails from supported image files.
/// This service only creates the thumbnail bitmap.
/// Saving/caching is handled elsewhere.
/// </summary>
public interface IThumbnailGenerator
{
    Task<BitmapSource> GenerateAsync(
        ThumbnailRequest request,
        CancellationToken cancellationToken = default);
}