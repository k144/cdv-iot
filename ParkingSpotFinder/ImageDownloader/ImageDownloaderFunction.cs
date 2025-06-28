using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Database;
using Database.Models;
using Azure.Storage.Blobs;

namespace ImageDownloader
{
    public class ImageDownloaderFunction
    {
        private readonly ILogger<ImageDownloaderFunction> _logger;
        private readonly ParkingDbContext _dbContext;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly HttpClient _httpClient;

        public ImageDownloaderFunction(
            ILogger<ImageDownloaderFunction> logger,
            ParkingDbContext dbContext,
            BlobServiceClient blobServiceClient,
            HttpClient httpClient)
        {
            _logger = logger;
            _dbContext = dbContext;
            _blobServiceClient = blobServiceClient;
            _httpClient = httpClient;
        }

        [Function("ImageDownloaderFunction")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");

            try
            {
                var parkingLots = await _dbContext.ParkingLot.ToListAsync();
                _logger.LogInformation($"Found {parkingLots.Count} parking lots to process");

                var containerClient = _blobServiceClient.GetBlobContainerClient("parking-images");
                await containerClient.CreateIfNotExistsAsync();

                foreach (var parkingLot in parkingLots)
                {
                    await DownloadAndStoreImage(parkingLot, containerClient);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while downloading images");
            }
        }

        private async Task DownloadAndStoreImage(ParkingLot parkingLot, BlobContainerClient containerClient)
        {
            try
            {
                _logger.LogInformation($"Downloading image from camera for parking lot: {parkingLot.Name}");

                var response = await _httpClient.GetAsync(parkingLot.CameraUrl);
                response.EnsureSuccessStatusCode();

                var imageData = await response.Content.ReadAsByteArrayAsync();
                var timestamp = DateTime.UtcNow;
                var blobName = $"{parkingLot.Id}/{timestamp:yyyy/MM/dd/HH-mm-ss}.png";

                var blobClient = containerClient.GetBlobClient(blobName);
                
                using var stream = new MemoryStream(imageData);
                await blobClient.UploadAsync(stream, overwrite: true);

                _logger.LogInformation($"Successfully uploaded image for parking lot {parkingLot.Name} to blob: {blobName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download and store image for parking lot: {parkingLot.Name}");
            }
        }
    }
}