using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ImageProcessor
{
    public class ImageProcessorFunction
    {
        private readonly ILogger<ImageProcessorFunction> _logger;

        public ImageProcessorFunction(ILogger<ImageProcessorFunction> logger)
        {
            _logger = logger;
        }

        [Function("ImageProcessorFunction")]
        public void Run([BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")] byte[] myBlob, string name)
        {
            _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {myBlob.Length} Bytes");

            // TODO: Dodać logikę wysyłania obrazu do modelu AI (E) i zapisywania wyniku w bazie danych (F)
        }
    }
}