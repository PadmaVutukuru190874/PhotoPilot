namespace PhotoPilot.Scanner;

public sealed record ScanSummary(
    string FolderPath,
    int FilesProcessed,
    int PhotoCount,
    int VideoCount,
    int UnsupportedCount,
    int AccessDeniedCount,
    TimeSpan Duration,
    bool WasCancelled);