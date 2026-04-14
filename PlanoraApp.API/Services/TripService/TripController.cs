using Microsoft.AspNetCore.Mvc;
using PlanoraApp.API.Services.TripService.Models;
using System.Text.Json;

namespace PlanoraApp.API.Services.TripService
{
    [ApiController]
    [Route("api/v1/trips")]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripController(ITripService tripService)
        {
            _tripService = tripService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTrips(string userId)
        {
            var trips = await _tripService.GetUserTripsAsync(userId);
            return Ok(trips);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrip([FromBody] TripDocument request)
        {
            // Note: UserId will be assigned securely via claims once JWT Bearer is wired up
            if(string.IsNullOrEmpty(request.UserId)) 
            {
                request.UserId = "placeholder_user";
            }
            
            var created = await _tripService.CreateTripAsync(request);
            return CreatedAtAction(nameof(GetTripById), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTripById(string id)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (trip == null) return NotFound();
            return Ok(trip);
        }

        [HttpGet("{id}/itinerary")]
        public async Task<IActionResult> GetTripItinerary(string id)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (trip == null) return NotFound();
            if (string.IsNullOrEmpty(trip.GeneratedItinerary)) 
                return Ok(new { itinerary = Array.Empty<object>() });
            
            try
            {
                // Parse and re-serialize to ensure valid JSON
                var parsed = JsonSerializer.Deserialize<object>(trip.GeneratedItinerary);
                return Ok(parsed);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[TripController] Invalid JSON in trip {id}: {ex.Message}");
                return Ok(new { itinerary = Array.Empty<object>(), error = "Invalid itinerary data" });
            }
        }
    }
}
