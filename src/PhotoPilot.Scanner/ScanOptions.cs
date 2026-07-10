namespace PhotoPilot.Scanner;

public sealed class ScanOptions
{
    public bool IncludeSubfolders { get; init; } = true;

    public bool IgnoreHiddenFiles { get; init; } = true;
}