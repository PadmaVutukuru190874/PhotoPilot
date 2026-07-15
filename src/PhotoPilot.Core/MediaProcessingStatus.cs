namespace PhotoPilot.Core;

public enum MediaProcessingStatus
{
    Discovered = 1,
    MetadataExtracted = 2,
    ThumbnailGenerated = 3,
    ProcessingFailed = 4
}