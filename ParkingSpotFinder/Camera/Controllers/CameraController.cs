
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using SkiaSharp;

namespace Camera.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CameraController : ControllerBase
    {
        [HttpGet("image")]
        public IActionResult GetParkingImage([FromQuery] int totalSpots = 50, [FromQuery] DateTime? testTime = null, [FromQuery] string? lotId = null)
        {
            var time = testTime ?? DateTime.UtcNow;
            int occupiedSpots = CalculateOccupiedSpots(totalSpots, time, lotId);

            return GenerateParkingImage(totalSpots, occupiedSpots);
        }

        [HttpGet("parkingImage")]
        public IActionResult GetParkingImageLegacy([FromQuery] int totalSpots = 50, [FromQuery] DateTime? simulationTime = null)
        {
            var time = simulationTime ?? DateTime.UtcNow;
            int occupiedSpots = CalculateOccupiedSpots(totalSpots, time);

            return GenerateParkingImage(totalSpots, occupiedSpots);
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        private IActionResult GenerateParkingImage(int totalSpots, int occupiedSpots)
        {
            int spotsPerRow = (int)Math.Ceiling(Math.Sqrt(totalSpots));
            int rows = (int)Math.Ceiling((double)totalSpots / spotsPerRow);
            
            int spotSize = 3;
            int roadWidth = 2;
            int imageWidth = spotsPerRow * spotSize + (spotsPerRow + 1) * roadWidth;
            int imageHeight = rows * spotSize + (rows + 1) * roadWidth;

            using (var bitmap = new SKBitmap(imageWidth, imageHeight))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Black);

                    for (int spotIndex = 0; spotIndex < totalSpots; spotIndex++)
                    {
                        int row = spotIndex / spotsPerRow;
                        int col = spotIndex % spotsPerRow;
                        
                        int startX = roadWidth + col * (spotSize + roadWidth);
                        int startY = roadWidth + row * (spotSize + roadWidth);
                        
                        SKColor spotColor = (spotIndex < occupiedSpots) ? SKColors.Red : SKColors.Green;
                        
                        for (int x = startX; x < startX + spotSize; x++)
                        {
                            for (int y = startY; y < startY + spotSize; y++)
                            {
                                if (x < imageWidth && y < imageHeight)
                                {
                                    bitmap.SetPixel(x, y, spotColor);
                                }
                            }
                        }
                    }
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
                    return File(stream.ToArray(), "image/png");
                }
            }
        }

        private int CalculateOccupiedSpots(int totalSpots, DateTime time, string? lotId = null)
        {
            double percentage;
            int hour = time.Hour;

            double basePercentage = lotId switch
            {
                "downtown" => GetDowntownPattern(hour),
                "mall-a" => GetMallPattern(hour),
                "airport" => GetAirportPattern(hour),
                _ => GetDefaultPattern(hour)
            };

            var random = new Random(time.GetHashCode());
            double variation = (random.NextDouble() - 0.5) * 0.1;
            percentage = Math.Max(0, Math.Min(1, basePercentage + variation));

            return (int)(totalSpots * percentage);
        }

        private double GetDefaultPattern(int hour)
        {
            if (hour >= 22 || hour < 6)
            {
                return 0.1; 
            }
            else if (hour >= 6 && hour < 9)
            {
                return 0.1 + (hour - 6) / 3.0 * 0.8;
            }
            else if (hour >= 9 && hour < 16)
            {
                return 0.9;
            }
            else if (hour >= 16 && hour < 18)
            {
                return 0.95;
            }
            else
            {
                return 0.95 - (hour - 18) / 4.0 * 0.85;
            }
        }

        private double GetDowntownPattern(int hour)
        {
            if (hour >= 22 || hour < 7) return 0.05;
            if (hour >= 7 && hour < 9) return 0.3 + (hour - 7) / 2.0 * 0.65;
            if (hour >= 9 && hour < 17) return 0.95;
            if (hour >= 17 && hour < 19) return 0.95 - (hour - 17) / 2.0 * 0.8;
            return 0.15;
        }

        private double GetMallPattern(int hour)
        {
            if (hour >= 22 || hour < 10) return 0.1;
            if (hour >= 10 && hour < 12) return 0.1 + (hour - 10) / 2.0 * 0.6;
            if (hour >= 12 && hour < 20) return 0.7 + Math.Sin((hour - 12) * Math.PI / 8) * 0.2;
            return 0.7 - (hour - 20) / 2.0 * 0.6;
        }

        private double GetAirportPattern(int hour)
        {
            if (hour >= 1 && hour < 5) return 0.3;
            if (hour >= 5 && hour < 8) return 0.3 + (hour - 5) / 3.0 * 0.5;
            if (hour >= 8 && hour < 11) return 0.8;
            if (hour >= 11 && hour < 17) return 0.6;
            if (hour >= 17 && hour < 20) return 0.6 + (hour - 17) / 3.0 * 0.3;
            return 0.9 - (hour >= 20 ? (hour - 20) / 5.0 * 0.6 : 0);
        }
    }
}
