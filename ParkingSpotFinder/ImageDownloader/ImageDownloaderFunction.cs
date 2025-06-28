using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ImageDownloader
{
    public class ImageDownloaderFunction
    {
        private readonly ILogger<ImageDownloaderFunction> _logger;

        public ImageDownloaderFunction(ILogger<ImageDownloaderFunction> logger)
        {
            _logger = logger;
        }

        [Function("ImageDownloaderFunction")]
        public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");

            // TODO: Dodać logikę pobierania obrazów z kamer i zapisywania ich do Blob Storage
        }
    }
}