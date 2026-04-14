using Microsoft.AspNetCore.Mvc;
using PlanoraApp.API.Services.TripService;

namespace PlanoraApp.API.Services.BookingService
{
    [ApiController]
    [Route("api/v1/booking")]
    public class BookingController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly IAffiliateLinkBuilder _linkBuilder;
        private readonly IPriceCacheService _cacheService;

        public BookingController(ITripService tripService, IAffiliateLinkBuilder linkBuilder, IPriceCacheService cacheService)
        {
            _tripService = tripService;
            _linkBuilder = linkBuilder;
            _cacheService = cacheService;
        }

        [HttpGet("options/{tripId}/{category}")]
        public async Task<IActionResult> GetBookingOptions(string tripId, string category)
        {
            var trip = await _tripService.GetTripByIdAsync(tripId);
            if (trip == null) return NotFound("Trip not found");

            // 1. Check Redis Cache first
            var cachedPricing = await _cacheService.GetIndicativePricesAsync<List<BookingOption>>(tripId, category);
            if (cachedPricing != null && cachedPricing.Count > 0)
            {
                return Ok(cachedPricing);
            }

            // 2. Build Links
            var origin = "LHR"; // Fallback origin if none provided by user profile
            var dest = trip.Destination?.City ?? "TYO";
            var startDate = trip.StartDate == default ? DateTime.UtcNow.AddDays(7) : trip.StartDate;
            var endDate = trip.EndDate == default ? DateTime.UtcNow.AddDays(14) : trip.EndDate;
            int pax = trip.PaxCount > 0 ? trip.PaxCount : 1;

            var options = _linkBuilder.BuildLinks(category, origin, dest, startDate, endDate, pax);

            // 3. Save to Redis (15m TTL)
            await _cacheService.SetIndicativePricesAsync(tripId, category, options);

            return Ok(options);
        }
    }
}
