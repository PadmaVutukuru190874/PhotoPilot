using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhotoPilot.Imaging.Models;

namespace PhotoPilot.Imaging.Services;

public sealed class FileSystemThumbnailGenerator
    : IThumbnailGenerator
{
    public Task<BitmapSource> GenerateAsync(
        ThumbnailRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                BitmapImage bitmap =
                    new BitmapImage();

                bitmap.BeginInit();

                bitmap.CacheOption =
                    BitmapCacheOption.OnLoad;

                bitmap.UriSource =
                    new Uri(
                        request.SourceFilePath,
                        UriKind.Absolute);

                bitmap.EndInit();

                bitmap.Freeze();

                cancellationToken.ThrowIfCancellationRequested();

                double scale =
                    Math.Min(
                        (double)request.MaxWidth /
                        bitmap.PixelWidth,

                        (double)request.MaxHeight /
                        bitmap.PixelHeight);

                if (scale > 1)
                {
                    scale = 1;
                }

                var transformed =
                    new TransformedBitmap(
                        bitmap,
                        new ScaleTransform(
                            scale,
                            scale));

                transformed.Freeze();

                return (BitmapSource)transformed;
            },
            cancellationToken);
    }
}