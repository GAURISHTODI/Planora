# PLANORA — BOOKING ARCHITECTURE (v2.0)
> Feed alongside PROJECT_BIBLE.md and AGENT_INSTRUCTIONS.md
> This fully replaces Step 7 in AGENT_INSTRUCTIONS.md
> **v2.0:** Skyscanner DENIED. Kayak DENIED. Primary affiliate is now **Booking.com via AWIN**. GCP Free Plan active. Bookings Tab removed — booking options are contextual inside each trip.

---

## CRITICAL CHANGES FROM v1.0

| Item | v1.0 | v2.0 |
|------|------|------|
| Skyscanner | Primary flight affiliate | DENIED — remove entirely |
| Kayak | Secondary | DENIED — remove entirely |
| Booking.com | Hotel affiliate | CONFIRMED via AWIN — primary for hotels AND activities |
| Bookings Tab | Standalone bottom nav tab | REMOVED — bookings are inside trips |
| Booking access | Separate /bookings route | Inside trip card: Days \| Map \| Book \| Vault tabs |

---

## 1. CORE MENTAL MODEL

Every bookable item in Planora falls into exactly one of three booking types.
Determined at POI creation time (Research Agent / Spot Enrichment Agent). Stored on POI.

```
BOOKING_TYPE_A — Aggregator Redirect
  Use when: commodity item (flights, hotels, cabs, generic activities)
  How: construct affiliate deeplink with trip params pre-filled
  Platforms: Booking.com (AWIN), Agoda, MakeMyTrip, Ixigo, Viator, Uber, Ola

BOOKING_TYPE_B — Official Website Redirect
  Use when: specific venue with its own ticketing (museum, fort, monument, show, park)
  How: official_website + booking_url fetched during POI enrichment
  Source: Google Places API `website` field + GPT-4o enrichment

BOOKING_TYPE_C — Offline / Gate Only
  Use when: no online booking exists for this place
  How: show info card (timings, entry fee, directions to ticket counter)
  Do NOT show any booking button — show "Buy at gate" badge instead
```

**Decision rule (stored on every POI as `booking_type`):**
```
IF poi.booking_url is not null       → BOOKING_TYPE_B
ELSE IF poi.category in              → BOOKING_TYPE_A
  [hotel, flight, cab, car_rental,
   activity_generic, dining]
ELSE IF poi.is_free = true           → BOOKING_TYPE_C (no ticket needed)
ELSE                                 → BOOKING_TYPE_C (offline only)
```

---

## 2. DATA MODEL

### POI document (Cosmos DB):
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

### Day document — `booking_summary` field:
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

## 3. POI ENRICHMENT (Research Agent + Spot Enrichment Agent)

This enrichment runs in two contexts:
- **Full AI generation:** ResearchAgent calls `EnrichPoiWithBookingData()` for every non-hotel/cab POI
- **Manual Builder / Edit Mode add:** SpotEnrichmentAgent calls the same enrichment logic per spot

### EnrichPoiWithBookingData (C#):

```csharp
private async Task<PlaceOfInterest> EnrichPoiWithBookingData(
    PlaceOfInterest poi,
    GooglePlaceDetails googleDetails)
{
    poi.OfficialWebsite = googleDetails.Website;

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

### Fallback chain when GPT-4o returns null booking_url:
```
1. Use official_website from Google Places directly (still Type B)
2. If official_website also null → Google search redirect:
   https://www.google.com/search?q={POI_NAME}+{CITY}+official+tickets+online
3. If poi.is_free = true → booking_type = C, show "Free entry" badge
4. If none of above → booking_type = C, show "Buy at gate" badge
```

---

## 4. AFFILIATE PROGRAMME STATUS

| Platform | Status | Register At | ID Field |
|----------|--------|------------|----------|
| **Booking.com (via AWIN)** | ✅ CONFIRMED | awin.com → Booking.com programme | `aid` |
| Skyscanner | ❌ DENIED | — | — |
| Kayak | ❌ DENIED | — | — |
| Agoda | Pending | partners.agoda.com | `cid` |
| Viator | Pending | viator.com/partner | `mcid` |
| GetYourGuide | Pending | partner.getyourguide.com | `partner_id` |
| MakeMyTrip | Plain deeplink | — | utm params only |
| Ixigo | Plain deeplink | — | — |
| Others | Plain deeplink | — | — |

> For platforms without affiliate approval, use plain deeplinks. They still provide user value and a smooth experience. Affiliate IDs can be added later without changing the deeplink structure.

---

## 5. COMPLETE DEEPLINK FORMATS

### 5A. FLIGHTS

> ⚠ Skyscanner and Kayak are NOT to be used — both denied. Use the below only.

**MakeMyTrip (primary for flights)**
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

### 5B. HOTELS (MAP-BASED)

Hotel search uses geographic center of that day's POI cluster.

**Center calculation (run per day):**
```csharp
var centerLat = day.Pois.Average(p => p.Lat);
var centerLng = day.Pois.Average(p => p.Lng);
```

**Booking.com (primary — AWIN confirmed)**
```
Map-based:
https://www.booking.com/searchresults.html
  ?latitude={CENTER_LAT}&longitude={CENTER_LNG}&radius=2
  &checkin_year={YYYY}&checkin_month={M}&checkin_monthday={D}
  &checkout_year={YYYY}&checkout_month={M}&checkout_monthday={D}
  &group_adults={PAX}&no_rooms=1
  &aid={BOOKING_AID}

City fallback:
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
  &roomStayQualifier=2e0e&country=IN
```

---

### 5C. ACTIVITIES & TOURS

**Booking.com Activities (AWIN — use as primary for generic activities)**
```
https://www.booking.com/attractions/searchresults.html
  ?ss={CITY_OR_POI_NAME_ENCODED}
  &checkin={YYYY-MM-DD}&checkout={YYYY-MM-DD}
  &aid={BOOKING_AID}
```

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

Logic: pickup = hotel lat/lng, drop = first POI of the day
```

**Ola**
```
App deeplink (try first):
ola://open?lat={LAT}&lng={LNG}&drop_lat={LAT}&drop_lng={LNG}

Web fallback:
https://book.olacabs.com/
  ?pickup_lat={LAT}&pickup_lng={LNG}
  &pickup_name={PICKUP_ENCODED}
  &drop_lat={LAT}&drop_lng={LNG}
  &drop_name={DROP_ENCODED}
  &utm_source=planora
```

---

### 5E. TRAINS (India)

**IRCTC**
```
https://www.irctc.co.in/nget/train-search
  ?from={STATION_CODE}&to={STATION_CODE}
  &date={YYYY-MM-DD}&quota=GN
  &class=3A
```

**ConfirmTkt**
```
https://confirmtkt.com/trains/
  {ORIGIN_STATION}-to-{DEST_STATION}
  ?date={YYYY-MM-DD}
```

---

### 5F. DINING

**Zomato**
```
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

**Eventbrite**
```
https://www.eventbrite.com/d/{COUNTRY_SLUG}/{CITY_SLUG}/
  ?q={QUERY}&start_date={YYYY-MM-DD}
```

---

## 6. BOOKING_TYPE_B — DIRECT OFFICIAL WEBSITE FLOW

Official website and booking_url are pre-fetched during POI enrichment.
No API call at booking time. Just read and redirect.

### Known portals (GPT-4o fallback reference):

**India — Central (ASI):**
```
https://asi.payumoney.com/
```

**India — State:**
```
Rajasthan: https://www.rajasthantourism.gov.in/
Maharashtra: https://www.maharashtratourism.gov.in/
Karnataka: https://www.karnatakatourism.org/
Kerala: https://www.keralatourism.org/
Tamil Nadu: https://www.tamilnadutourism.tn.gov.in/
```

**Europe:**
```
Italy (Colosseum): https://www.coopculture.it/en/
Vatican: https://www.museivaticani.va/
Louvre: https://www.louvre.fr/en/
Eiffel Tower: https://www.toureiffel.paris/en/
Sagrada Familia: https://sagradafamilia.org/en/
Acropolis: https://etickets.tap.gr/
```

**USA:**
```
National Parks: https://www.recreation.gov/
MoMA: https://www.moma.org/tickets/
```

---

## 7. BOOKING_TYPE_C — OFFLINE INFO CARD

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

"Search Online" → `https://www.google.com/search?q={POI_NAME}+{CITY}+tickets+entry`

---

## 8. BOOKING UI FLOW (inside Trip Card — no standalone Bookings tab)

### Entry Points:
- Trip Card → "Book" tab (alongside Days, Map, Vault within trip detail)
- Each POI card inline → booking section below the POI details
- Each day card → "Book for Day N" shortcut

### Booking Hub Screen (inside trip):
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

### Activities / Tickets Screen — POI-Driven:
```
Day 2 Activities

Amber Fort                [Type B]
  🎫 Official Tickets ↗  (booking_url from POI)
  Also on: Booking.com Activities ↗ | Viator ↗

City Palace Museum        [Type B]
  🎫 Official Site ↗     (official_website from POI)

Street Food Walk          [Type A]
  🎯 Booking.com Activities ↗ | Viator ↗ | GetYourGuide ↗

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
```

### AffiliateLinkBuilder.cs

```csharp
public class AffiliateLinkBuilder
{
    private readonly IConfiguration _config;
    public AffiliateLinkBuilder(IConfiguration config) => _config = config;

    // FLIGHTS — no Skyscanner, no Kayak
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

    public BookingOption BuildCleartripFlight(string origin, string dest,
        DateTime outbound, int adults) => new()
    {
        Platform = "Cleartrip",
        LogoUrl = "assets/logos/cleartrip.png",
        DeeplinkUrl = $"https://www.cleartrip.com/flights/results/" +
            $"?from={origin}&to={dest}&depart_date={outbound:dd/MM/yyyy}" +
            $"&adults={adults}&childs=0&infants=0&class=Economy&intl=n"
    };

    // HOTELS — Booking.com primary (AWIN confirmed)
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

    // ACTIVITIES — Booking.com Activities as primary (AWIN confirmed)
    public BookingOption BuildBookingComActivities(string poiName,
        string city, DateTime date) => new()
    {
        Platform = "Booking.com",
        LogoUrl = "assets/logos/bookingcom.png",
        DeeplinkUrl = $"https://www.booking.com/attractions/searchresults.html" +
            $"?ss={Uri.EscapeDataString(poiName + " " + city)}" +
            $"&checkin={date:yyyy-MM-dd}" +
            $"&aid={_config["Affiliates:BookingComAid"]}"
    };

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

## 10. FRONTEND TYPES + ROUTING

```typescript
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

const handleBookingTap = async (option: BookingOption) => {
  await logBookingRedirect(tripId, option.platform);
  const canOpen = await Linking.canOpenURL(option.deeplinkUrl);
  if (canOpen) {
    await Linking.openURL(option.deeplinkUrl);
  } else {
    await WebBrowser.openBrowserAsync(option.deeplinkUrl);
  }
};

const renderPoiBooking = (poi: PoiBookingOptions) => {
  if (poi.bookingType === 'C') return <OfflineInfoCard info={poi.offlineInfo} />;
  return (
    <View>
      {poi.bookingType === 'B' && poi.options[0]?.isOfficial && <OfficialBadge />}
      {poi.options.map(opt => (
        <PlatformButton key={opt.platform} option={opt} onTap={handleBookingTap} />
      ))}
    </View>
  );
};
```

---

## 11. APPSETTINGS — BOOKING KEYS

```json
{
  "Affiliates": {
    "BookingComAid": "PASTE_FROM_AWIN_DASHBOARD",
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

> Note: `SkyscannerPartnerId` key removed — Skyscanner denied.

---

## 12. TIER 2 — BOOKING.COM DEMAND API (post-launch)

Currently `Enabled: false`. Flip to true once approved as Managed Affiliate Partner.

When enabled: hotel cards show with real photos + prices INSIDE Planora before redirect.
Apply at developers.booking.com once app is live with users.

Falls back to AWIN affiliate deeplink automatically if API disabled or fails.

---

## 13. FILES TO CREATE — BUILD STEP 7

```
PlanoraApp.API/Services/BookingService/
  BookingController.cs
  AffiliateLinkBuilder.cs
  PriceCacheService.cs          ← Azure Cache for Redis, 15min TTL
  Models/BookingOption.cs
  Models/PoiBookingOptions.cs
  Models/OfflineInfo.cs
  Models/BookingRequest.cs

PlanoraApp/app/trip/[trip_id]/book/
  hub.tsx                       ← 8-category grid + advance warning banner
  hotel-day.tsx                 ← day tabs + mini map + hotel platforms
  activity-poi.tsx              ← per-POI Type A/B/C rendering
  flight.tsx                    ← MakeMyTrip, Ixigo, Cleartrip (NO Skyscanner)
  cab.tsx                       ← Uber + Ola
  train.tsx                     ← IRCTC + ConfirmTkt
  dining.tsx                    ← Zomato + EazyDiner
  events.tsx                    ← BookMyShow + Ticketmaster + Eventbrite
```

**Also update ResearchAgent.cs (Build Step 6):**
- Add `EnrichPoiWithBookingData()` for every non-hotel/cab POI
- Booking.com Activities added as primary Type A activity option

---

## 14. STEP 7 TEST CHECKLIST

```
[ ] Type A flight → MakeMyTrip and Ixigo open with correct params (NO Skyscanner)
[ ] Type A hotel Day 2 → Booking.com opens near Day 2 POI cluster + contains aid= param
[ ] Type A activity → Booking.com Activities deeplink present + Viator/GYG alternatives
[ ] Type B POI (e.g. Colosseum) → "Official Website" button present + correct URL
[ ] Type B POI shows Booking.com Activities + Viator/GYG as alternatives below official
[ ] Type C POI → no booking button, offline info card shown
[ ] Type C free entry → "Free entry" badge
[ ] Uber deeplink → opens with hotel as pickup + first POI as drop
[ ] IRCTC deeplink → correct station codes + date
[ ] Redis cache hit → second identical request < 50ms
[ ] All links open in device browser (Linking.openURL), not WebView
[ ] Advance booking warning banner → reads from booking_summary on trip
[ ] No Skyscanner or Kayak links anywhere in codebase
[ ] Booking.com links all contain aid= param from AWIN
```
