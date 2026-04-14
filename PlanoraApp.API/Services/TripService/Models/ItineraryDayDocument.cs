using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanoraApp.API.Services.TripService.Models
{
    public class ItineraryDayDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public int DayNumber { get; set; }
        public DateTime Date { get; set; }
        public string Theme { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        
        public List<PlaceOfInterestDocument> Places { get; set; } = new();
    }
}
