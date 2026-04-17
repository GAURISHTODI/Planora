using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanoraApp.API.Services.TripService.Models
{
    public class PlaceOfInterestDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string TripId { get; set; } = string.Empty;
        public int DayNumber { get; set; }
        public int SortOrder { get; set; }

        // v3.0: Choiceable vs Non-Choiceable model
        public string PoiClass { get; set; } = "non_choiceable"; // "choiceable" | "non_choiceable"
        public string Category { get; set; } = string.Empty; // hotel, flight, cab, train, car_rental, monument, museum, park, restaurant, etc.

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AiSummary { get; set; } = string.Empty;

        public double Lat { get; set; }
        public double Lng { get; set; }

        // Timing info
        public string BestTimeOfDay { get; set; } = "any"; // "morning" | "afternoon" | "evening" | "any"
        public int VisitDurationMinutes { get; set; } = 60;
        public string EstimatedCost { get; set; } = string.Empty; // e.g. "₹50"
        public string OpeningHours { get; set; } = string.Empty; // e.g. "09:00–17:00"
        public bool IsFree { get; set; } = false;

        // Media
        public string PhotoUrl { get; set; } = string.Empty;
        public string GooglePlaceId { get; set; } = string.Empty;

        // v3.0: Deeplinks (built at generation time, no affiliate IDs)
        // For Choiceable items (hotels, flights, cabs, trains)
        public List<DeeplinkOption>? SuggestedOptions { get; set; } // 2–3 shown inline
        public List<DeeplinkOption>? BrowseLinks { get; set; } // rest shown in Browse sheet

        // For Non-Choiceable items (attractions, restaurants)
        public string? TicketUrl { get; set; } // single ticket link if exists
        public string GoogleMapsUrl { get; set; } = string.Empty; // directions link

        // Legacy fields (kept for compatibility)
        public string SuggestedTime { get; set; } = string.Empty;
        public decimal EstimatedCostUsd { get; set; }
        public string MapsUrl { get; set; } = string.Empty;
    }

    public class DeeplinkOption
    {
        public string Platform { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
    }
}
