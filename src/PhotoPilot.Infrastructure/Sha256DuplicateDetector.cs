using System.Diagnostics;
using System.Security.Cryptography;
using PhotoPilot.Core;

namespace PhotoPilot.Infrastructure;

/// <summary>
/// Detects exact duplicate files using file-size grouping followed by
/// SHA-256 content hashing.
/// </summary>
public sealed class Sha256DuplicateDetector
    : IExactDuplicateDetector
{
    private const int StreamBufferSize = 1024 * 128;

    public async Task<DuplicateDetectionResult> DetectAsync(
        IReadOnlyList<MediaItem> mediaItems,
        IProgress<DuplicateDetectionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaItems);

        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();

        int filesProcessed = 0;
        int filesHashed = 0;
        int filesSkipped = 0;
        int failedFiles = 0;
        bool wasCancelled = false;

        /*
         * Remove duplicate catalog paths before processing. The catalog should
         * already prevent them, but this makes the detector defensive.
         */
        MediaItem[] distinctItems =
            mediaItems
                .Where(
                    item =>
                        !string.IsNullOrWhiteSpace(item.FilePath))
                .GroupBy(
                    item => item.FilePath,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();

        /*
         * Files with a unique size cannot be exact duplicates, so they do not
         * need to be read and hashed.
         */
        MediaItem[][] possibleDuplicateGroups =
            distinctItems
                .GroupBy(item => item.FileSizeBytes)
                .Where(group => group.Count() > 1)
                .Select(group => group.ToArray())
                .ToArray();

        int possibleDuplicateFileCount =
            possibleDuplicateGroups.Sum(group => group.Length);

        filesSkipped =
            distinctItems.Length -
            possibleDuplicateFileCount;

        var filesByHash =
            new Dictionary<
                string,
                List<DuplicateFileItem>>(
                    StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (MediaItem[] sizeGroup in possibleDuplicateGroups)
            {
                foreach (MediaItem mediaItem in sizeGroup)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        string hash =
                            await CalculateSha256Async(
                                mediaItem.FilePath,
                                cancellationToken);

                        filesHashed++;

                        var duplicateFile =
                            new DuplicateFileItem
                            {
                                MediaItemId = mediaItem.Id,
                                FilePath = mediaItem.FilePath,
                                FileName = mediaItem.FileName,
                                FileHash = hash,
                                FileSizeBytes =
                                    mediaItem.FileSizeBytes,
                                FileCreatedDate =
                                    mediaItem.FileCreatedDate,
                                FileModifiedDate =
                                    mediaItem.FileModifiedDate,
                                ThumbnailPath =
                                    mediaItem.ThumbnailPath
                            };

                        if (!filesByHash.TryGetValue(
                                hash,
                                out List<DuplicateFileItem>? hashFiles))
                        {
                            hashFiles = [];
                            filesByHash.Add(hash, hashFiles);
                        }

                        hashFiles.Add(duplicateFile);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        failedFiles++;

                        errors.Add(
                            $"{mediaItem.FilePath}: {ex.Message}");
                    }
                    finally
                    {
                        filesProcessed++;

                        progress?.Report(
                            new DuplicateDetectionProgress
                            {
                                FilesProcessed =
                                    filesProcessed,

                                TotalFiles =
                                    possibleDuplicateFileCount,

                                FilesHashed =
                                    filesHashed,

                                FilesSkipped =
                                    filesSkipped,

                                FailedFiles =
                                    failedFiles,

                                CurrentFile =
                                    mediaItem.FilePath
                            });
                    }
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

        DuplicateGroup[] duplicateGroups =
            filesByHash
                .Where(pair => pair.Value.Count > 1)
                .Select(
                    pair =>
                        new DuplicateGroup
                        {
                            FileHash = pair.Key,

                            Files =
                                pair.Value
                                    .OrderBy(
                                        file =>
                                            file.FileCreatedDate)
                                    .ThenBy(
                                        file =>
                                            file.FilePath,
                                        StringComparer
                                            .OrdinalIgnoreCase)
                                    .ToArray()
                        })
                .OrderByDescending(
                    group =>
                        group.RecoverableBytes)
                .ThenByDescending(
                    group =>
                        group.FileCount)
                .ToArray();

        return new DuplicateDetectionResult
        {
            Groups = duplicateGroups,
            FilesExamined = distinctItems.Length,
            FilesHashed = filesHashed,
            FilesSkipped = filesSkipped,
            FailedFiles = failedFiles,
            Duration = stopwatch.Elapsed,
            WasCancelled = wasCancelled,
            Errors = errors
        };
    }

    private static async Task<string> CalculateSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The media file no longer exists.",
                filePath);
        }

        await using var stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                StreamBufferSize,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan);

        byte[] hash =
            await SHA256.HashDataAsync(
                stream,
                cancellationToken);

        return Convert.ToHexString(hash);
    }
}