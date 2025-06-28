using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Database.Models;
using Database;
using System;
using ImageProcessor.Models;

namespace ImageProcessor
{
    public class ImageProcessorFunction
    {
        private readonly ILogger<ImageProcessorFunction> _logger;
        private readonly HttpClient _httpClient;
        private readonly ParkingDbContext _dbContext;

        public ImageProcessorFunction(ILogger<ImageProcessorFunction> logger, HttpClient httpClient, ParkingDbContext dbContext)
        {
            _logger = logger;
            _httpClient = httpClient;
            _dbContext = dbContext;
        }

        [Function("ImageProcessorFunction")]
        public async Task Run([BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")] byte[] myBlob, string name)
        {
            _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {myBlob.Length} Bytes");

            var pathParts = name.Split('/');
            var parkingLotId = pathParts.Length > 0 ? pathParts[0] : "unknown";
            
            var aiModelUrl = Environment.GetEnvironmentVariable("AI_VISION_MODEL_URL") ?? "http://localhost:5000";
            var imageBase64 = Convert.ToBase64String(myBlob);
            
            var requestData = new
            {
                ImageData = imageBase64,
                TotalSpots = 50
            };
            
            var jsonContent = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{aiModelUrl}/api/ai/analyze", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var aiResult = JsonConvert.DeserializeObject<Models.AiAnalysisResult>(responseString);

            if (aiResult == null)
            {
                _logger.LogError("Failed to deserialize AI analysis result.");
                return;
            }

            int occupiedSpots = aiResult.OccupiedSpots;
            int freeSpots = aiResult.FreeSpots;
            int totalSpots = aiResult.TotalSpots;

            _logger.LogInformation($"AI Model Analysis Result: Occupied Spots = {occupiedSpots}, Free Spots = {freeSpots}, Total = {totalSpots}");

            var parkingState = new ParkingStateHistory
            {
                Timestamp = DateTime.UtcNow,
                OccupiedSpots = occupiedSpots,
                FreeSpots = freeSpots,
                TotalSpots = totalSpots,
                ParkingLotId = parkingLotId
            };

            _dbContext.ParkingStateHistory.Add(parkingState);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Parking state saved to database.");
        }
    }
}
