# PLANORA — BOOKING ARCHITECTURE (v3.0)
> Feed alongside PROJECT_BIBLE.md and AGENT_INSTRUCTIONS.md
> **v3.0:** No affiliate IDs. No partner approvals. Pure deeplinks only. Skyscanner + Kayak removed. Choiceable vs Non-Choiceable model. Deeplinks built at generation time, stored on POI document, opened at tap time.

---

## 1. FUNDAMENTAL MODEL

### No Affiliates. No Approval. Just Links.

Every deeplink in Planora is a plain URL to an external platform, pre-filled with the user's trip parameters. No `?aid=`, no `?mcid=`, no `?cid=` needed. They work without any partner registration. Affiliate parameters can be appended later once partnerships are established — the URL structure does not change.

### Where deeplinks live

Deeplinks are constructed by `DeeplinkBuilderService.cs` at trip generation time (inside the Personalisation Agent and Spot Enrichment Agent). They are stored directly on the POI document in Cosmos DB:

```json
"suggested_options": [  ← shown inline on trip card (2–3 items)
  { "platform": "Booking.com", "url": "...", "logo": "bookingcom" },
  { "platform": "Agoda",       "url": "...", "logo": "agoda" }
],
"browse_links": [       ← shown in "Browse more" bottom sheet (all remaining platforms)
  { "platform": "OYO",      "url": "...", "logo": "oyo" },
  { "platform": "MakeMyTrip","url": "...", "logo": "mmt" },
  { "platform": "Goibibo",   "url": "...", "logo": "goibibo" }
]
```

At booking time: `await Linking.openURL(option.url)` — that is the entire booking flow.

### Choiceable vs Non-Choiceable

See PROJECT_BIBLE.md Section 1 for the full definition. In this document:
- **Choiceable** POIs get `suggested_options` + `browse_links` populated
- **Non-Choiceable** POIs get `ticket_url` (one link, if exists) + Google Maps directions link

---

## 2. DEEPLINK BUILDER SERVICE

### DeeplinkBuilderService.cs — called from Personalisation Agent and Spot Enrichment Agent

The service receives the POI's details + trip context (dates, pax, destination lat/lng, day number) and returns populated `suggested_options` and `browse_links` arrays.

```csharp
public class DeeplinkBuilderService
{
    public List<DeeplinkOption> BuildSuggestedOptions(
        string category, DeeplinkContext ctx) => category switch
    {
        "hotel"      => BuildHotelOptions(ctx),
        "flight"     => BuildFlightOptions(ctx),
        "cab"        => BuildCabOptions(ctx),
        "train"      => BuildTrainOptions(ctx),
        "car_rental" => BuildCarRentalOptions(ctx),
        _            => new List<DeeplinkOption>()
    };

    public List<DeeplinkOption> BuildBrowseLinks(
        string category, DeeplinkContext ctx) => category switch
    {
        "hotel"  => BuildHotelBrowseLinks(ctx),
        "flight" => BuildFlightBrowseLinks(ctx),
        "cab"    => BuildCabBrowseLinks(ctx),
        "train"  => BuildTrainBrowseLinks(ctx),
        _        => new List<DeeplinkOption>()
    };
}

public class DeeplinkContext
{
    public string  Destination  { get; set; }  // city name
    public string  CityCode     { get; set; }  // e.g. "JAI" for Jaipur
    public double  CenterLat    { get; set; }  // POI cluster centroid for this day
    public double  CenterLng    { get; set; }
    public DateTime CheckIn     { get; set; }
    public DateTime CheckOut    { get; set; }
    public int     Adults       { get; set; }
    public string  OriginIata   { get; set; }  // for flights, if user provided
    public string  DestIata     { get; set; }
    public double  PickupLat    { get; set; }  // for cabs
    public double  PickupLng    { get; set; }
    public string  PickupName   { get; set; }
    public double  DropLat      { get; set; }
    public double  DropLng      { get; set; }
    public string  DropName     { get; set; }
}
```

---

## 3. ALL DEEPLINK FORMATS

### 3A. HOTELS

**Center calculation (per day — used for all hotel deeplinks):**
```csharp
var centerLat = day.Pois.Where(p => p.PoisClass == "non_choiceable").Average(p => p.Lat);
var centerLng = day.Pois.Where(p => p.PoisClass == "non_choiceable").Average(p => p.Lng);
```

**suggested_options (shown inline — primary 2–3):**

Booking.com (map-based, most precise):
```
https://www.booking.com/searchresults.html
  ?latitude={CENTER_LAT}
  &longitude={CENTER_LNG}
  &radius=2
  &checkin_year={YYYY}&checkin_month={M}&checkin_monthday={D}
  &checkout_year={YYYY}&checkout_month={M}&checkout_monthday={D}
  &group_adults={PAX}&no_rooms=1
```

Booking.com (city fallback if coords unavailable):
```
https://www.booking.com/searchresults.html
  ?ss={CITY_URL_ENCODED}
  &checkin_year={YYYY}&checkin_month={M}&checkin_monthday={D}
  &checkout_year={YYYY}&checkout_month={M}&checkout_monthday={D}
  &group_adults={PAX}&no_rooms=1
```

Agoda:
```
https://www.agoda.com/search
  ?latitude={CENTER_LAT}
  &longitude={CENTER_LNG}
  &checkIn={YYYY-MM-DD}
  &checkOut={YYYY-MM-DD}
  &rooms=1&adults={PAX}
```

**browse_links (shown in Browse bottom sheet — remaining platforms):**

MakeMyTrip Hotels:
```
https://www.makemytrip.com/hotels/hotel-listing/
  ?city={CITY_CODE}
  &checkin={DDMMYYYY}
  &checkout={DDMMYYYY}
  &roomStayQualifier=2e0e&country=IN
```

OYO:
```
https://www.oyorooms.com/search/
  ?location={CITY_URL_ENCODED}
  &check_in={YYYY-MM-DD}
  &check_out={YYYY-MM-DD}
  &guest_count={PAX}
```

Goibibo:
```
https://www.goibibo.com/hotels/hotels-in-{CITY_SLUG}/
  ?ci={YYYYMMDD}&co={YYYYMMDD}&r=1&a={PAX}
```

Cleartrip Hotels:
```
https://www.cleartrip.com/hotels/results/
  ?city={CITY_URL_ENCODED}
  &checkin={DD/MM/YYYY}
  &checkout={DD/MM/YYYY}
  &adults={PAX}&children=0&rooms=1
```

---

### 3B. FLIGHTS

> ⚠ Skyscanner DENIED. Kayak DENIED. Do not use either.

**suggested_options (primary 2–3):**

MakeMyTrip (domestic India):
```
https://www.makemytrip.com/flights/domestic/
  {ORIGIN_CITY_SLUG}-to-{DEST_CITY_SLUG}/{DDMMYYYY}/
  ?tripType=O&paxType=A-{PAX}_C-0_I-0&cabinClass=E&lang=eng
```

MakeMyTrip (international):
```
https://www.makemytrip.com/flights/international-flights/
  {ORIGIN_IATA}-to-{DEST_IATA}/{DDMMYYYY}/
  ?tripType=O&paxType=A-{PAX}_C-0_I-0&cabinClass=E&lang=eng
```

Ixigo:
```
https://www.ixigo.com/flight/search/flight/
  {ORIGIN_IATA}-{DEST_IATA}-{DDMMYYYY}-null-E-{PAX}-0-0/ONEWAY
```

**browse_links:**

Cleartrip:
```
https://www.cleartrip.com/flights/results/
  ?from={ORIGIN_IATA}&to={DEST_IATA}
  &depart_date={DD/MM/YYYY}&adults={PAX}
  &childs=0&infants=0&class=Economy&intl=n
```

Yatra:
```
https://www.yatra.com/flights#pType=1
  &origin={ORIGIN_IATA}&destination={DEST_IATA}
  &departDate={DD/MM/YYYY}&adult={PAX}
```

EaseMyTrip:
```
https://www.easemytrip.com/flights/
  {ORIGIN_IATA}-{DEST_IATA}-flight-tickets/
  ?dd={DD+Mon+YYYY}&pax={PAX}-0-0&cls=E
```

> Note: If user did not provide origin city (Step 3 of wizard was skipped), flight deeplinks are not generated. Only show flight section if origin is known.

---

### 3C. CABS / TRANSFERS

**suggested_options:**

Uber:
```
https://m.uber.com/ul/?action=setPickup
  &pickup[latitude]={PICKUP_LAT}
  &pickup[longitude]={PICKUP_LNG}
  &pickup[nickname]={PICKUP_NAME_ENCODED}
  &dropoff[latitude]={DROP_LAT}
  &dropoff[longitude]={DROP_LNG}
  &dropoff[nickname]={DROP_NAME_ENCODED}

Logic: pickup = hotel lat/lng, drop = first Non-Choiceable POI of the day
```

Ola (app deeplink first, web fallback):
```
App: ola://open?lat={PICKUP_LAT}&lng={PICKUP_LNG}&drop_lat={DROP_LAT}&drop_lng={DROP_LNG}

Web fallback (use Linking.canOpenURL first):
https://book.olacabs.com/
  ?pickup_lat={PICKUP_LAT}&pickup_lng={PICKUP_LNG}
  &pickup_name={PICKUP_ENCODED}
  &drop_lat={DROP_LAT}&drop_lng={DROP_LNG}
  &drop_name={DROP_ENCODED}
  &utm_source=planora
```

**browse_links:**

Rapido:
```
https://www.rapido.bike/
(app-only — show with note "Book in the Rapido app")
```

InDrive:
```
https://indrive.com/en/cities/{CITY_SLUG}/
(app-only — show with note "Book in the InDrive app")
```

---

### 3D. TRAINS (India)

**suggested_options:**

IRCTC:
```
https://www.irctc.co.in/nget/train-search
  ?from={ORIGIN_STATION_CODE}
  &to={DEST_STATION_CODE}
  &date={YYYY-MM-DD}
  &quota=GN&class=3A
  (class options: SL=Sleeper, 3A=AC3Tier, 2A=AC2Tier, 1A=ACFirstClass)
```

ConfirmTkt:
```
https://confirmtkt.com/trains/
  {ORIGIN_STATION_CODE}-to-{DEST_STATION_CODE}
  ?date={YYYY-MM-DD}
```

**browse_links:**

RailYatri:
```
https://www.railyatri.in/trains-between-stations
  ?from={ORIGIN_CODE}&to={DEST_CODE}&date={YYYY-MM-DD}
```

Ixigo Trains:
```
https://www.ixigo.com/train/search/
  {ORIGIN_CODE}/{DEST_CODE}/{DDMMYYYY}
```

> Note: Station codes (e.g. "NDLS" for New Delhi, "BCT" for Mumbai) must be resolved by GPT-4o during enrichment from the destination city name.

---

### 3E. ACTIVITIES (when a Non-Choiceable has online ticketing)

These are NOT Choiceable items. These are single ticket URLs stored in `ticket_url` on the POI.

GPT-4o enrichment determines the ticket URL during generation. Fallback chain:
```
1. GPT-4o returns direct ticket URL (e.g. asi.payumoney.com for Red Fort)
2. If null → use Google Places `website` field
3. If null → Google search fallback:
   https://www.google.com/search?q={POI_NAME}+{CITY}+tickets+online+book
4. If poi.is_free = true → show "Free Entry" badge, no URL needed
5. If no online booking → show "Buy at gate" badge
```

**Known ticket portals (GPT-4o reference — not exhaustive):**

India — ASI monuments:
```
https://asi.payumoney.com/
```

India — State tourism (used as fallback for state-managed sites):
```
Rajasthan:   https://www.rajasthantourism.gov.in/
Kerala:      https://www.keralatourism.org/
Karnataka:   https://www.karnatakatourism.org/
Tamil Nadu:  https://www.tamilnadutourism.tn.gov.in/
Maharashtra: https://www.maharashtratourism.gov.in/
```

Europe:
```
Italy (Colosseum, Forum): https://www.coopculture.it/en/
Vatican Museums:          https://www.museivaticani.va/
Louvre:                   https://www.louvre.fr/en/
Eiffel Tower:             https://www.toureiffel.paris/en/
Sagrada Familia:          https://sagradafamilia.org/en/
Acropolis:                https://etickets.tap.gr/
```

USA:
```
National Parks:   https://www.recreation.gov/
MoMA:             https://www.moma.org/tickets/
```

Asia:
```
Angkor Wat:       https://www.angkorwatticket.com/
Forbidden City:   https://gugong.emuseum.com/
```

---

### 3F. DINING (Non-Choiceable — directions only)

Specific restaurants added as Non-Choiceable spots. No comparison needed. Show:

Google Maps directions:
```
https://www.google.com/maps/search/?api=1
  &query={POI_NAME_ENCODED}+{CITY_ENCODED}
  &query_place_id={GOOGLE_PLACE_ID}
```

Zomato page (if restaurant is on Zomato — from GPT-4o enrichment):
```
https://www.zomato.com/{CITY_SLUG}/{RESTAURANT_SLUG}
(direct restaurant page, not a search — only if GPT-4o returns a confident URL)
```

Swiggy Dineout (if available):
```
https://www.swiggy.com/dineout/{CITY}/{RESTAURANT_SLUG}
```

If no specific restaurant URL available → show only Google Maps directions link.

---

### 3G. EVENTS & SHOWS (Non-Choiceable — ticket link if exists)

Show, performance, or event added as a Non-Choiceable spot.

BookMyShow (India — from GPT-4o enrichment):
```
https://in.bookmyshow.com/events/{EVENT_SLUG}/{CITY_SLUG}
(direct event page if known) OR
https://in.bookmyshow.com/explore/events-{CITY_SLUG}
(city events fallback)
```

Ticketmaster (international):
```
https://www.ticketmaster.com/search
  ?q={EVENT_NAME_ENCODED}&dateStart={YYYY-MM-DD}
```

---

## 4. GOOGLE MAPS DIRECTIONS LINK (all POIs)

Every Non-Choiceable POI gets a Google Maps directions deeplink regardless of type:
```
https://www.google.com/maps/dir/?api=1
  &destination={POI_LAT},{POI_LNG}
  &destination_place_id={GOOGLE_PLACE_ID}
```

For Choiceable POIs (hotels after selection) → directions use lat/lng of the hotel area centroid.

---

## 5. BROWSE MORE BOTTOM SHEET

When a Choiceable item's **+ Browse more options** button is tapped, a bottom sheet opens showing all available `browse_links` for that category.

**Design:**
- Bottom sheet, 60% screen height, draggable
- List of platform rows: logo (left) + platform name (Oswald 600, `#0E2038`) + "Open ↗" button (right, secondary button style)
- Title: "More options near [Day X spots]" in Oswald 700
- Each tap → `Linking.openURL(link.url)` → opens device browser

**No prices shown** — prices are on the external platforms. Planora does not display prices.

---

## 6. FRONTEND TYPES

```typescript
interface DeeplinkOption {
  platform: string;
  url:      string;
  logo:     string;  // logo asset name in assets/logos/
}

interface ChoiceablePoi {
  poiId:            string;
  name:             string;
  poiClass:         'choiceable';
  category:         'hotel' | 'flight' | 'cab' | 'train' | 'car_rental';
  suggestedOptions: DeeplinkOption[];   // 2–3, shown inline
  browseLinks:      DeeplinkOption[];   // rest, shown in bottom sheet
}

interface NonChoiceablePoi {
  poiId:             string;
  name:              string;
  poiClass:          'non_choiceable';
  visitDuration:     number;
  bestTimeOfDay:     string;
  estimatedCost:     string;
  openingHours:      string;
  isFree:            boolean;
  ticketUrl:         string | null;
  googleMapsUrl:     string;
  aiSummary:         string;
  photoUrl:          string;
}

// Tap handler — same for all
const handleDeeplinkTap = async (url: string, platform: string) => {
  await logDeeplinkTap(tripId, platform); // analytics
  const canOpen = await Linking.canOpenURL(url);
  if (canOpen) {
    await Linking.openURL(url);
  } else {
    await WebBrowser.openBrowserAsync(url);
  }
};
```

---

## 7. BACKEND — DeeplinkBuilderService.cs (full methods)

```csharp
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
          $"?ss={Uri.EscapeDataString(ctx.Destination)}" +
          $"&checkin_year={ctx.CheckIn.Year}&checkin_month={ctx.CheckIn.Month}" +
          $"&checkin_monthday={ctx.CheckIn.Day}" +
          $"&checkout_year={ctx.CheckOut.Year}&checkout_month={ctx.CheckOut.Month}" +
          $"&checkout_monthday={ctx.CheckOut.Day}" +
          $"&group_adults={ctx.Adults}&no_rooms=1";
    options.Add(new("Booking.com", bcUrl, "bookingcom"));

    // Agoda
    if (ctx.CenterLat != 0)
        options.Add(new("Agoda",
            $"https://www.agoda.com/search?latitude={ctx.CenterLat}&longitude={ctx.CenterLng}" +
            $"&checkIn={ctx.CheckIn:yyyy-MM-dd}&checkOut={ctx.CheckOut:yyyy-MM-dd}" +
            $"&rooms=1&adults={ctx.Adults}",
            "agoda"));

    return options;
}

private List<DeeplinkOption> BuildHotelBrowseLinks(DeeplinkContext ctx) =>
    new()
    {
        new("MakeMyTrip",
            $"https://www.makemytrip.com/hotels/hotel-listing/" +
            $"?city={ctx.CityCode}&checkin={ctx.CheckIn:ddMMyyyy}" +
            $"&checkout={ctx.CheckOut:ddMMyyyy}&roomStayQualifier=2e0e&country=IN",
            "mmt"),
        new("OYO",
            $"https://www.oyorooms.com/search/?location={Uri.EscapeDataString(ctx.Destination)}" +
            $"&check_in={ctx.CheckIn:yyyy-MM-dd}&check_out={ctx.CheckOut:yyyy-MM-dd}" +
            $"&guest_count={ctx.Adults}",
            "oyo"),
        new("Goibibo",
            $"https://www.goibibo.com/hotels/hotels-in-{ctx.Destination.ToLower().Replace(" ", "-")}/" +
            $"?ci={ctx.CheckIn:yyyyMMdd}&co={ctx.CheckOut:yyyyMMdd}&r=1&a={ctx.Adults}",
            "goibibo"),
        new("Cleartrip",
            $"https://www.cleartrip.com/hotels/results/?city={Uri.EscapeDataString(ctx.Destination)}" +
            $"&checkin={ctx.CheckIn:dd/MM/yyyy}&checkout={ctx.CheckOut:dd/MM/yyyy}" +
            $"&adults={ctx.Adults}&children=0&rooms=1",
            "cleartrip"),
    };

// FLIGHTS
private List<DeeplinkOption> BuildFlightOptions(DeeplinkContext ctx)
{
    if (string.IsNullOrEmpty(ctx.OriginIata) || string.IsNullOrEmpty(ctx.DestIata))
        return new(); // Skip if no origin provided

    return new()
    {
        new("MakeMyTrip",
            $"https://www.makemytrip.com/flights/international-flights/" +
            $"{ctx.OriginIata}-to-{ctx.DestIata}/{ctx.CheckIn:ddMMyyyy}/" +
            $"?tripType=O&paxType=A-{ctx.Adults}_C-0_I-0&cabinClass=E&lang=eng",
            "mmt"),
        new("Ixigo",
            $"https://www.ixigo.com/flight/search/flight/" +
            $"{ctx.OriginIata}-{ctx.DestIata}-{ctx.CheckIn:ddMMyyyy}-null-E-{ctx.Adults}-0-0/ONEWAY",
            "ixigo"),
    };
}

private List<DeeplinkOption> BuildFlightBrowseLinks(DeeplinkContext ctx)
{
    if (string.IsNullOrEmpty(ctx.OriginIata)) return new();
    return new()
    {
        new("Cleartrip",
            $"https://www.cleartrip.com/flights/results/?from={ctx.OriginIata}&to={ctx.DestIata}" +
            $"&depart_date={ctx.CheckIn:dd/MM/yyyy}&adults={ctx.Adults}&childs=0&infants=0&class=Economy&intl=n",
            "cleartrip"),
        new("Yatra",
            $"https://www.yatra.com/flights#pType=1&origin={ctx.OriginIata}&destination={ctx.DestIata}" +
            $"&departDate={ctx.CheckIn:dd/MM/yyyy}&adult={ctx.Adults}",
            "yatra"),
        new("EaseMyTrip",
            $"https://www.easemytrip.com/flights/{ctx.OriginIata}-{ctx.DestIata}-flight-tickets/" +
            $"?dd={ctx.CheckIn:dd+MMM+yyyy}&pax={ctx.Adults}-0-0&cls=E",
            "easemytrip"),
    };
}

// CABS
private List<DeeplinkOption> BuildCabOptions(DeeplinkContext ctx) =>
    new()
    {
        new("Uber",
            $"https://m.uber.com/ul/?action=setPickup" +
            $"&pickup[latitude]={ctx.PickupLat}&pickup[longitude]={ctx.PickupLng}" +
            $"&pickup[nickname]={Uri.EscapeDataString(ctx.PickupName)}" +
            $"&dropoff[latitude]={ctx.DropLat}&dropoff[longitude]={ctx.DropLng}" +
            $"&dropoff[nickname]={Uri.EscapeDataString(ctx.DropName)}",
            "uber"),
        new("Ola",
            $"https://book.olacabs.com/" +
            $"?pickup_lat={ctx.PickupLat}&pickup_lng={ctx.PickupLng}" +
            $"&pickup_name={Uri.EscapeDataString(ctx.PickupName)}" +
            $"&drop_lat={ctx.DropLat}&drop_lng={ctx.DropLng}" +
            $"&drop_name={Uri.EscapeDataString(ctx.DropName)}&utm_source=planora",
            "ola"),
    };

// TRAINS
private List<DeeplinkOption> BuildTrainOptions(DeeplinkContext ctx) =>
    new()
    {
        new("IRCTC",
            $"https://www.irctc.co.in/nget/train-search?from={ctx.OriginIata}&to={ctx.DestIata}" +
            $"&date={ctx.CheckIn:yyyy-MM-dd}&quota=GN&class=3A",
            "irctc"),
        new("ConfirmTkt",
            $"https://confirmtkt.com/trains/{ctx.OriginIata}-to-{ctx.DestIata}?date={ctx.CheckIn:yyyy-MM-dd}",
            "confirmtkt"),
    };

private List<DeeplinkOption> BuildTrainBrowseLinks(DeeplinkContext ctx) =>
    new()
    {
        new("RailYatri",
            $"https://www.railyatri.in/trains-between-stations?from={ctx.OriginIata}&to={ctx.DestIata}&date={ctx.CheckIn:yyyy-MM-dd}",
            "railyatri"),
        new("Ixigo Trains",
            $"https://www.ixigo.com/train/search/{ctx.OriginIata}/{ctx.DestIata}/{ctx.CheckIn:ddMMyyyy}",
            "ixigo"),
    };
```

---

## 8. APPSETTINGS — NO AFFILIATE KEYS NEEDED

```json
{
  "DeeplinkBuilder": {
    "DefaultRadiusKm": 2,
    "MaxSuggestedOptions": 3,
    "MaxBrowseLinks": 5
  }
}
```

No `Affiliates:*` keys. No partner IDs. Nothing to register for. All links work immediately.

---

## 9. TEST CHECKLIST — BUILD STEP 7

```
[ ] Non-Choiceable POI card shows: photo, summary, cost, hours, ticket link (if exists), directions link
[ ] No "compare options" UI on Non-Choiceable POIs
[ ] Choiceable hotel POI shows 2–3 suggested_options inline
[ ] Hotel deeplinks open Booking.com/Agoda with correct lat/lng + dates + pax
[ ] Browse more button opens bottom sheet with additional platform links
[ ] No affiliate IDs (aid=, mcid=, cid=) in any deeplink URL
[ ] Flight section only shown if user provided origin city in wizard Step 3
[ ] Flight deeplinks open MakeMyTrip/Ixigo with correct IATA codes + date
[ ] Cab deeplinks: Uber + Ola with hotel as pickup, first POI of day as drop
[ ] Train deeplinks use station codes (not city names)
[ ] All deeplinks open in device browser (Linking.openURL), NOT WebView
[ ] No Skyscanner or Kayak links anywhere in the codebase
[ ] "Buy at gate" badge shown for POIs with no ticket URL and is_free = false
[ ] "Free Entry" badge shown for is_free = true
[ ] Google Maps directions link present on every Non-Choiceable POI
```
