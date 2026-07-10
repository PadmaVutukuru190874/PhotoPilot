namespace PhotoPilot.Scanner;

public sealed record ScanProgress(
    int FilesProcessed,
    int PhotoCount,
    int VideoCount,
    int UnsupportedCount,
    string? CurrentFile);