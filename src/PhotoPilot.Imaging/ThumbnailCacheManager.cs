using PhotoPilot.Imaging.Models;
using PhotoPilot.Imaging.Services;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace PhotoPilot.Imaging;

/// <summary>
/// Creates and reuses JPEG thumbnails in PhotoPilot's local cache.
/// </summary>
public sealed class ThumbnailCacheManager
{
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly string _cacheRootPath;

    public ThumbnailCacheManager(
        IThumbnailGenerator thumbnailGenerator,
        string? cacheRootPath = null)
    {
        _thumbnailGenerator =
            thumbnailGenerator ??
            throw new ArgumentNullException(
                nameof(thumbnailGenerator));

        _cacheRootPath =
            string.IsNullOrWhiteSpace(cacheRootPath)
                ? GetDefaultCacheRootPath()
                : Path.GetFullPath(cacheRootPath);
    }

    public string CacheRootPath => _cacheRootPath;

    public async Task<ThumbnailResult> GetOrCreateAsync(
        string sourceFilePath,
        int maxWidth = 320,
        int maxHeight = 240,
        int jpegQuality = 85,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sourceFilePath);

        ValidateDimensions(
            maxWidth,
            maxHeight,
            jpegQuality);

        string fullSourcePath =
            Path.GetFullPath(sourceFilePath);

        if (!File.Exists(fullSourcePath))
        {
            return CreateFailureResult(
                fullSourcePath,
                string.Empty,
                "The source image does not exist.");
        }

        string thumbnailPath =
            GetThumbnailPath(
                fullSourcePath,
                maxWidth,
                maxHeight);

        if (File.Exists(thumbnailPath) &&
            !overwriteExisting)
        {
            return ReadExistingThumbnailResult(
                fullSourcePath,
                thumbnailPath);
        }

        string temporaryPath =
            thumbnailPath +
            "." +
            Guid.NewGuid().ToString("N") +
            ".tmp";

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? thumbnailDirectory =
                Path.GetDirectoryName(thumbnailPath);

            if (!string.IsNullOrWhiteSpace(
                    thumbnailDirectory))
            {
                Directory.CreateDirectory(
                    thumbnailDirectory);
            }

            var request = new ThumbnailRequest
            {
                SourceFilePath = fullSourcePath,
                DestinationFilePath = thumbnailPath,
                MaxWidth = maxWidth,
                MaxHeight = maxHeight,
                JpegQuality = jpegQuality,
                OverwriteExisting = overwriteExisting
            };

            BitmapSource bitmap =
                await _thumbnailGenerator.GenerateAsync(
                    request,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await Task.Run(
                () => SaveAsJpeg(
                    bitmap,
                    temporaryPath,
                    jpegQuality),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(
                temporaryPath,
                thumbnailPath,
                overwriteExisting);

            return new ThumbnailResult
            {
                SourceFilePath = fullSourcePath,
                ThumbnailFilePath = thumbnailPath,
                Success = true,
                ThumbnailCreated = true,
                Width = bitmap.PixelWidth,
                Height = bitmap.PixelHeight
            };
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(temporaryPath);
            throw;
        }
        catch (Exception ex)
        {
            TryDeleteFile(temporaryPath);

            return CreateFailureResult(
                fullSourcePath,
                thumbnailPath,
                ex.Message);
        }
    }

    public string GetThumbnailPath(
        string sourceFilePath,
        int maxWidth = 320,
        int maxHeight = 240)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            sourceFilePath);

        string fullSourcePath =
            Path.GetFullPath(sourceFilePath);

        var fileInfo =
            new FileInfo(fullSourcePath);

        string cacheIdentity =
            string.Join(
                "|",
                fullSourcePath.ToUpperInvariant(),
                fileInfo.Exists
                    ? fileInfo.Length
                    : 0,
                fileInfo.Exists
                    ? fileInfo.LastWriteTimeUtc.Ticks
                    : 0,
                maxWidth,
                maxHeight);

        byte[] hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    cacheIdentity));

        string hashText =
            Convert.ToHexString(hash);

        string subfolder =
            hashText[..2];

        string fileName =
            hashText + ".jpg";

        return Path.Combine(
            _cacheRootPath,
            subfolder,
            fileName);
    }

    public bool TryDeleteCachedThumbnail(
        string sourceFilePath,
        int maxWidth = 320,
        int maxHeight = 240)
    {
        try
        {
            string thumbnailPath =
                GetThumbnailPath(
                    sourceFilePath,
                    maxWidth,
                    maxHeight);

            if (!File.Exists(thumbnailPath))
            {
                return false;
            }

            File.Delete(thumbnailPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetDefaultCacheRootPath()
    {
        string localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .LocalApplicationData);

        return Path.Combine(
            localAppData,
            "PhotoPilot",
            "Cache",
            "Thumbnails");
    }

    private static void SaveAsJpeg(
        BitmapSource bitmap,
        string destinationPath,
        int jpegQuality)
    {
        var encoder =
            new JpegBitmapEncoder
            {
                QualityLevel = jpegQuality
            };

        encoder.Frames.Add(
            BitmapFrame.Create(bitmap));

        using var outputStream =
            new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

        encoder.Save(outputStream);
    }

    private static ThumbnailResult
        ReadExistingThumbnailResult(
            string sourceFilePath,
            string thumbnailPath)
    {
        try
        {
            BitmapFrame frame =
                BitmapFrame.Create(
                    new Uri(
                        thumbnailPath,
                        UriKind.Absolute),
                    BitmapCreateOptions
                        .PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

            frame.Freeze();

            return new ThumbnailResult
            {
                SourceFilePath = sourceFilePath,
                ThumbnailFilePath = thumbnailPath,
                Success = true,
                ThumbnailCreated = false,
                Width = frame.PixelWidth,
                Height = frame.PixelHeight
            };
        }
        catch (Exception ex)
        {
            return CreateFailureResult(
                sourceFilePath,
                thumbnailPath,
                $"The cached thumbnail could not be read: {ex.Message}");
        }
    }

    private static ThumbnailResult
        CreateFailureResult(
            string sourceFilePath,
            string thumbnailPath,
            string errorMessage)
    {
        return new ThumbnailResult
        {
            SourceFilePath = sourceFilePath,
            ThumbnailFilePath = thumbnailPath,
            Success = false,
            ThumbnailCreated = false,
            ErrorMessage = errorMessage
        };
    }

    private static void ValidateDimensions(
        int maxWidth,
        int maxHeight,
        int jpegQuality)
    {
        if (maxWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxWidth),
                "Maximum width must be greater than zero.");
        }

        if (maxHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxHeight),
                "Maximum height must be greater than zero.");
        }

        if (jpegQuality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jpegQuality),
                "JPEG quality must be between 1 and 100.");
        }
    }

    private static void TryDeleteFile(
        string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Cleanup failure must not hide
            // the original exception.
        }
    }
}