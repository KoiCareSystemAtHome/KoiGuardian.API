using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KoiGuardian.Api.Services;

public interface IImageUploadService
{
    Task<string> UploadImageAsync(string tablename, string id, IFormFile imageFile);
}

public class ImageUploadService : IImageUploadService
{
    private readonly Cloudinary _cloudinary;
    private const string Placeholder = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTXxZR0_1ISIJx_T4oB5-5OJVSNgSMFLe8eCw&s";

    public ImageUploadService(IConfiguration configuration)
    {
        var account = new Account(
            configuration["Cloudinary:CloudName"],
            configuration["Cloudinary:ApiKey"],
            configuration["Cloudinary:ApiSecret"]
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(string tablename, string id, IFormFile imageFile)
    {
        try
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return Placeholder;
            }

            using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(imageFile.FileName, memoryStream),
                PublicId = $"{tablename}/{id}_{Guid.NewGuid()}"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.AbsoluteUri;
        }
        catch (Exception)
        {
            return Placeholder;
        }
    }
}
