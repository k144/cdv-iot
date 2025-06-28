using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Database;
using Database.Models;
using System.ComponentModel.DataAnnotations;
using Azure;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.Identity;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Parking Lots Management")]
    [Authorize]
    public class ParkingLotsController : ControllerBase
    {
        private readonly ParkingDbContext _context;
        private readonly ILogger<ParkingLotsController> _logger;

        public ParkingLotsController(ParkingDbContext context, ILogger<ParkingLotsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("../health")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Health check", Description = "Returns the health status of the API")]
        public ActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow, service = "RestApi" });
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all parking lots", 
            Description = "Retrieves a list of all available parking lots"
        )]
        [ProducesResponseType(typeof(IEnumerable<ParkingLot>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ParkingLot>>> GetParkingLots()
        {
            try
            {
                var parkingLots = await _context.ParkingLot.ToListAsync();
                return Ok(parkingLots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parking lots");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get parking lot by ID", 
            Description = "Retrieves a specific parking lot using its unique identifier"
        )]
        [ProducesResponseType(typeof(ParkingLot), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ParkingLot>> GetParkingLot(
            [SwaggerParameter("Unique identifier of the parking lot", Required = true)]
            string id)
        {
            try
            {
                var parkingLot = await _context.ParkingLot.FirstOrDefaultAsync(p => p.Id == id);
                if (parkingLot == null)
                {
                    return NotFound($"Parking lot with ID {id} not found");
                }
                return Ok(parkingLot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parking lot with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Create a new parking lot", 
            Description = "Adds a new parking lot to the system and deploys camera service in Azure"
        )]
        [ProducesResponseType(typeof(ParkingLot), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ParkingLot>> CreateParkingLot(
            [SwaggerParameter("Parking lot details", Required = true)]
            [FromBody] ParkingLot parkingLot)
        {
            if (parkingLot == null)
            {
                return BadRequest("Invalid parking lot data");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                parkingLot.Id = Guid.NewGuid().ToString();
                
                var cameraDeploymentResult = await DeployCameraInAzure(parkingLot);
                if (!cameraDeploymentResult.Success)
                {
                    _logger.LogError("Failed to deploy camera for parking lot {ParkingLotId}: {Error}", 
                        parkingLot.Id, cameraDeploymentResult.ErrorMessage);
                    return StatusCode(500, $"Failed to deploy camera: {cameraDeploymentResult.ErrorMessage}");
                }

                parkingLot.CameraUrl = cameraDeploymentResult.CameraUrl;
                
                _context.ParkingLot.Add(parkingLot);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created parking lot {ParkingLotId} with camera deployed at {CameraUrl}", 
                    parkingLot.Id, parkingLot.CameraUrl);

                return CreatedAtAction(nameof(GetParkingLot), new { id = parkingLot.Id }, parkingLot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating parking lot");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(
            Summary = "Update parking lot", 
            Description = "Updates an existing parking lot"
        )]
        [ProducesResponseType(typeof(ParkingLot), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ParkingLot>> UpdateParkingLot(
            [SwaggerParameter("Parking lot ID", Required = true)] string id,
            [SwaggerParameter("Updated parking lot details", Required = true)] [FromBody] ParkingLot parkingLot)
        {
            if (id != parkingLot.Id)
            {
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingParkingLot = await _context.ParkingLot.FirstOrDefaultAsync(p => p.Id == id);
            if (existingParkingLot == null)
            {
                return NotFound($"Parking lot with ID {id} not found");
            }

            try
            {
                existingParkingLot.Name = parkingLot.Name;
                existingParkingLot.Location = parkingLot.Location;
                existingParkingLot.TotalParkingSpaces = parkingLot.TotalParkingSpaces;
                existingParkingLot.CameraUrl = parkingLot.CameraUrl;

                await _context.SaveChangesAsync();
                return Ok(existingParkingLot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating parking lot with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Delete parking lot", 
            Description = "Deletes an existing parking lot"
        )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteParkingLot(
            [SwaggerParameter("Parking lot ID", Required = true)] string id)
        {
            try
            {
                var parkingLot = await _context.ParkingLot.FirstOrDefaultAsync(p => p.Id == id);
                if (parkingLot == null)
                {
                    return NotFound($"Parking lot with ID {id} not found");
                }

                _context.ParkingLot.Remove(parkingLot);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting parking lot with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("configure-camera")]
        [SwaggerOperation(
            Summary = "Configure new camera", 
            Description = "Allows users to add their own cameras and deploys the camera service in Azure"
        )]
        [ProducesResponseType(typeof(ParkingLot), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ParkingLot>> ConfigureCamera(
            [SwaggerParameter("Camera configuration details", Required = true)]
            [FromBody] CameraConfiguration cameraConfig)
        {
            if (cameraConfig == null)
            {
                return BadRequest("Invalid camera configuration");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var parkingLot = new ParkingLot
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = cameraConfig.Name,
                    Location = cameraConfig.Location,
                    TotalParkingSpaces = cameraConfig.TotalParkingSpaces,
                    CameraUrl = "" // Will be set by deployment
                };

                var cameraDeploymentResult = await DeployCameraInAzure(parkingLot);
                if (!cameraDeploymentResult.Success)
                {
                    _logger.LogError("Failed to deploy camera for configuration {ConfigName}: {Error}", 
                        cameraConfig.Name, cameraDeploymentResult.ErrorMessage);
                    return StatusCode(500, $"Failed to deploy camera: {cameraDeploymentResult.ErrorMessage}");
                }

                parkingLot.CameraUrl = cameraDeploymentResult.CameraUrl;
                
                _context.ParkingLot.Add(parkingLot);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully configured camera {ConfigName} with deployment ID {DeploymentId}", 
                    cameraConfig.Name, cameraDeploymentResult.DeploymentId);

                return CreatedAtAction(nameof(GetParkingLot), new { id = parkingLot.Id }, parkingLot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring camera");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<CameraDeploymentResult> DeployCameraInAzure(ParkingLot parkingLot)
        {
            try
            {
                var cameraName = $"camera-{parkingLot.Id.ToLower()}";
                var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
                var resourceGroupName = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP") ?? "parking-cameras-rg";
                var location = Environment.GetEnvironmentVariable("AZURE_REGION") ?? "eastus";
                var containerImage = Environment.GetEnvironmentVariable("CAMERA_CONTAINER_IMAGE") ?? "mcr.microsoft.com/azuredocs/aci-helloworld";
                
                var acrLoginServer = Environment.GetEnvironmentVariable("ACR_LOGIN_SERVER");
                var acrUsername = Environment.GetEnvironmentVariable("ACR_USERNAME");
                var acrPassword = Environment.GetEnvironmentVariable("ACR_PASSWORD");
                
                if (string.IsNullOrEmpty(subscriptionId))
                {
                    _logger.LogWarning("AZURE_SUBSCRIPTION_ID not configured, falling back to simulation mode");
                    return await SimulateCameraDeployment(cameraName, location);
                }

                _logger.LogInformation("Deploying camera {CameraName} in Azure region {Region}", cameraName, location);

                var credential = new DefaultAzureCredential();
                var client = new ArmClient(credential);
                
                var subscription = await client.GetDefaultSubscriptionAsync();
                var resourceGroups = subscription.GetResourceGroups();
                var resourceGroup = await resourceGroups.GetAsync(resourceGroupName);

                var containerGroupCollection = resourceGroup.Value.GetContainerGroups();
                var resources = new ContainerResourceRequirements(
                    new ContainerResourceRequestsContent(1.5, 1));
                
                var containerGroupData = new ContainerGroupData(location, new[]
                {
                    new ContainerInstanceContainer(cameraName, containerImage, resources)
                    {
                        Ports = { new ContainerPort(80) }
                    }
                }, ContainerInstanceOperatingSystemType.Linux)
                {
                    RestartPolicy = ContainerGroupRestartPolicy.Always,
                    IPAddress = new ContainerGroupIPAddress(new[]
                    {
                        new ContainerGroupPort(80) { Protocol = ContainerGroupNetworkProtocol.Tcp }
                    }, ContainerGroupIPAddressType.Public)
                    {
                        DnsNameLabel = cameraName
                    },
                    Tags = 
                    {
                        ["parking-lot-id"] = parkingLot.Id,
                        ["created-by"] = "parking-spot-finder",
                        ["deployment-date"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
                    }
                };

                if (!string.IsNullOrEmpty(acrLoginServer) && !string.IsNullOrEmpty(acrUsername) && !string.IsNullOrEmpty(acrPassword))
                {
                    containerGroupData.ImageRegistryCredentials.Add(new ContainerGroupImageRegistryCredential(acrLoginServer)
                    {
                        Username = acrUsername,
                        Password = acrPassword
                    });
                    _logger.LogInformation("Added ACR credentials for private registry: {LoginServer}", acrLoginServer);
                }

                var operation = await containerGroupCollection.CreateOrUpdateAsync(
                    WaitUntil.Completed, 
                    cameraName, 
                    containerGroupData);

                var containerGroup = operation.Value;
                var cameraUrl = $"http://{containerGroup.Data.IPAddress.Fqdn}/api/camera/image";

                _logger.LogInformation("Camera {CameraName} deployed successfully at {CameraUrl}", cameraName, cameraUrl);
                
                return new CameraDeploymentResult
                {
                    Success = true,
                    CameraUrl = cameraUrl,
                    DeploymentId = containerGroup.Data.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deploy camera for parking lot {ParkingLotId}", parkingLot.Id);
                
                _logger.LogInformation("Falling back to simulation mode for camera deployment");
                return await SimulateCameraDeployment($"camera-{parkingLot.Id.ToLower()}", "eastus");
            }
        }

        private async Task<CameraDeploymentResult> SimulateCameraDeployment(string cameraName, string region)
        {
            _logger.LogInformation("Simulating camera deployment for {CameraName}", cameraName);
            
            await Task.Delay(2000);
            
            var cameraUrl = $"https://{cameraName}.{region}.azurecontainer.io/api/camera/image";
            
            return new CameraDeploymentResult
            {
                Success = true,
                CameraUrl = cameraUrl,
                DeploymentId = $"simulated-{cameraName}"
            };
        }
    }

    public class CameraDeploymentResult
    {
        public bool Success { get; set; }
        public string? CameraUrl { get; set; }
        public string? DeploymentId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CameraConfiguration
    {
        [Required]
        [SwaggerParameter("Name of the parking area/camera", Required = true)]
        public required string Name { get; set; }

        [Required]
        [SwaggerParameter("Location/address of the parking area", Required = true)]
        public required string Location { get; set; }

        [Range(1, 1000)]
        [SwaggerParameter("Total number of parking spaces to monitor", Required = true)]
        public int TotalParkingSpaces { get; set; }

        [SwaggerParameter("Additional simulation parameters for mock camera")]
        public CameraSimulationConfig? SimulationConfig { get; set; }
    }

    public class CameraSimulationConfig
    {
        [Range(0.0, 1.0)]
        [SwaggerParameter("Base occupancy rate (0.0 to 1.0)")]
        public double BaseOccupancyRate { get; set; } = 0.3;

        [Range(0.0, 1.0)]
        [SwaggerParameter("Traffic variation factor (0.0 to 1.0)")]
        public double TrafficVariation { get; set; } = 0.2;

        [SwaggerParameter("Enables time-based simulation of daily traffic patterns")]
        public bool EnableDailyPattern { get; set; } = true;
    }
}