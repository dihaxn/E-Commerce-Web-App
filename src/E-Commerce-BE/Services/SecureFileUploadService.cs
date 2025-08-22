using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace E_Commerce_BE.Services
{
    public class SecureFileUploadService
    {
        private readonly FileUploadSettings _settings;
        private readonly IWebHostEnvironment _environment;

        public SecureFileUploadService(FileUploadSettings settings, IWebHostEnvironment environment)
        {
            _settings = settings;
            _environment = environment;
        }

        public async Task<(bool IsValid, string FileName, string ErrorMessage)> ValidateAndSaveFileAsync(IFormFile file)
        {
            // Check if file is null
            if (file == null)
            {
                return (false, "", "No file provided");
            }

            // Validate file size
            if (file.Length > _settings.MaxFileSizeMB * 1024 * 1024)
            {
                return (false, "", $"File size exceeds maximum allowed size of {_settings.MaxFileSizeMB}MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(extension))
            {
                return (false, "", $"File type {extension} is not allowed. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}");
            }

            // Validate file content type
            if (!IsValidImageContentType(file.ContentType))
            {
                return (false, "", "Invalid file content type");
            }

            // Generate secure filename using GUID and timestamp
            var secureFileName = GenerateSecureFileName(extension);
            var uploadPath = Path.Combine(_environment.WebRootPath, "products");
            var fullPath = Path.Combine(uploadPath, secureFileName);

            // Ensure upload directory exists
            Directory.CreateDirectory(uploadPath);

            // Validate path to prevent directory traversal
            if (!IsPathSafe(uploadPath, fullPath))
            {
                return (false, "", "Invalid file path");
            }

            try
            {
                // Save file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Verify file was saved and is a valid image
                if (!await IsValidImageFileAsync(fullPath))
                {
                    // Delete invalid file
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                    return (false, "", "File appears to be corrupted or invalid");
                }

                return (true, secureFileName, "");
            }
            catch (Exception ex)
            {
                // Clean up on error
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                return (false, "", $"Error saving file: {ex.Message}");
            }
        }

        private string GenerateSecureFileName(string extension)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var guid = Guid.NewGuid().ToString("N");
            return $"{timestamp}_{guid}{extension}";
        }

        private bool IsPathSafe(string basePath, string fullPath)
        {
            var normalizedBasePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);
            var normalizedFullPath = Path.GetFullPath(fullPath);
            return normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsValidImageContentType(string contentType)
        {
            var validTypes = new[]
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "image/webp"
            };
            return validTypes.Contains(contentType.ToLowerInvariant());
        }

        private async Task<bool> IsValidImageFileAsync(string filePath)
        {
            try
            {
                // Read first few bytes to check file signature
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[8];
                await stream.ReadAsync(buffer, 0, 8);

                // Check for common image file signatures
                if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) // JPEG
                    return true;
                if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) // PNG
                    return true;
                if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46) // GIF
                    return true;
                if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46) // WebP
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void DeleteFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            var filePath = Path.Combine(_environment.WebRootPath, "products", fileName);
            if (IsPathSafe(Path.Combine(_environment.WebRootPath, "products"), filePath) && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Log error but don't throw
                }
            }
        }
    }
}
