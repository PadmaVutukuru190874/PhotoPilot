using System.Diagnostics;

namespace PhotoPilot.Scanner;

public sealed class FileSystemMediaScanner : IMediaScanner
{
    private static readonly HashSet<string> PhotoExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp",
            ".gif",
            ".tif",
            ".tiff",
            ".webp",
            ".heic"
        };

    private static readonly HashSet<string> VideoExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".mov",
            ".avi",
            ".mkv",
            ".wmv"
        };

    public Task<ScanSummary> ScanAsync(
        string folderPath,
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        ArgumentNullException.ThrowIfNull(options);

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"The folder '{folderPath}' does not exist.");
        }

        return Task.Run(
            () => ScanInternal(
                folderPath,
                options,
                progress,
                cancellationToken),
            cancellationToken);
    }

    private static ScanSummary ScanInternal(
        string folderPath,
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        int filesProcessed = 0;
        int photoCount = 0;
        int videoCount = 0;
        int unsupportedCount = 0;
        int accessDeniedCount = 0;
        bool wasCancelled = false;

        try
        {
            var pendingFolders = new Stack<string>();
            pendingFolders.Push(folderPath);

            while (pendingFolders.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string currentFolder = pendingFolders.Pop();

                IEnumerable<string> files;

                try
                {
                    files = Directory.EnumerateFiles(currentFolder);
                }
                catch (UnauthorizedAccessException)
                {
                    accessDeniedCount++;
                    continue;
                }
                catch (IOException)
                {
                    accessDeniedCount++;
                    continue;
                }

                foreach (string filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (options.IgnoreHiddenFiles &&
                        IsHidden(filePath))
                    {
                        continue;
                    }

                    MediaFileKind kind = GetMediaFileKind(filePath);

                    filesProcessed++;

                    switch (kind)
                    {
                        case MediaFileKind.Photo:
                            photoCount++;
                            break;

                        case MediaFileKind.Video:
                            videoCount++;
                            break;

                        default:
                            unsupportedCount++;
                            break;
                    }

                    progress?.Report(
                        new ScanProgress(
                            filesProcessed,
                            photoCount,
                            videoCount,
                            unsupportedCount,
                            filePath));
                }

                if (!options.IncludeSubfolders)
                {
                    continue;
                }

                IEnumerable<string> subfolders;

                try
                {
                    subfolders = Directory.EnumerateDirectories(currentFolder);
                }
                catch (UnauthorizedAccessException)
                {
                    accessDeniedCount++;
                    continue;
                }
                catch (IOException)
                {
                    accessDeniedCount++;
                    continue;
                }

                foreach (string subfolder in subfolders)
                {
                    if (options.IgnoreHiddenFiles &&
                        IsHidden(subfolder))
                    {
                        continue;
                    }

                    pendingFolders.Push(subfolder);
                }
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }
        finally
        {
            stopwatch.Stop();
        }

        return new ScanSummary(
            folderPath,
            filesProcessed,
            photoCount,
            videoCount,
            unsupportedCount,
            accessDeniedCount,
            stopwatch.Elapsed,
            wasCancelled);
    }

    private static MediaFileKind GetMediaFileKind(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        if (PhotoExtensions.Contains(extension))
        {
            return MediaFileKind.Photo;
        }

        if (VideoExtensions.Contains(extension))
        {
            return MediaFileKind.Video;
        }

        return MediaFileKind.Unsupported;
    }

    private static bool IsHidden(string path)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.Hidden);
        }
        catch
        {
            return false;
        }
    }
}