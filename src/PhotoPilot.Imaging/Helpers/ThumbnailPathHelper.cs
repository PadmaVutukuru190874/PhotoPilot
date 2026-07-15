using System.Security.Cryptography;
using System.Text;
using PhotoPilot.Imaging.Constants;

namespace PhotoPilot.Imaging.Helpers;

public static class ThumbnailPathHelper
{
    public static string GetThumbnailFileName(
        string sourceFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);

        byte[] hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    sourceFile.ToUpperInvariant()));

        string hashString =
            Convert.ToHexString(hash);

        return hashString +
               ThumbnailConstants.ThumbnailExtension;
    }

    public static string GetThumbnailSubFolder(
        string thumbnailFileName)
    {
        return thumbnailFileName.Substring(0, 2);
    }
}