namespace LostAndFound.Application.Common.Options;

public class ImageUploadOptions
{
    public const string SectionName = "ImageUpload";

    public long MaxFileSizeInBytes { get; set; } = 5 * 1024 * 1024; // 5 MB default

    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];

    public string UploadFolder { get; set; } = "uploads/items";
}
