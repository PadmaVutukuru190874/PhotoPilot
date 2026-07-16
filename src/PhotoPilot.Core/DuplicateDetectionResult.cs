namespace PhotoPilot.Core;

/// <summary>
/// Contains the outcome of one exact-duplicate detection operation.
/// </summary>
public sealed record DuplicateDetectionResult
{
    public required IReadOnlyList<DuplicateGroup> Groups { get; init; }

    public int FilesExamined { get; init; }

    public int FilesHashed { get; init; }

    public int FilesSkipped { get; init; }

    public int FailedFiles { get; init; }

    public TimeSpan Duration { get; init; }

    public bool WasCancelled { get; init; }

    public IReadOnlyList<string> Errors { get; init; } =
        Array.Empty<string>();

    public int DuplicateGroupCount =>
        Groups.Count;

    public int DuplicateFileCount =>
        Groups.Sum(group => group.FileCount);

    public long TotalRecoverableBytes =>
        Groups.Sum(group => group.RecoverableBytes);
}