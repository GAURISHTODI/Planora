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

        /// <summary>
        /// Lightweight endpoint for the trips list — returns metadata only (no GeneratedItinerary blob).
        /// </summary>
        [HttpGet("user/{userId}/summary")]
        public async Task<IActionResult> GetUserTripsSummary(string userId)
        {
            var trips = await _tripService.GetUserTripsAsync(userId);
            var summaries = trips.Select(t => new
            {
                t.Id,
                t.UserId,
                t.Destination,
                t.StartDate,
                t.EndDate,
                t.BudgetTier,
                t.GroupType,
                t.PaxCount,
                t.Themes,
                t.TravelMode,
                t.Origin,
                t.Status,
                t.CreatedAt,
                HasItinerary = !string.IsNullOrEmpty(t.GeneratedItinerary)
            });
            return Ok(summaries);
        }

        /// <summary>
        /// Full endpoint — still available for backward compat.
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTrips(string userId)
        {
            var trips = await _tripService.GetUserTripsAsync(userId);
            return Ok(trips);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrip([FromBody] TripDocument request)
        {
            if(string.IsNullOrEmpty(request.UserId)) 
            {
                request.UserId = "placeholder_user";
            }
            
            var created = await _tripService.CreateTripAsync(request);
            return CreatedAtAction(nameof(GetTripById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Returns trip metadata + parsed itinerary in a single response.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTripById(string id)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (trip == null) return NotFound();

            // Build a combined response with parsed itinerary
            object? parsedItinerary = null;
            if (!string.IsNullOrEmpty(trip.GeneratedItinerary))
            {
                try
                {
                    parsedItinerary = JsonSerializer.Deserialize<object>(trip.GeneratedItinerary);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[TripController] Invalid JSON in trip {id}: {ex.Message}");
                }
            }

            return Ok(new
            {
                trip.Id,
                trip.UserId,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                trip.BudgetTier,
                trip.GroupType,
                trip.PaxCount,
                trip.Themes,
                trip.TravelMode,
                trip.Origin,
                trip.Status,
                trip.CreatedAt,
                GeneratedItinerary = parsedItinerary
            });
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
