using System.Web;

namespace PlanoraApp.AI.Services
{
    public class DeeplinkOption
    {
        public string Platform { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
    }

    public class DeeplinkContext
    {
        public string Destination { get; set; } = string.Empty;  // city name
        public string CityCode { get; set; } = string.Empty;  // e.g. "JAI" for Jaipur
        public double CenterLat { get; set; }  // POI cluster centroid for this day
        public double CenterLng { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Adults { get; set; }
        public string OriginIata { get; set; } = string.Empty;  // for flights, if user provided
        public string DestIata { get; set; } = string.Empty;
        public double PickupLat { get; set; }  // for cabs
        public double PickupLng { get; set; }
        public string PickupName { get; set; } = string.Empty;
        public double DropLat { get; set; }
        public double DropLng { get; set; }
        public string DropName { get; set; } = string.Empty;
    }

    public class DeeplinkBuilderService
    {
        public List<DeeplinkOption> BuildSuggestedOptions(
            string category, DeeplinkContext ctx) => category.ToLower() switch
        {
            "hotel" => BuildHotelOptions(ctx),
            "flight" => BuildFlightOptions(ctx),
            "cab" => BuildCabOptions(ctx),
            "train" => BuildTrainOptions(ctx),
            "car_rental" => BuildCarRentalOptions(ctx),
            _ => new List<DeeplinkOption>()
        };

        public List<DeeplinkOption> BuildBrowseLinks(
            string category, DeeplinkContext ctx) => category.ToLower() switch
        {
            "hotel" => BuildHotelBrowseLinks(ctx),
            "flight" => BuildFlightBrowseLinks(ctx),
            "cab" => BuildCabBrowseLinks(ctx),
            "train" => BuildTrainBrowseLinks(ctx),
            _ => new List<DeeplinkOption>()
        };

        // HOTELS
        private List<DeeplinkOption> BuildHotelOptions(DeeplinkContext ctx)
        {
            var options = new List<DeeplinkOption>();

            // Booking.com — map-based (primary)
            var bcUrl = ctx.CenterLat != 0
                ? $"https://www.booking.com/searchresults.html" +
                  $"?latitude={ctx.CenterLat}&longitude={ctx.CenterLng}&radius=2" +
                  $"&checkin_year={ctx.CheckIn.Year}&checkin_month={ctx.CheckIn.Month}" +
                  $"&checkin_monthday={ctx.CheckIn.Day}" +
                  $"&checkout_year={ctx.CheckOut.Year}&checkout_month={ctx.CheckOut.Month}" +
                  $"&checkout_monthday={ctx.CheckOut.Day}" +
                  $"&group_adults={ctx.Adults}&no_rooms=1"
                : $"https://www.booking.com/searchresults.html" +
                  $"?ss={HttpUtility.UrlEncode(ctx.Destination)}" +
                  $"&checkin_year={ctx.CheckIn.Year}&checkin_month={ctx.CheckIn.Month}" +
                  $"&checkin_monthday={ctx.CheckIn.Day}" +
                  $"&checkout_year={ctx.CheckOut.Year}&checkout_month={ctx.CheckOut.Month}" +
                  $"&checkout_monthday={ctx.CheckOut.Day}" +
                  $"&group_adults={ctx.Adults}&no_rooms=1";
            options.Add(new DeeplinkOption { Platform = "Booking.com", Url = bcUrl, Logo = "bookingcom" });

            // Agoda
            if (ctx.CenterLat != 0)
                options.Add(new DeeplinkOption
                {
                    Platform = "Agoda",
                    Url = $"https://www.agoda.com/search?latitude={ctx.CenterLat}&longitude={ctx.CenterLng}" +
                          $"&checkIn={ctx.CheckIn:yyyy-MM-dd}&checkOut={ctx.CheckOut:yyyy-MM-dd}" +
                          $"&rooms=1&adults={ctx.Adults}",
                    Logo = "agoda"
                });

            return options;
        }

        private List<DeeplinkOption> BuildHotelBrowseLinks(DeeplinkContext ctx) =>
            new()
            {
                new DeeplinkOption
                {
                    Platform = "MakeMyTrip",
                    Url = $"https://www.makemytrip.com/hotels/hotel-listing/" +
                          $"?city={ctx.CityCode}&checkin={ctx.CheckIn:ddMMyyyy}" +
                          $"&checkout={ctx.CheckOut:ddMMyyyy}&roomStayQualifier=2e0e&country=IN",
                    Logo = "mmt"
                },
                new DeeplinkOption
                {
                    Platform = "OYO",
                    Url = $"https://www.oyorooms.com/search/?location={HttpUtility.UrlEncode(ctx.Destination)}" +
                          $"&check_in={ctx.CheckIn:yyyy-MM-dd}&check_out={ctx.CheckOut:yyyy-MM-dd}" +
                          $"&guest_count={ctx.Adults}",
                    Logo = "oyo"
                },
                new DeeplinkOption
                {
                    Platform = "Goibibo",
                    Url = $"https://www.goibibo.com/hotels/hotels-in-{ctx.Destination.ToLower().Replace(" ", "-")}/" +
                          $"?ci={ctx.CheckIn:yyyyMMdd}&co={ctx.CheckOut:yyyyMMdd}&r=1&a={ctx.Adults}",
                    Logo = "goibibo"
                },
                new DeeplinkOption
                {
                    Platform = "Cleartrip",
                    Url = $"https://www.cleartrip.com/hotels/results/?city={HttpUtility.UrlEncode(ctx.Destination)}" +
                          $"&checkin={ctx.CheckIn:dd/MM/yyyy}&checkout={ctx.CheckOut:dd/MM/yyyy}" +
                          $"&adults={ctx.Adults}&children=0&rooms=1",
                    Logo = "cleartrip"
                },
            };

        // FLIGHTS (Skyscanner and Kayak DENIED - using alternatives)
        private List<DeeplinkOption> BuildFlightOptions(DeeplinkContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.OriginIata) || string.IsNullOrEmpty(ctx.DestIata))
                return new List<DeeplinkOption>();

            return new()
            {
                new DeeplinkOption
                {
                    Platform = "MakeMyTrip",
                    Url = $"https://www.makemytrip.com/flights/international-flights/" +
                          $"{ctx.OriginIata}-to-{ctx.DestIata}/{ctx.CheckIn:ddMMyyyy}/" +
                          $"?tripType=O&paxType=A-{ctx.Adults}_C-0_I-0&cabinClass=E&lang=eng",
                    Logo = "mmt"
                },
                new DeeplinkOption
                {
                    Platform = "Ixigo",
                    Url = $"https://www.ixigo.com/flight/search/flight/" +
                          $"{ctx.OriginIata}-{ctx.DestIata}-{ctx.CheckIn:ddMMyyyy}-null-E-{ctx.Adults}-0-0/ONEWAY",
                    Logo = "ixigo"
                },
            };
        }

        private List<DeeplinkOption> BuildFlightBrowseLinks(DeeplinkContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.OriginIata)) return new();

            return new()
            {
                new DeeplinkOption
                {
                    Platform = "Cleartrip",
                    Url = $"https://www.cleartrip.com/flights/results/?from={ctx.OriginIata}&to={ctx.DestIata}" +
                          $"&depart_date={ctx.CheckIn:dd/MM/yyyy}&adults={ctx.Adults}" +
                          $"&childs=0&infants=0&class=Economy&intl=n",
                    Logo = "cleartrip"
                },
                new DeeplinkOption
                {
                    Platform = "Yatra",
                    Url = $"https://www.yatra.com/flights#pType=1&origin={ctx.OriginIata}&destination={ctx.DestIata}" +
                          $"&departDate={ctx.CheckIn:dd/MM/yyyy}&adult={ctx.Adults}",
                    Logo = "yatra"
                },
                new DeeplinkOption
                {
                    Platform = "EaseMyTrip",
                    Url = $"https://www.easemytrip.com/flights/{ctx.OriginIata}-{ctx.DestIata}-flight-tickets/" +
                          $"?dd={ctx.CheckIn:dd+MMM+yyyy}&pax={ctx.Adults}-0-0&cls=E",
                    Logo = "easemytrip"
                },
            };
        }

        // CABS
        private List<DeeplinkOption> BuildCabOptions(DeeplinkContext ctx) =>
            new()
            {
                new DeeplinkOption
                {
                    Platform = "Uber",
                    Url = $"https://m.uber.com/ul/?action=setPickup" +
                          $"&pickup[latitude]={ctx.PickupLat}&pickup[longitude]={ctx.PickupLng}" +
                          $"&pickup[nickname]={HttpUtility.UrlEncode(ctx.PickupName)}" +
                          $"&dropoff[latitude]={ctx.DropLat}&dropoff[longitude]={ctx.DropLng}" +
                          $"&dropoff[nickname]={HttpUtility.UrlEncode(ctx.DropName)}",
                    Logo = "uber"
                },
                new DeeplinkOption
                {
                    Platform = "Ola",
                    Url = $"https://book.olacabs.com/" +
                          $"?pickup_lat={ctx.PickupLat}&pickup_lng={ctx.PickupLng}" +
                          $"&pickup_name={HttpUtility.UrlEncode(ctx.PickupName)}" +
                          $"&drop_lat={ctx.DropLat}&drop_lng={ctx.DropLng}" +
                          $"&drop_name={HttpUtility.UrlEncode(ctx.DropName)}&utm_source=planora",
                    Logo = "ola"
                },
            };

        private List<DeeplinkOption> BuildCabBrowseLinks(DeeplinkContext ctx) =>
            new()
            {
                new DeeplinkOption
                {
                    Platform = "Rapido",
                    Url = "https://www.rapido.bike/",
                    Logo = "rapido"
                },
                new DeeplinkOption
                {
                    Platform = "InDrive",
                    Url = $"https://indrive.com/en/cities/{ctx.Destination.ToLower().Replace(" ", "-")}/",
                    Logo = "indrive"
                },
            };

        // TRAINS (India)
        private List<DeeplinkOption> BuildTrainOptions(DeeplinkContext ctx) =>
            new()
            {
                new DeeplinkOption
                {
                    Platform = "IRCTC",
                    Url = $"https://www.irctc.co.in/nget/train-search?from={ctx.OriginIata}&to={ctx.DestIata}" +
                          $"&date={ctx.CheckIn:yyyy-MM-dd}&quota=GN&class=3A",
                    Logo = "irctc"
                },
                new DeeplinkOption
                {
                    Platform = "ConfirmTkt",
                    Url = $"https://confirmtkt.com/trains/{ctx.OriginIata}-to-{ctx.DestIata}?date={ctx.CheckIn:yyyy-MM-dd}",
                    Logo = "confirmtkt"
                },
            };

        private List<DeeplinkOption> BuildTrainBrowseLinks(DeeplinkContext ctx) =>
            new()
            {
                new DeeplinkOption
                {
                    Platform = "RailYatri",
                    Url = $"https://www.railyatri.in/trains-between-stations?from={ctx.OriginIata}&to={ctx.DestIata}&date={ctx.CheckIn:yyyy-MM-dd}",
                    Logo = "railyatri"
                },
                new DeeplinkOption
                {
                    Platform = "Ixigo Trains",
                    Url = $"https://www.ixigo.com/train/search/{ctx.OriginIata}/{ctx.DestIata}/{ctx.CheckIn:ddMMyyyy}",
                    Logo = "ixigo"
                },
            };

        // CAR RENTAL
        private List<DeeplinkOption> BuildCarRentalOptions(DeeplinkContext ctx) =>
            new()
            {
                new DeeplinkOption
                {
                    Platform = "Zoomcar",
                    Url = $"https://www.zoomcar.com/{ctx.Destination.ToLower().Replace(" ", "-")}",
                    Logo = "zoomcar"
                },
                new DeeplinkOption
                {
                    Platform = "Revv",
                    Url = $"https://www.revv.co.in/car-rentals/{ctx.Destination.ToLower().Replace(" ", "-")}",
                    Logo = "revv"
                },
            };
    }
}
