using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System.IO;

namespace AiVisionModel.Controllers
{
    public class ImageAnalysisRequest
    {
        public string ImageData { get; set; } = string.Empty;
        public int TotalSpots { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        [HttpGet("../health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow, service = "AiVisionModel" });
        }

        [HttpPost("analyze")]
        public IActionResult AnalyzeImage([FromBody] ImageAnalysisRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ImageData))
            {
                return BadRequest("Invalid request. ImageData is required.");
            }

            try
            {
                byte[] imageBytes = Convert.FromBase64String(request.ImageData);
                
                int occupiedSpots = 0;
                int freeSpots = 0;

                using (var stream = new MemoryStream(imageBytes))
                {
                    using (var bitmap = SKBitmap.Decode(stream))
                    {
                        if (bitmap == null)
                        {
                            return BadRequest("Could not decode image data.");
                        }

                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            SKColor pixelColor = bitmap.GetPixel(x, 0);

                            if (pixelColor == SKColors.Red)
                            {
                                occupiedSpots++;
                            }
                            else if (pixelColor == SKColors.Green)
                            {
                                freeSpots++;
                            }
                        }
                    }
                }

                int totalAnalyzed = occupiedSpots + freeSpots;
                if (request.TotalSpots > 0 && totalAnalyzed != request.TotalSpots)
                {
                    double occupancyRatio = totalAnalyzed > 0 ? (double)occupiedSpots / totalAnalyzed : 0.5;
                    occupiedSpots = (int)(request.TotalSpots * occupancyRatio);
                    freeSpots = request.TotalSpots - occupiedSpots;
                }

                return Ok(new { 
                    occupiedSpots = occupiedSpots, 
                    freeSpots = freeSpots,
                    totalSpots = request.TotalSpots,
                    analysisTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing image: {ex.Message}");
            }
        }
    }
}