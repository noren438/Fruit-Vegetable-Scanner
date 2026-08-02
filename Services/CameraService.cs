using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using SkiaSharp;

namespace Frugt_Grønt_Scanner.Services
{
    public class CameraService
    {
        private static readonly string[] AllowedImageTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/bmp",
        };
        public async Task<byte[]> CapturePhotoAsync()
        {
            
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                throw new NotSupportedException("Kameraet understøttes ikke på denne enhed. Brug Vælg foto eller Upload fil i stedet for.");

            }
            try
            {
                var file = await MediaPicker.Default.CapturePhotoAsync();

                return file is null ? null : await ReadFileAsync(file);
            }
            catch (PermissionException)
            {
                throw new UnauthorizedAccessException("Appen har ikke Tilladelse til at bruge kameraet.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Der opstod en fejl under optagelse af billedet.", ex);
            }
        }
        public async Task<byte[]?> PickPhotoAsync()
        {
            try
            {
                var file = await MediaPicker.Default.PickPhotoAsync();

                return file is null ? null : await ReadFileAsync(file);
            }
            catch (PermissionException)
            {
                throw new UnauthorizedAccessException("Appen er ikke tilladelse til at vælge billeder.");

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Der opstod en fejl under valg af billedet", ex);
            }
        }
        public async Task<byte[]?> UploadFileAsync()
        {
            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Vælg et billede",
                    FileTypes = FilePickerFileType.Images
                });
                return file is null ? null : await ReadFileAsync(file);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Der opstod en fejl under upload af filen.", ex);
            }
        }
        private static async Task<byte[]?> ReadFileAsync(FileResult file)
        {
            if (!IsImageFile(file))
            {
                throw new InvalidOperationException("Den valgte fil er ikke et gyldigt billede.");
            }
            await using var input = await file.OpenReadAsync();
            using var memory = new MemoryStream();

            await input.CopyToAsync(memory);

            if (memory.Length == 0)
            {
                throw new InvalidOperationException("Den valgte billede er tom.");
            }

            var optimized = OptimizeImage(memory.ToArray(), 1600, 85);
            return optimized;
        }

        private static byte[] OptimizeImage(byte[] sourceBytes, int maxSide, int quality)
        {
            try
            {
                using var sourceBitmap = SKBitmap.Decode(sourceBytes);
                if (sourceBitmap == null)
                    return sourceBytes;

                var longestSide = Math.Max(sourceBitmap.Width, sourceBitmap.Height);
                var scale = Math.Min(1f, maxSide / (float)longestSide);

                if (scale >= 0.999f)
                {
                    using var imageNoResize = SKImage.FromBitmap(sourceBitmap);
                    using var encodedNoResize = imageNoResize.Encode(SKEncodedImageFormat.Jpeg, quality);
                    return encodedNoResize?.ToArray() ?? sourceBytes;
                }

                var newWidth = Math.Max(1, (int)Math.Round(sourceBitmap.Width * scale));
                var newHeight = Math.Max(1, (int)Math.Round(sourceBitmap.Height * scale));

                using var resizedBitmap = sourceBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
                if (resizedBitmap == null)
                    return sourceBytes;

                using var image = SKImage.FromBitmap(resizedBitmap);
                using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                return encoded?.ToArray() ?? sourceBytes;
            }
            catch
            {
                return sourceBytes;
            }
        }
        private static bool IsImageFile(FileResult file)
        {
            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return true;
            }
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            return extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp";
        }
    }
}
