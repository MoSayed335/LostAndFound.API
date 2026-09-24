using FluentAssertions;
using LostAndFound.Application.Common.Exceptions;
using LostAndFound.Application.Common.Options;
using LostAndFound.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LostAndFound.Tests.FileUpload;

public class FileServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<IWebHostEnvironment> _envMock = new();
    private readonly Mock<ILogger<FileService>> _loggerMock = new();
    private readonly ImageUploadOptions _options;
    private readonly FileService _sut;

    public FileServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "LostAndFoundTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _envMock.Setup(e => e.WebRootPath).Returns(_tempDirectory);

        _options = new ImageUploadOptions
        {
            MaxFileSizeInBytes = 5 * 1024 * 1024, // 5 MB
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" },
            UploadFolder = "uploads/items"
        };

        _sut = new FileService(_envMock.Object, Options.Create(_options), _loggerMock.Object);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    private static IFormFile CreateMockFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "Image", fileName);
    }

    [Fact]
    public void ValidateImage_WhenFileIsNull_ShouldReturnTrue()
    {
        var isValid = _sut.ValidateImage(null, out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateImage_WhenFileIsEmpty_ShouldReturnFalse()
    {
        var file = CreateMockFile("empty.jpg", Array.Empty<byte>());

        var isValid = _sut.ValidateImage(file, out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("empty");
    }

    [Fact]
    public void ValidateImage_WhenFileSizeExceedsLimit_ShouldReturnFalse()
    {
        var oversizedBytes = new byte[6 * 1024 * 1024]; // 6 MB
        oversizedBytes[0] = 0xFF;
        oversizedBytes[1] = 0xD8;
        oversizedBytes[2] = 0xFF;
        var file = CreateMockFile("large.jpg", oversizedBytes);

        var isValid = _sut.ValidateImage(file, out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("exceeds the maximum allowed limit");
    }

    [Fact]
    public void ValidateImage_WhenExtensionIsDisallowed_ShouldReturnFalse()
    {
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        var file = CreateMockFile("malicious.exe", exeBytes);

        var isValid = _sut.ValidateImage(file, out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("is not allowed");
    }

    [Fact]
    public void ValidateImage_WhenExtensionIsSpoofed_ShouldFailMagicNumberCheck()
    {
        // Name says .jpg, but bytes are plain text / executable
        var fakeJpgBytes = System.Text.Encoding.UTF8.GetBytes("Not a real jpeg file header");
        var file = CreateMockFile("fake.jpg", fakeJpgBytes);

        var isValid = _sut.ValidateImage(file, out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("does not match a valid image format");
    }

    [Fact]
    public void ValidateImage_WhenValidJpeg_ShouldReturnTrue()
    {
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var file = CreateMockFile("photo.jpg", jpegBytes);

        var isValid = _sut.ValidateImage(file, out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateImage_WhenValidPng_ShouldReturnTrue()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        var file = CreateMockFile("screenshot.png", pngBytes);

        var isValid = _sut.ValidateImage(file, out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateImage_WhenValidWebp_ShouldReturnTrue()
    {
        // "RIFF" (4 bytes) + 4 arbitrary length bytes + "WEBP" (4 bytes)
        var webpBytes = new byte[]
        {
            0x52, 0x49, 0x46, 0x46,
            0x24, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50
        };
        var file = CreateMockFile("graphic.webp", webpBytes);

        var isValid = _sut.ValidateImage(file, out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public async Task SaveFileAsync_WhenValid_ShouldSaveFileWithGuidName_AndReturnRelativeUrl()
    {
        // Arrange
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var file = CreateMockFile("my-family-photo.jpg", jpegBytes);

        // Act
        var relativeUrl = await _sut.SaveFileAsync(file, "items");

        // Assert
        relativeUrl.Should().StartWith("/uploads/items/");
        relativeUrl.Should().EndWith(".jpg");
        relativeUrl.Should().NotContain("my-family-photo"); // User filename discarded

        // Verify physical file was written into web root
        var physicalPath = Path.Combine(_tempDirectory, relativeUrl.TrimStart('/'));
        File.Exists(physicalPath).Should().BeTrue();
        File.ReadAllBytes(physicalPath).Should().BeEquivalentTo(jpegBytes);
    }

    [Fact]
    public async Task SaveFileAsync_WhenInvalidFile_ShouldThrowValidationException()
    {
        var emptyFile = CreateMockFile("empty.png", Array.Empty<byte>());

        var act = () => _sut.SaveFileAsync(emptyFile, "items");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SaveFileAsync_WhenPathTraversalAttempted_ShouldThrowInvalidOperationException()
    {
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var file = CreateMockFile("test.jpg", jpegBytes);

        var act = () => _sut.SaveFileAsync(file, "../../outside-webroot");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid destination folder path.");
    }

    [Fact]
    public async Task DeleteFileAsync_WhenFileExists_ShouldDeletePhysicalFile()
    {
        // Arrange
        var testFolder = Path.Combine(_tempDirectory, "uploads", "items");
        Directory.CreateDirectory(testFolder);
        var testFile = Path.Combine(testFolder, "to-delete.jpg");
        await File.WriteAllBytesAsync(testFile, new byte[] { 0xFF, 0xD8, 0xFF });

        var relativeUrl = "/uploads/items/to-delete.jpg";

        // Act
        await _sut.DeleteFileAsync(relativeUrl);

        // Assert
        File.Exists(testFile).Should().BeFalse();
    }
}
