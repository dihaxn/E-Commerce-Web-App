using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace E_Commerce_BE.Services
{
    public interface ISecureFileUploadService
    {
        Task<(bool, string, string?)> ValidateAndSaveFileAsync(IFormFile file);
        void DeleteFile(string fileName);
    }
}
