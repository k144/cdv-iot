
using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Camera.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CameraController : ControllerBase
    {
        [HttpGet("parkingImage")]
        public IActionResult GetParkingImage([FromQuery] int totalSpots = 50, [FromQuery] DateTime? simulationTime = null)
        {
            var time = simulationTime ?? DateTime.UtcNow;
            int occupiedSpots = CalculateOccupiedSpots(totalSpots, time);

            int spotWidth = 15;
            int spotHeight = 25;
            int padding = 5;
            int spotsPerRow = 10;
            int numRows = (totalSpots + spotsPerRow - 1) / spotsPerRow;

            int imageWidth = (spotWidth + padding) * spotsPerRow + padding;
            int imageHeight = (spotHeight + padding) * numRows + padding;

            using (var bitmap = new Bitmap(imageWidth, imageHeight))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.DarkGray);

                    for (int i = 0; i < totalSpots; i++)
                    {
                        var brush = (i < occupiedSpots) ? Brushes.Red : Brushes.Green;
                        
                        int row = i / spotsPerRow;
                        int col = i % spotsPerRow;

                        int x = padding + col * (spotWidth + padding);
                        int y = padding + row * (spotHeight + padding);

                        graphics.FillRectangle(brush, x, y, spotWidth, spotHeight);
                    }
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return File(stream.ToArray(), "image/png");
                }
            }
        }

        private int CalculateOccupiedSpots(int totalSpots, DateTime time)
        {
            double percentage;
            int hour = time.Hour;

            // Simple model of occupancy throughout the day
            if (hour >= 22 || hour < 6) // Night time
            {
                percentage = 0.1; 
            }
            else if (hour >= 6 && hour < 9) // Morning ramp-up
            {
                percentage = 0.1 + (hour - 6) / 3.0 * 0.8; // Ramp up to 90%
            }
            else if (hour >= 9 && hour < 16) // Daytime peak
            {
                percentage = 0.9;
            }
            else if (hour >= 16 && hour < 18) // Evening peak
            {
                percentage = 0.95;
            }
            else // Evening ramp-down
            {
                percentage = 0.95 - (hour - 18) / 4.0 * 0.85; // Ramp down to 10%
            }

            return (int)(totalSpots * percentage);
        }
    }
}
