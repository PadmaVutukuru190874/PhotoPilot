namespace PhotoPilot.Scanner;

public interface IMediaScanner
{
    Task<ScanSummary> ScanAsync(
        string folderPath,
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}