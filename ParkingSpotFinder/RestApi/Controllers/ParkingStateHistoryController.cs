using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Database;
using Database.Models;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace RestApi.Controllers
{
    /// <summary>
    /// Controller for managing parking state history and current parking status
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Parking State History")]
    [Authorize]
    public class ParkingStateHistoryController : ControllerBase
    {
        private readonly ParkingDbContext _context;
        private readonly ILogger<ParkingStateHistoryController> _logger;

        public ParkingStateHistoryController(ParkingDbContext context, ILogger<ParkingStateHistoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current parking status for all parking lots
        /// </summary>
        /// <returns>Current parking status for all lots</returns>
        /// <response code="200">Successfully retrieved current parking status</response>
        [HttpGet("current")]
        [SwaggerOperation(
            Summary = "Get current parking status", 
            Description = "Retrieves the latest parking status for all parking lots"
        )]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCurrentParkingStatus()
        {
            try
            {
                var currentStatus = await _context.ParkingStateHistory
                    .GroupBy(p => p.ParkingLotId)
                    .Select(g => g.OrderByDescending(p => p.Timestamp).First())
                    .Join(_context.ParkingLot,
                        history => history.ParkingLotId,
                        lot => lot.Id,
                        (history, lot) => new
                        {
                            ParkingLotId = lot.Id,
                            ParkingLotName = lot.Name,
                            Location = lot.Location,
                            TotalSpots = history.TotalSpots,
                            OccupiedSpots = history.OccupiedSpots,
                            FreeSpots = history.FreeSpots,
                            LastUpdated = history.Timestamp
                        })
                    .ToListAsync();

                return Ok(currentStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current parking status");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets the current parking status for a specific parking lot
        /// </summary>
        /// <param name="parkingLotId">Parking lot ID</param>
        /// <returns>Current parking status for the specified lot</returns>
        /// <response code="200">Successfully retrieved parking status</response>
        /// <response code="404">Parking lot not found or no data available</response>
        [HttpGet("current/{parkingLotId}")]
        [SwaggerOperation(
            Summary = "Get current parking status for specific lot", 
            Description = "Retrieves the latest parking status for a specific parking lot"
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCurrentParkingStatus(
            [SwaggerParameter("Parking lot ID", Required = true)] string parkingLotId)
        {
            try
            {
                var currentStatus = await _context.ParkingStateHistory
                    .Where(p => p.ParkingLotId == parkingLotId)
                    .OrderByDescending(p => p.Timestamp)
                    .Join(_context.ParkingLot,
                        history => history.ParkingLotId,
                        lot => lot.Id,
                        (history, lot) => new
                        {
                            ParkingLotId = lot.Id,
                            ParkingLotName = lot.Name,
                            Location = lot.Location,
                            TotalSpots = history.TotalSpots,
                            OccupiedSpots = history.OccupiedSpots,
                            FreeSpots = history.FreeSpots,
                            LastUpdated = history.Timestamp
                        })
                    .FirstOrDefaultAsync();

                if (currentStatus == null)
                {
                    return NotFound($"No parking data found for parking lot {parkingLotId}");
                }

                return Ok(currentStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parking status for lot {ParkingLotId}", parkingLotId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets parking history for a specific parking lot
        /// </summary>
        /// <param name="parkingLotId">Parking lot ID</param>
        /// <param name="hours">Number of hours to look back (default: 24)</param>
        /// <returns>Parking history for the specified period</returns>
        /// <response code="200">Successfully retrieved parking history</response>
        /// <response code="404">Parking lot not found</response>
        [HttpGet("history/{parkingLotId}")]
        [SwaggerOperation(
            Summary = "Get parking history for specific lot", 
            Description = "Retrieves parking history for a specific parking lot over a specified time period"
        )]
        [ProducesResponseType(typeof(IEnumerable<ParkingStateHistory>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ParkingStateHistory>>> GetParkingHistory(
            [SwaggerParameter("Parking lot ID", Required = true)] string parkingLotId,
            [SwaggerParameter("Number of hours to look back")] int hours = 24)
        {
            try
            {
                var parkingLotExists = await _context.ParkingLot.AnyAsync(p => p.Id == parkingLotId);
                if (!parkingLotExists)
                {
                    return NotFound($"Parking lot with ID {parkingLotId} not found");
                }

                var fromDate = DateTime.UtcNow.AddHours(-hours);
                var history = await _context.ParkingStateHistory
                    .Where(p => p.ParkingLotId.ToString() == parkingLotId && p.Timestamp >= fromDate)
                    .OrderByDescending(p => p.Timestamp)
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parking history for lot {ParkingLotId}", parkingLotId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Adds a new parking state record (typically called by the image processing function)
        /// </summary>
        /// <param name="parkingState">Parking state data</param>
        /// <returns>Created parking state record</returns>
        /// <response code="201">Successfully created parking state record</response>
        /// <response code="400">Invalid parking state data</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Add parking state record", 
            Description = "Adds a new parking state record to the history"
        )]
        [ProducesResponseType(typeof(ParkingStateHistory), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ParkingStateHistory>> CreateParkingState(
            [SwaggerParameter("Parking state data", Required = true)]
            [FromBody] ParkingStateHistory parkingState)
        {
            if (parkingState == null)
            {
                return BadRequest("Invalid parking state data");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var parkingLotExists = await _context.ParkingLot.AnyAsync(p => p.Id == parkingState.ParkingLotId);
                if (!parkingLotExists)
                {
                    return BadRequest($"Parking lot with ID {parkingState.ParkingLotId} does not exist");
                }

                parkingState.Timestamp = DateTime.UtcNow;
                _context.ParkingStateHistory.Add(parkingState);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCurrentParkingStatus), 
                    new { parkingLotId = parkingState.ParkingLotId }, parkingState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating parking state record");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets parking statistics for a specific lot
        /// </summary>
        /// <param name="parkingLotId">Parking lot ID</param>
        /// <param name="days">Number of days to analyze (default: 7)</param>
        /// <returns>Parking statistics including occupancy rates</returns>
        /// <response code="200">Successfully retrieved parking statistics</response>
        /// <response code="404">Parking lot not found</response>
        [HttpGet("statistics/{parkingLotId}")]
        [SwaggerOperation(
            Summary = "Get parking statistics", 
            Description = "Retrieves parking statistics including average occupancy for a specific parking lot"
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetParkingStatistics(
            [SwaggerParameter("Parking lot ID", Required = true)] string parkingLotId,
            [SwaggerParameter("Number of days to analyze")] int days = 7)
        {
            try
            {
                var parkingLot = await _context.ParkingLot.FirstOrDefaultAsync(p => p.Id == parkingLotId);
                if (parkingLot == null)
                {
                    return NotFound($"Parking lot with ID {parkingLotId} not found");
                }

                var fromDate = DateTime.UtcNow.AddDays(-days);
                var history = await _context.ParkingStateHistory
                    .Where(p => p.ParkingLotId.ToString() == parkingLotId && p.Timestamp >= fromDate)
                    .ToListAsync();

                if (!history.Any())
                {
                    return Ok(new
                    {
                        ParkingLotId = parkingLotId,
                        ParkingLotName = parkingLot.Name,
                        AnalysisPeriod = $"{days} days",
                        Message = "No data available for the specified period"
                    });
                }

                var statistics = new
                {
                    ParkingLotId = parkingLotId,
                    ParkingLotName = parkingLot.Name,
                    AnalysisPeriod = $"{days} days",
                    TotalRecords = history.Count,
                    AverageOccupancy = Math.Round(history.Average(h => (double)h.OccupiedSpots / h.TotalSpots * 100), 2),
                    MaxOccupancy = history.Max(h => h.OccupiedSpots),
                    MinOccupancy = history.Min(h => h.OccupiedSpots),
                    PeakOccupancyTime = history.OrderByDescending(h => h.OccupiedSpots).First().Timestamp,
                    LowestOccupancyTime = history.OrderBy(h => h.OccupiedSpots).First().Timestamp
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving parking statistics for lot {ParkingLotId}", parkingLotId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}