namespace PlanoraApp.API.Services.BookingService
{
    public class BookingOption
    {
        public string Platform { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public decimal IndicativePrice { get; set; }
        public string Currency { get; set; } = "INR";
        public string Logo { get; set; } = string.Empty;
    }

    public interface IAffiliateLinkBuilder
    {
        List<BookingOption> BuildLinks(string category, string origin, string destination, DateTime checkIn, DateTime checkOut, int paxCount);
    }

    public class AffiliateLinkBuilder : IAffiliateLinkBuilder
    {
        public List<BookingOption> BuildLinks(string category, string origin, string destination, DateTime checkIn, DateTime checkOut, int paxCount)
        {
            var options = new List<BookingOption>();
            
            // Format dates
            string dateFmt = checkIn.ToString("yyyy-MM-dd");
            string retFmt = checkOut.ToString("yyyy-MM-dd");

            if (category.ToLower() == "flight")
            {
                options.Add(new BookingOption
                {
                    Platform = "Skyscanner",
                    // NOTE: This is a placeholder structural deeplink. Get exact affiliate tracking link from Skyscanner Partner Portal.
                    Url = $"https://www.skyscanner.net/transport/flights/{origin}/{destination}/{dateFmt}/{retFmt}/?adults={paxCount}&affiliate=planora",
                    IndicativePrice = 4500,
                    Logo = "airplane-outline"
                });

                options.Add(new BookingOption
                {
                    Platform = "MakeMyTrip",
                    Url = $"https://www.makemytrip.com/flight/search?itinerary={origin}-{destination}-{dateFmt}_{destination}-{origin}-{retFmt}&paxType=A-{paxCount}",
                    IndicativePrice = 4750,
                    Logo = "airplane"
                });

                options.Add(new BookingOption
                {
                    Platform = "Ixigo",
                    Url = $"https://www.ixigo.com/search/result/flight?from={origin}&to={destination}&date={dateFmt}&returnDate={retFmt}&adults={paxCount}",
                    IndicativePrice = 4400,
                    Logo = "paper-plane-outline"
                });
            }
            else if (category.ToLower() == "hotel")
            {
                options.Add(new BookingOption
                {
                    Platform = "Booking.com",
                    Url = $"https://www.booking.com/searchresults.html?ss={destination}&checkin={dateFmt}&checkout={retFmt}&group_adults={paxCount}&aid=TODO_AFFILIATE_ID",
                    IndicativePrice = 8500,
                    Logo = "bed-outline"
                });

                options.Add(new BookingOption
                {
                    Platform = "Agoda",
                    Url = $"https://www.agoda.com/search?city={destination}&checkIn={dateFmt}&checkOut={retFmt}&adults={paxCount}",
                    IndicativePrice = 7800,
                    Logo = "home-outline"
                });
            }
            else if (category.ToLower() == "activity")
            {
                options.Add(new BookingOption
                {
                    Platform = "Viator",
                    Url = $"https://www.viator.com/searchResults/all?text={destination}",
                    IndicativePrice = 2500,
                    Logo = "ticket-outline"
                });
            }
            else if (category.ToLower() == "cab")
            {
                options.Add(new BookingOption
                {
                    Platform = "Uber",
                    Url = $"uber://?action=setPickup&pickup=my_location&dropoff[formatted_address]={destination}",
                    IndicativePrice = 500,
                    Logo = "car-sport-outline"
                });
            }

            return options;
        }
    }
}
