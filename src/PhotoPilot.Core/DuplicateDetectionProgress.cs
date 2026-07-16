namespace PhotoPilot.Core;

/// <summary>
/// Reports progress while calculating hashes and locating duplicates.
/// </summary>
public sealed record DuplicateDetectionProgress
{
    public int FilesProcessed { get; init; }

    public int TotalFiles { get; init; }

    public int FilesHashed { get; init; }

    public int FilesSkipped { get; init; }

    public int FailedFiles { get; init; }

    public required string CurrentFile { get; init; }

    public double Percentage =>
        TotalFiles <= 0
            ? 0
            : Math.Clamp(
                FilesProcessed * 100.0 / TotalFiles,
                0,
                100);
}