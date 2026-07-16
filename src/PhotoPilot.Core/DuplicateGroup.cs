namespace PhotoPilot.Core;

/// <summary>
/// Represents files whose content is exactly identical.
/// </summary>
public sealed record DuplicateGroup
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string FileHash { get; init; }

    public required IReadOnlyList<DuplicateFileItem> Files { get; init; }

    public int FileCount => Files.Count;

    public long FileSizeBytes =>
        Files.Count == 0
            ? 0
            : Files[0].FileSizeBytes;

    public long RecoverableBytes =>
        FileCount <= 1
            ? 0
            : FileSizeBytes * (FileCount - 1);

    public bool IsDuplicate =>
        FileCount > 1;
}