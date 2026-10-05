using MarketLink.Repositories;
using MarketLink.Services;

namespace MarketLink.Services;


public class ProductImageStore
{
    private const long MaxBytes = 4 * 1024 * 1024;
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/pjpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly IWebHostEnvironment _env;

    public ProductImageStore(IWebHostEnvironment env) => _env = env;

    public async Task<(string? Url, string? Error)> SaveAsync(IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return (null, null);
        if (file.Length > MaxBytes) return (null, "Image must be 4 MB or smaller.");
        if (!Allowed.TryGetValue(file.ContentType, out var extension))
            return (null, "Only JPG, PNG or WEBP images are allowed.");

        if (!await HasValidSignatureAsync(file, extension))
            return (null, "The image could not be verified.");

        var directory = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(directory);

        var name = $"{Guid.NewGuid():N}{extension}";
        var target = Path.Combine(directory, name);
        await using (var stream = File.Create(target))
        {
            await file.CopyToAsync(stream, ct);
        }

        return ($"/uploads/products/{name}", null);
    }


    private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension)
    {
        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, CancellationToken.None);
            if (read < header.Length) return false;
        }

        return extension switch
        {
            ".jpg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".webp" => header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                       header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P',
            _ => false
        };
    }


    public void DeleteIfLocal(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
            return;

        var directory = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads", "products"));
        var fileName = Path.GetFileName(url);
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var target = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!target.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (File.Exists(target))
                File.Delete(target);
        }
        catch (IOException)
        {

        }
    }

}
