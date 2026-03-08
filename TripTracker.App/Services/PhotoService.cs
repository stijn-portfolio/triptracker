using Microsoft.Maui.Graphics.Platform;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Foto service - combineert capture/pick met compressie.
    /// Gebaseerd op SafariSnap PhotoImageService (Les 3).
    /// </summary>
    public class PhotoService : IPhotoService
    {
        // ═══════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════

        // Max 4MB voor OpenAI Vision API
        private const int ImageMaxSizeBytes = 4194304;
        // Max resolutie voor compressie
        private const int ImageMaxResolution = 1024;

        // ═══════════════════════════════════════════════════════════
        // PHOTO CAPTURE
        // ═══════════════════════════════════════════════════════════

        public async Task<byte[]?> CapturePhotoAsync()
        {
            try
            {
                // Check of camera beschikbaar is
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    System.Diagnostics.Debug.WriteLine("Camera niet ondersteund op dit device");
                    return null;
                }

                // Maak foto met camera
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                System.Diagnostics.Debug.WriteLine($"Foto genomen");

                if (photo == null)
                    return null;

                // Kleine delay om Android tijd te geven om te herstellen na camera
                await Task.Delay(100);

                // Comprimeer met retry pattern (Android kan file lock houden)
                byte[]? bytes = null;
                try
                {
                    bytes = await ResizePhotoStreamAsync(photo);
                    System.Diagnostics.Debug.WriteLine($"Foto verkleind");

                }
                catch
                {
                    // Retry na extra delay
                    await Task.Delay(200);
                    bytes = await ResizePhotoStreamAsync(photo);
                }

                return bytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Camera error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PHOTO PICK
        // ═══════════════════════════════════════════════════════════

        public async Task<byte[]?> PickPhotoAsync()
        {
            try
            {
                // Kies foto uit galerij
                var photo = await MediaPicker.Default.PickPhotoAsync();

                if (photo == null)
                    return null;

                // Comprimeer indien nodig en retourneer bytes
                return await ResizePhotoStreamAsync(photo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        // Compressie methode - ref SafariSnap PhotoImageService
        private async Task<byte[]> ResizePhotoStreamAsync(FileResult photo)
        {
            byte[]? result = null;

            using (var stream = await photo.OpenReadAsync())
            {
                if (stream.Length > ImageMaxSizeBytes)
                {
                    // Foto te groot, resize nodig
                    var image = PlatformImage.FromStream(stream);
                    if (image != null)
                    {
                        var newImage = image.Downsize(ImageMaxResolution, true);
                        result = newImage.AsBytes();
                    }
                }
                else
                {
                    // Foto klein genoeg, lees direct
                    using (var binaryReader = new BinaryReader(stream))
                    {
                        result = binaryReader.ReadBytes((int)stream.Length);
                    }
                }
            }

            return result!;
        }
    }
}
