using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanoraApp.API.Services.TripService.Models
{
    public class TripDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public Destination? Destination { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string BudgetTier { get; set; } = "mid"; // budget, mid, luxury
        public string GroupType { get; set; } = "solo"; // solo, couple, family, friends
        public int PaxCount { get; set; } = 1;

        public List<string> Themes { get; set; } = new();
        
        public string? TravelMode { get; set; } // "flightIncluded" or "travelOnly"
        
        public Destination? Origin { get; set; } // Origin city if provided

        public List<ItineraryDayDocument> Days { get; set; } = new();
        
        public string Status { get; set; } = "draft"; // draft, generating, active, past
        
        public string? GeneratedItinerary { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Destination
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
