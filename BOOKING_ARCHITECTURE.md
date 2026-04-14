# PLANORA — BOOKING ARCHITECTURE (FINAL)
> Feed alongside PROJECT_BIBLE.md and AGENT_INSTRUCTIONS.md
> This fully replaces Step 7 in AGENT_INSTRUCTIONS.md

---

## 1. CORE MENTAL MODEL

Every bookable item in Planora falls into exactly one of three booking types.
The agent must determine booking type at POI creation time (during Research Agent) and store it.
The Booking Section UI then reads this field and renders accordingly.

```
BOOKING_TYPE_A — Aggregator Redirect
  Use when: commodity item (flights, hotels, car rentals)
  How: construct affiliate deeplink with trip params pre-filled
  Platforms: Skyscanner, Booking.com, Agoda, MakeMyTrip, Ixigo, Viator, Uber, Ola

BOOKING_TYPE_B — Official Website Redirect
  Use when: specific venue with its own ticketing (museum, fort, monument, show, park)
  How: official_website + booking_url fetched by Research Agent during generation
  Source: Google Places API `website` field + GPT-4o enrichment
  Examples: Colosseum, Louvre, Vatican, Amber Fort, India Gate, local govt sites

BOOKING_TYPE_C — Offline / Gate Only
  Use when: no online booking exists for this place
  How: show info card (timings, entry fee, directions to ticket counter)
  Do NOT show any booking button — show "Buy at gate" badge instead
```

**Decision rule (stored on every POI as `booking_type` field):**
```
IF poi.booking_url is not null       → BOOKING_TYPE_B
ELSE IF poi.category in              → BOOKING_TYPE_A
  [hotel, flight, cab, car_rental,
   activity_generic, dining]
ELSE IF poi.is_free = true           → BOOKING_TYPE_C (no ticket needed)
ELSE                                 → BOOKING_TYPE_C (offline only)
```

---

## 2. DATA MODEL CHANGES

### Addition to `places_of_interest` MongoDB document:
```json
{
  "poi_id": "UUID",
  "name": "Colosseum",
  "category": "monument",
  "lat": 41.8902, "lng": 12.4922,

  "booking_type": "B",
  "official_website": "https://www.coopculture.it/en/colosseo-e-shop.cfm",
  "booking_url": "https://www.coopculture.it/en/colosseo-e-shop.cfm",
  "requires_advance_booking": true,
  "is_free": false,
  "entry_fee_approx": "€18",
  "opening_hours": "09:00–19:00",
  "booking_notes": "Book 2–3 weeks in advance. Skip-the-line available.",

  "google_place_id": "ChIJrRMgU7ZhLxMRxAOFkC7I8Sg",
  "sort_order": 1
}
```

### New field on `itinerary_days`:
```json
{
  "day_id": "UUID",
  "booking_summary": {
    "has_type_b_pois": true,
    "advance_booking_required": ["Colosseum", "Vatican Museums"],
    "all_booking_urls_fetched": true
  }
}
```

---

## 3. RESEARCH AGENT EXTENSION (Build Step 6)

This is the communication channel between itinerary generation and booking.
The Research Agent fetches all booking data during trip creation.
No separate API call is ever needed at booking time for Type B items.

### ResearchAgent.cs — extend POI fetch:

```csharp
private async Task<PlaceOfInterest> EnrichPoiWithBookingData(
    PlaceOfInterest poi,
    GooglePlaceDetails googleDetails)
{
    // Step 1: Store official website from Google Places API directly
    poi.OfficialWebsite = googleDetails.Website; // Google Places `website` field

    // Step 2: GPT-4o enrichment call for ticketing details
    var prompt = $"""
        Place: {poi.Name}
        City: {poi.City}, {poi.Country}
        Category: {poi.Category}
        Official website (if known): {poi.OfficialWebsite ?? "unknown"}

        Return ONLY valid JSON, no explanation:
        {{
          "booking_url": "direct ticket purchase URL or null",
          "requires_advance_booking": true/false,
          "is_free": true/false,
          "entry_fee_approx": "e.g. €18 or Free or null",
          "opening_hours": "e.g. 09:00-19:00 or null",
          "booking_notes": "important tip in one sentence or null",
          "booking_type": "B if has own ticketing, C if offline/free only"
        }}
        """;

    var result = await _kernel.InvokePromptAsync(prompt);
    var enrichment = JsonSerializer.Deserialize<PoiBookingEnrichment>(result.ToString());

    poi.BookingUrl = enrichment.BookingUrl;
    poi.RequiresAdvanceBooking = enrichment.RequiresAdvanceBooking;
    poi.IsFree = enrichment.IsFree;
    poi.EntryFeeApprox = enrichment.EntryFeeApprox;
    poi.OpeningHours = enrichment.OpeningHours;
    poi.BookingNotes = enrichment.BookingNotes;
    poi.BookingType = enrichment.BookingType;

    return poi;
}
```

### When to call this:
- Called for EVERY POI during Research Agent execution
- Only if `poi.category` is NOT in [hotel, cab, car_rental, dining_generic]
- Hotels/cabs/flights → always BOOKING_TYPE_A, skip enrichment
- Monuments/museums/parks/shows/experiences → always enrich

### GPT-4o knowledge covers (no web search needed):
- All major world museums: Louvre, Uffizi, MoMA, British Museum, Prado
- Major monuments: Colosseum, Eiffel Tower, Sagrada Familia, Acropolis
- Indian ASI monuments: Taj Mahal, Red Fort, Qutub Minar, Humayun's Tomb
- Indian state monuments: Amber Fort, Mysore Palace, Ajanta/Ellora
- Theme parks, national parks with entry systems
- Opera houses, major theatres

### Fallback chain when GPT-4o returns null booking_url:
```
1. Use official_website from Google Places directly (still Type B)
2. If official_website also null → Google search redirect:
   https://www.google.com/search?q={POI_NAME}+{CITY}+official+tickets+online
   (booking_type stays B — user will find it)
3. If poi.is_free = true → booking_type = C, show "Free entry" badge
4. If none of above → booking_type = C, show "Buy at gate" badge
```

---

## 4. AFFILIATE PROGRAM REGISTRATIONS

Register on all before building. Get your IDs. Store in appsettings.

| Platform | Register At | ID Field | Commission |
|----------|------------|----------|------------|
| Skyscanner | partners.skyscanner.net (via Impact.com) | mediaPartnerId | Up to 20% |
| Booking.com | booking.com/affiliate-program | aid | 25–35% of their cut |
| Agoda | partners.agoda.com | cid | Variable |
| Viator | viator.com/partner | mcid | 8% |
| GetYourGuide | partner.getyourguide.com | partner_id | 8% |
| MakeMyTrip | refer.makemytrip.com | utm tracking | Fixed |
| Ixigo | affiliate.ixigo.com | affiliate code | Fixed |

---

## 5. COMPLETE DEEPLINK FORMATS — BOOKING_TYPE_A

### 5A. FLIGHTS

**Skyscanner (primary — always use first)**
```
One-way:
https://skyscanner.net/g/referrals/v1/flights/day-view/
  ?origin={IATA_ORIGIN}
  &destination={IATA_DEST}
  &outboundDate={YYYY-MM-DD}
  &adultsv2={PAX}
  &currency=INR&locale=en-IN&market=IN
  &mediaPartnerId={SKYSCANNER_PARTNER_ID}

Return:
https://skyscanner.net/g/referrals/v1/flights/day-view/
  ?origin={IATA_ORIGIN}&destination={IATA_DEST}
  &outboundDate={TRIP_START}&inboundDate={TRIP_END}
  &adultsv2={PAX}
  &currency=INR&locale=en-IN&market=IN
  &mediaPartnerId={SKYSCANNER_PARTNER_ID}
```

**MakeMyTrip**
```
https://www.makemytrip.com/flights/international-flights/
  {ORIGIN}-to-{DEST}/{DDMMYYYY}/
  ?tripType=O&paxType=A-{PAX}_C-0_I-0&cabinClass=E&lang=eng
```

**Ixigo**
```
https://www.ixigo.com/flight/search/flight/
  {ORIGIN_IATA}-{DEST_IATA}-{DDMMYYYY}-null-E-{PAX}-0-0/ONEWAY
```

**Cleartrip**
```
https://www.cleartrip.com/flights/results/
  ?from={ORIGIN_IATA}&to={DEST_IATA}
  &depart_date={DD/MM/YYYY}&adults={PAX}
  &childs=0&infants=0&class=Economy&intl=n
```

---

### 5B. HOTELS (MAP-BASED — KEY FEATURE)

Hotel search uses geographic center of that day's POI cluster.
This means hotels shown are near where user actually spends the day.

**Center calculation (run per day, not per trip):**
```csharp
var centerLat = day.Pois.Average(p => p.Lat);
var centerLng = day.Pois.Average(p => p.Lng);
// Use these in hotel deeplinks — radius 2km
```

**Booking.com (primary)**
```
Map-based (use this — most precise):
https://www.booking.com/searchresults.html
  ?latitude={CENTER_LAT}&longitude={CENTER_LNG}&radius=2
  &checkin_year={YYYY}&checkin_month={M}&checkin_monthday={D}
  &checkout_year={YYYY}&checkout_month={M}&checkout_monthday={D}
  &group_adults={PAX}&no_rooms=1
  &aid={BOOKING_AID}

City fallback (if lat/lng unavailable):
https://www.booking.com/searchresults.html
  ?ss={CITY_URL_ENCODED}
  &checkin_year={YYYY}&checkin_month={M}&checkin_monthday={D}
  &checkout_year={YYYY}&checkout_month={M}&checkout_monthday={D}
  &group_adults={PAX}&no_rooms=1&aid={BOOKING_AID}
```

**Agoda**
```
https://www.agoda.com/search
  ?latitude={CENTER_LAT}&longitude={CENTER_LNG}
  &checkIn={YYYY-MM-DD}&checkOut={YYYY-MM-DD}
  &rooms=1&adults={PAX}&cid={AGODA_CID}
```

**MakeMyTrip Hotels**
```
https://www.makemytrip.com/hotels/hotel-listing/
  ?city={CITY_CODE}
  &checkin={DDMMYYYY}&checkout={DDMMYYYY}
  &roomStayQualifier=2e0e
  &country=IN
```

---

### 5C. ACTIVITIES (generic — when no specific booking_url exists)

**Viator**
```
https://www.viator.com/search/{POI_NAME_URL_ENCODED}
  ?mcid={VIATOR_MCID}&startDate={YYYY-MM-DD}
```

**GetYourGuide**
```
https://www.getyourguide.com/s/
  ?q={POI_NAME_URL_ENCODED}
  &date_from={YYYY-MM-DD}&date_to={YYYY-MM-DD}
  &partner_id={GYG_PARTNER_ID}
```

---

### 5D. CABS & TRANSFERS

**Uber**
```
https://m.uber.com/ul/?action=setPickup
  &pickup[latitude]={PICKUP_LAT}&pickup[longitude]={PICKUP_LNG}
  &pickup[nickname]={PICKUP_NAME_ENCODED}
  &dropoff[latitude]={DROP_LAT}&dropoff[longitude]={DROP_LNG}
  &dropoff[nickname]={DROP_NAME_ENCODED}

Logic: pickup = current hotel lat/lng, drop = first POI of the day
```

**Ola**
```
Web fallback:
https://book.olacabs.com/
  ?pickup_lat={LAT}&pickup_lng={LNG}
  &pickup_name={PICKUP_ENCODED}
  &drop_lat={LAT}&drop_lng={LNG}
  &drop_name={DROP_ENCODED}
  &utm_source=planora

App deeplink (try first with Linking.canOpenURL):
ola://open?lat={LAT}&lng={LNG}&drop_lat={LAT}&drop_lng={LNG}
```

---

### 5E. TRAINS (India)

**IRCTC**
```
https://www.irctc.co.in/nget/train-search
  ?from={STATION_CODE}&to={STATION_CODE}
  &date={YYYY-MM-DD}&quota=GN
  &class=3A  (SL=Sleeper, 3A=AC3, 2A=AC2, 1A=AC1)
```

**ConfirmTkt (better UX)**
```
https://confirmtkt.com/trains/
  {ORIGIN_STATION}-to-{DEST_STATION}
  ?date={YYYY-MM-DD}
```

---

### 5F. DINING

**Zomato**
```
Near day POIs:
https://www.zomato.com/{CITY_SLUG}/restaurants
  ?lat={CENTER_LAT}&lng={CENTER_LNG}&rad=500
```

**EazyDiner**
```
https://www.eazydiner.com/restaurant-search
  ?city={CITY}&date={YYYY-MM-DD}&covers={PAX}
```

---

### 5G. EVENTS & SHOWS

**BookMyShow (India)**
```
https://in.bookmyshow.com/explore/events-{CITY_SLUG}
```

**Ticketmaster (international)**
```
https://www.ticketmaster.com/search
  ?q={EVENT_OR_VENUE_NAME}&dateStart={YYYY-MM-DD}
```

**Eventbrite (global)**
```
https://www.eventbrite.com/d/{COUNTRY_SLUG}/{CITY_SLUG}/
  ?q={QUERY}&start_date={YYYY-MM-DD}
```

---

## 6. BOOKING_TYPE_B — DIRECT OFFICIAL WEBSITE FLOW

For Type B POIs, official_website and booking_url are already stored on the POI.
No API call needed at booking time. Just read and redirect.

### Known portals by region (for fallback hints in prompt):

**India — Central:**
```
ASI monuments:    https://asi.payumoney.com/
                  Search: https://asi.payumoney.com/index.php#monuments
```

**India — State:**
```
Rajasthan:        https://www.rajasthantourism.gov.in/
Maharashtra:      https://www.maharashtratourism.gov.in/
Karnataka:        https://www.karnatakatourism.org/
Kerala:           https://www.keralatourism.org/
Tamil Nadu:       https://www.tamilnadutourism.tn.gov.in/
```

**Europe:**
```
Italy (Colosseum, Forum): https://www.coopculture.it/en/
Vatican:          https://www.museivaticani.va/
Louvre:           https://www.louvre.fr/en/
Uffizi (Florence): https://www.uffizi.it/en/
Sagrada Familia:  https://sagradafamilia.org/en/
Acropolis:        https://etickets.tap.gr/
Eiffel Tower:     https://www.toureiffel.paris/en/
```

**UK:**
```
National museums: generally free, no ticket needed (Type C)
Tower of London:  https://www.hrp.org.uk/
Stonehenge:       https://www.english-heritage.org.uk/
```

**USA:**
```
National Parks:   https://www.recreation.gov/
Smithsonian:      Free (Type C)
MoMA:             https://www.moma.org/tickets/
```

**Asia:**
```
Angkor Wat:       https://www.angkorwatticket.com/
Forbidden City:   https://gugong.emuseum.com/
Bali temples:     Generally at-gate cash (Type C)
Japan shrines:    Generally free (Type C) except some:
                  https://www.fushimi-inari.jp/
```

> GPT-4o has this knowledge built in. The above is reference only.
> The Research Agent's GPT-4o call will return correct URLs for all major sites.
> This list helps when GPT-4o returns null for an obscure location.

---

## 7. BOOKING_TYPE_C — OFFLINE INFO CARD

When booking_type = C, show this card instead of any booking button:

```
┌──────────────────────────────────────┐
│  🏛️  {POI_NAME}                      │
│                                      │
│  🎫 Tickets: Buy at entrance gate    │
│  💰 Entry: {entry_fee_approx}        │
│  🕐 Hours: {opening_hours}           │
│  📍 {address}                        │
│                                      │
│  [Get Directions]  [Search Online]   │
│                                      │
│  Note: {booking_notes}               │
└──────────────────────────────────────┘
```

"Search Online" button → Google search fallback:
```
https://www.google.com/search?q={POI_NAME}+{CITY}+tickets+entry
```

---

## 8. BOOKING SECTION UI FLOW

### Entry Points
- Trip Card → "Book" tab (alongside Days, Map, Vault tabs)
- Each day card → "Book for Day {N}" shortcut button
- Each POI card → booking chip showing its type

### Booking Hub Screen
```
┌─────────────────────────────────────┐
│  Book Your Jaipur Trip              │
│  Dec 1–7 · 2 travellers            │
├─────────────────────────────────────┤
│  ✈️ Flights      🏨 Hotels          │
│  🎯 Activities   🚕 Cabs            │
│  🚂 Trains       🍽️ Dining          │
│  🎭 Events       🎫 Tickets         │
├─────────────────────────────────────┤
│  ⚠️  Advance booking needed:        │
│  • Amber Fort (book 3 days ahead)   │
│  • Light & Sound Show               │
└─────────────────────────────────────┘
```

The "Advance booking needed" banner reads from `itinerary_days.booking_summary.advance_booking_required` — populated during generation.

### Hotel Screen — Day Selector + Map
```
┌─────────────────────────────────────┐
│  Hotels  [Day1] [Day2] [Day3]...    │
├─────────────────────────────────────┤
│  Day 2 · Dec 2 → Dec 3             │
│  Near: City Palace area             │
│  [Mini map of Day 2 POI cluster]    │
│  Search radius: 2km                 │
├─────────────────────────────────────┤
│  🌐 Booking.com  → Open ↗          │
│  🌐 Agoda        → Open ↗          │
│  🌐 MakeMyTrip   → Open ↗          │
└─────────────────────────────────────┘
```

### Activities / Tickets Screen — POI-Driven
```
Day 2 Activities

Amber Fort                [Type B]
  🎫 Official Tickets ↗  (booking_url from POI)
  Also on: Viator ↗ | GetYourGuide ↗

City Palace Museum        [Type B]
  🎫 Official Site ↗     (official_website from POI)

Street Food Walk          [Type A]
  🎯 Viator ↗ | GetYourGuide ↗

Albert Hall Museum        [Type C]
  🎫 Buy at gate · ₹40 · Open 10am–5pm
  [Get Directions]
```

---

## 9. BACKEND IMPLEMENTATION

### BookingController.cs

```csharp
[HttpGet("options/{tripId}/{category}")]
public async Task<IActionResult> GetBookingOptions(
    string tripId, string category,
    [FromQuery] int? dayNumber)
{
    var trip = await _tripRepo.GetByIdAsync(tripId);
    if (trip == null) return NotFound();

    // For hotel: get day POI center
    double centerLat = 0, centerLng = 0;
    ItineraryDay? day = null;
    if (dayNumber.HasValue)
    {
        day = await _dayRepo.GetByTripAndDayAsync(tripId, dayNumber.Value);
        if (day?.Pois?.Any() == true)
        {
            centerLat = day.Pois.Average(p => p.Lat);
            centerLng = day.Pois.Average(p => p.Lng);
        }
    }

    // Check cache
    var cacheKey = $"booking:{tripId}:{category}:{dayNumber}";
    var cached = await _cache.GetAsync<List<BookingOption>>(cacheKey);
    if (cached != null) return Ok(cached);

    var options = category.ToLower() switch
    {
        "flight"   => BuildFlightOptions(trip),
        "hotel"    => BuildHotelOptions(trip, day, centerLat, centerLng),
        "activity" => BuildActivityOptions(trip, day),
        "cab"      => BuildCabOptions(trip, day),
        "train"    => BuildTrainOptions(trip),
        "dining"   => BuildDiningOptions(trip, day, centerLat, centerLng),
        "events"   => BuildEventOptions(trip),
        _          => new List<BookingOption>()
    };

    await _cache.SetAsync(cacheKey, options, TimeSpan.FromMinutes(15));
    return Ok(options);
}

// Activity options — key method showing Type A vs B decision
private List<BookingOption> BuildActivityOptions(Trip trip, ItineraryDay? day)
{
    var options = new List<BookingOption>();
    if (day?.Pois == null) return options;

    foreach (var poi in day.Pois)
    {
        var poiOptions = new PoiBookingOptions { PoiId = poi.PoiId, PoiName = poi.Name };

        if (poi.BookingType == "B")
        {
            // Type B — use pre-fetched official URL
            if (!string.IsNullOrEmpty(poi.BookingUrl))
                poiOptions.Options.Add(new BookingOption
                {
                    Platform = "Official Website",
                    DeeplinkUrl = poi.BookingUrl,
                    IsOfficial = true,
                    Description = poi.BookingNotes,
                    RequiresAdvanceBooking = poi.RequiresAdvanceBooking
                });
            else if (!string.IsNullOrEmpty(poi.OfficialWebsite))
                poiOptions.Options.Add(new BookingOption
                {
                    Platform = "Official Website",
                    DeeplinkUrl = poi.OfficialWebsite,
                    IsOfficial = true
                });

            // Also offer Viator/GYG as alternatives for Type B
            poiOptions.Options.Add(_builder.BuildViatorActivity(poi.Name, trip.Destination.City, day.Date));
            poiOptions.Options.Add(_builder.BuildGygActivity(poi.Name, day.Date));
        }
        else if (poi.BookingType == "A")
        {
            // Type A — aggregator platforms only
            poiOptions.Options.Add(_builder.BuildViatorActivity(poi.Name, trip.Destination.City, day.Date));
            poiOptions.Options.Add(_builder.BuildGygActivity(poi.Name, day.Date));
        }
        else // Type C
        {
            poiOptions.IsOfflineOnly = true;
            poiOptions.OfflineInfo = new OfflineInfo
            {
                EntryFee = poi.EntryFeeApprox,
                OpeningHours = poi.OpeningHours,
                Note = poi.BookingNotes,
                IsFree = poi.IsFree,
                GoogleSearchUrl = $"https://www.google.com/search?q=" +
                    Uri.EscapeDataString($"{poi.Name} {trip.Destination.City} tickets entry")
            };
        }

        options.Add(poiOptions);
    }
    return options;
}
```

### AffiliateLinkBuilder.cs

```csharp
public class AffiliateLinkBuilder
{
    private readonly IConfiguration _config;
    public AffiliateLinkBuilder(IConfiguration config) => _config = config;

    // FLIGHTS
    public BookingOption BuildSkyscannerFlight(string origin, string dest,
        DateTime outbound, DateTime? inbound, int adults) => new()
    {
        Platform = "Skyscanner",
        LogoUrl = "assets/logos/skyscanner.png",
        DeeplinkUrl = $"https://skyscanner.net/g/referrals/v1/flights/day-view/" +
            $"?origin={origin}&destination={dest}&outboundDate={outbound:yyyy-MM-dd}" +
            (inbound.HasValue ? $"&inboundDate={inbound:yyyy-MM-dd}" : "") +
            $"&adultsv2={adults}&currency=INR&locale=en-IN&market=IN" +
            $"&mediaPartnerId={_config["Affiliates:SkyscannerPartnerId"]}"
    };

    public BookingOption BuildMakeMyTripFlight(string origin, string dest,
        DateTime outbound, int adults) => new()
    {
        Platform = "MakeMyTrip",
        LogoUrl = "assets/logos/mmt.png",
        DeeplinkUrl = $"https://www.makemytrip.com/flights/international-flights/" +
            $"{origin}-to-{dest}/{outbound:ddMMyyyy}/" +
            $"?tripType=O&paxType=A-{adults}_C-0_I-0&cabinClass=E&lang=eng"
    };

    public BookingOption BuildIxigoFlight(string origin, string dest,
        DateTime outbound, int adults) => new()
    {
        Platform = "Ixigo",
        LogoUrl = "assets/logos/ixigo.png",
        DeeplinkUrl = $"https://www.ixigo.com/flight/search/flight/" +
            $"{origin}-{dest}-{outbound:ddMMyyyy}-null-E-{adults}-0-0/ONEWAY"
    };

    // HOTELS
    public BookingOption BuildBookingComHotel(double lat, double lng,
        DateTime checkIn, DateTime checkOut, int adults) => new()
    {
        Platform = "Booking.com",
        LogoUrl = "assets/logos/bookingcom.png",
        Description = "Near your day's spots",
        DeeplinkUrl = $"https://www.booking.com/searchresults.html" +
            $"?latitude={lat}&longitude={lng}&radius=2" +
            $"&checkin_year={checkIn.Year}&checkin_month={checkIn.Month}" +
            $"&checkin_monthday={checkIn.Day}" +
            $"&checkout_year={checkOut.Year}&checkout_month={checkOut.Month}" +
            $"&checkout_monthday={checkOut.Day}" +
            $"&group_adults={adults}&no_rooms=1" +
            $"&aid={_config["Affiliates:BookingComAid"]}"
    };

    public BookingOption BuildAgodaHotel(double lat, double lng,
        DateTime checkIn, DateTime checkOut, int adults) => new()
    {
        Platform = "Agoda",
        LogoUrl = "assets/logos/agoda.png",
        DeeplinkUrl = $"https://www.agoda.com/search" +
            $"?latitude={lat}&longitude={lng}" +
            $"&checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}" +
            $"&rooms=1&adults={adults}&cid={_config["Affiliates:AgodaCid"]}"
    };

    // ACTIVITIES
    public BookingOption BuildViatorActivity(string poiName,
        string city, DateTime date) => new()
    {
        Platform = "Viator",
        LogoUrl = "assets/logos/viator.png",
        DeeplinkUrl = $"https://www.viator.com/search/" +
            $"{Uri.EscapeDataString(poiName)}" +
            $"?mcid={_config["Affiliates:ViatorMcid"]}&startDate={date:yyyy-MM-dd}"
    };

    public BookingOption BuildGygActivity(string poiName, DateTime date) => new()
    {
        Platform = "GetYourGuide",
        LogoUrl = "assets/logos/gyg.png",
        DeeplinkUrl = $"https://www.getyourguide.com/s/" +
            $"?q={Uri.EscapeDataString(poiName)}" +
            $"&date_from={date:yyyy-MM-dd}&date_to={date:yyyy-MM-dd}" +
            $"&partner_id={_config["Affiliates:GygPartnerId"]}"
    };

    // CABS
    public BookingOption BuildUber(double pickupLat, double pickupLng,
        string pickupName, double dropLat, double dropLng, string dropName) => new()
    {
        Platform = "Uber",
        LogoUrl = "assets/logos/uber.png",
        DeeplinkUrl = $"https://m.uber.com/ul/?action=setPickup" +
            $"&pickup[latitude]={pickupLat}&pickup[longitude]={pickupLng}" +
            $"&pickup[nickname]={Uri.EscapeDataString(pickupName)}" +
            $"&dropoff[latitude]={dropLat}&dropoff[longitude]={dropLng}" +
            $"&dropoff[nickname]={Uri.EscapeDataString(dropName)}"
    };

    public BookingOption BuildOla(double pickupLat, double pickupLng,
        string pickupName, double dropLat, double dropLng, string dropName) => new()
    {
        Platform = "Ola",
        LogoUrl = "assets/logos/ola.png",
        DeeplinkUrl = $"https://book.olacabs.com/" +
            $"?pickup_lat={pickupLat}&pickup_lng={pickupLng}" +
            $"&pickup_name={Uri.EscapeDataString(pickupName)}" +
            $"&drop_lat={dropLat}&drop_lng={dropLng}" +
            $"&drop_name={Uri.EscapeDataString(dropName)}" +
            $"&utm_source=planora"
    };

    // TRAINS
    public BookingOption BuildIrctc(string fromCode, string toCode,
        DateTime date, string trainClass = "3A") => new()
    {
        Platform = "IRCTC",
        LogoUrl = "assets/logos/irctc.png",
        DeeplinkUrl = $"https://www.irctc.co.in/nget/train-search" +
            $"?from={fromCode}&to={toCode}" +
            $"&date={date:yyyy-MM-dd}&quota=GN&class={trainClass}"
    };

    public BookingOption BuildConfirmTkt(string from, string to, DateTime date) => new()
    {
        Platform = "ConfirmTkt",
        LogoUrl = "assets/logos/confirmtkt.png",
        DeeplinkUrl = $"https://confirmtkt.com/trains/{from}-to-{to}?date={date:yyyy-MM-dd}"
    };

    // DINING
    public BookingOption BuildZomato(string citySlug,
        double lat, double lng) => new()
    {
        Platform = "Zomato",
        LogoUrl = "assets/logos/zomato.png",
        DeeplinkUrl = $"https://www.zomato.com/{citySlug}/restaurants" +
            $"?lat={lat}&lng={lng}&rad=500"
    };

    // EVENTS
    public BookingOption BuildBookMyShow(string citySlug) => new()
    {
        Platform = "BookMyShow",
        LogoUrl = "assets/logos/bookmyshow.png",
        DeeplinkUrl = $"https://in.bookmyshow.com/explore/events-{citySlug}"
    };

    public BookingOption BuildTicketmaster(string query, DateTime date) => new()
    {
        Platform = "Ticketmaster",
        LogoUrl = "assets/logos/ticketmaster.png",
        DeeplinkUrl = $"https://www.ticketmaster.com/search" +
            $"?q={Uri.EscapeDataString(query)}&dateStart={date:yyyy-MM-dd}"
    };
}
```

---

## 10. FRONTEND — ROUTING LOGIC

```typescript
// BookingOption type
interface BookingOption {
  platform: string;
  deeplinkUrl: string;
  logoUrl: string;
  isOfficial?: boolean;
  requiresAdvanceBooking?: boolean;
  indicativePrice?: string;
  description?: string;
}

interface PoiBookingOptions {
  poiId: string;
  poiName: string;
  bookingType: 'A' | 'B' | 'C';
  options: BookingOption[];
  isOfflineOnly?: boolean;
  offlineInfo?: {
    entryFee: string;
    openingHours: string;
    note: string;
    isFree: boolean;
    googleSearchUrl: string;
  };
}

// Tap handler — same for all types A and B
const handleBookingTap = async (option: BookingOption) => {
  await logBookingRedirect(tripId, option.platform); // analytics
  const canOpen = await Linking.canOpenURL(option.deeplinkUrl);
  if (canOpen) {
    await Linking.openURL(option.deeplinkUrl);
  } else {
    await WebBrowser.openBrowserAsync(option.deeplinkUrl);
  }
};

// Rendering logic per POI
const renderPoiBooking = (poi: PoiBookingOptions) => {
  if (poi.bookingType === 'C') {
    return <OfflineInfoCard info={poi.offlineInfo} />;
  }
  return (
    <View>
      {poi.bookingType === 'B' && poi.options[0]?.isOfficial && (
        <OfficialBadge />  // "Official Website" highlighted at top
      )}
      {poi.options.map(opt => (
        <PlatformButton key={opt.platform} option={opt} onTap={handleBookingTap} />
      ))}
    </View>
  );
};
```

---

## 11. APPSETTINGS ADDITIONS

```json
{
  "Affiliates": {
    "SkyscannerPartnerId": "PASTE_FROM_IMPACT",
    "BookingComAid": "PASTE_FROM_BOOKING_DASHBOARD",
    "AgodaCid": "PASTE_FROM_AGODA",
    "ViatorMcid": "PASTE_FROM_VIATOR",
    "GygPartnerId": "PASTE_FROM_GYG"
  },
  "BookingDemandApi": {
    "ApiKey": "PASTE_AFTER_APPROVAL",
    "AffiliateId": "PASTE_AFTER_APPROVAL",
    "BaseUrl": "https://demandapi.booking.com/3.1",
    "Enabled": false
  }
}
```

---

## 12. TIER 2 — BOOKING.COM DEMAND API (add post-launch)

Currently `Enabled: false`. Flip to true once approved as Managed Affiliate Partner.

When enabled: hotel cards show with real photos + prices INSIDE Planora before redirect.
Apply at developers.booking.com once app is live with users.

```
POST https://demandapi.booking.com/3.1/accommodations/search
Headers: Authorization: Bearer {API_KEY}, X-Affiliate-Id: {ID}
Body: { city_id, checkin, checkout, guests: { rooms, adults } }
Returns: hotels with name, price, photos, rating, booking deeplink
```

Falls back to affiliate deeplink automatically if API disabled or fails.

---

## 13. FILES TO CREATE — BUILD STEP 7

```
PlanoraApp.API/Services/BookingService/
  BookingController.cs
  AffiliateLinkBuilder.cs
  PriceCacheService.cs          ← Upstash Redis, 15min TTL
  Models/BookingOption.cs
  Models/PoiBookingOptions.cs
  Models/OfflineInfo.cs
  Models/BookingRequest.cs

PlanoraApp/app/bookings/
  hub.tsx                       ← 8-category grid + advance warning banner
  compare.tsx                   ← platform list + tap handler
  hotel-day.tsx                 ← day tabs + mini map + hotel platforms
  activity-poi.tsx              ← per-POI Type A/B/C rendering
  flight.tsx                    ← flight options with origin/dest/dates
  cab.tsx                       ← Uber + Ola with hotel → first POI routing
  train.tsx                     ← IRCTC + ConfirmTkt
  dining.tsx                    ← Zomato + EazyDiner near day center
  events.tsx                    ← BookMyShow + Ticketmaster + Eventbrite
```

**Also update ResearchAgent.cs (Build Step 6):**
- Add `EnrichPoiWithBookingData()` call for every non-hotel/non-cab POI
- Store booking_type, booking_url, official_website, entry_fee_approx, opening_hours on POI

---

## 14. STEP 7 TEST CHECKLIST

```
[ ] Type A flight → Skyscanner opens with correct IATA codes + dates
[ ] Type A hotel Day 2 → Booking.com opens near Day 2 POI cluster (lat/lng)
[ ] Type B POI (e.g. Colosseum) → "Official Website" button present + correct URL
[ ] Type B POI shows Viator/GYG as alternative below official
[ ] Type C POI → no booking button, offline info card shown
[ ] Type C free entry → "Free entry" badge, no fee shown
[ ] Uber deeplink → opens with hotel as pickup + first POI as drop
[ ] IRCTC deeplink → correct station codes + date
[ ] Redis cache hit → second identical request returns in <50ms
[ ] Booking.com Demand API disabled → falls back to affiliate deeplink silently
[ ] All links open in device browser (Linking.openURL), not WebView
[ ] Advance booking warning banner → reads from booking_summary on trip
```
