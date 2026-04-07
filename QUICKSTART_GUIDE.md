# 🚀 TravelApp - Quick Start Guide

## Database Setup Instructions

### Step 1: Apply Migrations

Navigate to the Infrastructure project and run:

```powershell
# In Package Manager Console
Update-Database

# Or via CLI
dotnet ef database update --project src/TravelApp.Infrastructure --startup-project src/TravelApp.Api
```

This will:
- ✅ Create all tables (POI, POI_Localization, Audio, Users, Roles, UserRoles)
- ✅ Apply constraints and indexes
- ✅ Seed 6 POIs (3 HCM + 3 Hanoi)
- ✅ Seed 12 localizations (English + Vietnamese)
- ✅ Seed 12 audio guides (English + Vietnamese)

---

### Step 2: Verify Data in SQL Server

```sql
-- Check POIs
SELECT Id, Title, Location, Latitude, Longitude FROM POI;

-- Expected output:
-- 1 | Chợ Bến Thành | Quận 1, TPHCM | 10.7725 | 106.6992
-- 2 | Phở Vĩnh Khánh | Quận 4, TPHCM | 10.7660 | 106.7090
-- 3 | Bến Bạch Đằng | Quận 1, TPHCM | 10.7558 | 106.7062
-- 4 | Chùa Một Cột | Hà Nội | 21.0294 | 105.8352
-- 5 | Phố Hàng Xanh | Hà Nội | 21.0285 | 105.8489
-- 6 | Phố Hàng Dâu | Hà Nội | 21.0273 | 105.8506

-- Check Localizations
SELECT p.Title, l.LanguageCode, l.Title as LocalizedTitle 
FROM POI p 
JOIN POI_Localization l ON p.Id = l.PoiId
ORDER BY p.Id, l.LanguageCode;

-- Check Audio guides
SELECT p.Title, a.LanguageCode, a.AudioUrl 
FROM POI p 
JOIN Audio a ON p.Id = a.PoiId
ORDER BY p.Id, a.LanguageCode;
```

---

## API Endpoints

### **Base URL**
```
http://localhost:5000/api/pois
```

### **1. Get All POIs**

```bash
# English
GET /api/pois?lang=en

# Vietnamese
GET /api/pois?lang=vi

# With pagination
GET /api/pois?lang=en&pageNumber=1&pageSize=10

# Example Response:
{
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 6,
  "items": [
    {
      "id": 1,
      "title": "Chợ Bến Thành",
      "subtitle": "Food Tour HCM - Starting Point",
      "description": "Điểm khởi đầu...",
      "latitude": 10.7725,
      "longitude": 106.6992,
      "geofenceRadiusMeters": 150,
      "category": "Food Tour",
      "imageUrl": "https://images.unsplash.com/...",
      "audioAssets": [...]
    }
  ]
}
```

### **2. Get Single POI by ID**

```bash
GET /api/pois/1?lang=vi

# Response includes full details + localizations + audio guides
```

### **3. Get Nearby POIs (Geofencing)**

```bash
# Find POIs within 5km of current location
GET /api/pois?lat=10.7725&lng=106.6992&radius=5000

# Useful for:
# - Auto-triggering audio guides when user enters geofence
# - Showing nearby points of interest on map
```

### **4. Get Audio Guide**

```bash
GET /api/pois/1/audio?lang=en

# Response:
{
  "id": 1,
  "languageCode": "en",
  "audioUrl": "https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-en.mp3",
  "transcript": "Welcome to Ben Thanh Market...",
  "isGenerated": false
}
```

---

## Frontend Integration

### **1. Update MockDataService (Optional - For Testing)**

```csharp
// You can now comment out mock data and use real API
public class PoiApiService : ApiClientBase, IPoiApiService
{
    public async Task<IReadOnlyList<PoiMobileDto>> GetPoisAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        string? languageCode,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // Calls API endpoint with geofencing
        var url = $"pois?lat={latitude}&lng={longitude}&radius={radiusMeters}&lang={languageCode}";
        var response = await GetAsync<PagedResultDto<PoiMobileDto>>(url, cancellationToken);
        return response?.Items ?? [];
    }
}
```

### **2. Load POIs in ExploreViewModel**

```csharp
public async Task LoadPoisAsync()
{
    var location = await Geolocation.Default.GetLocationAsync();
    
    var pois = await _poiApiService.GetPoisAsync(
        latitude: location.Latitude,
        longitude: location.Longitude,
        radiusMeters: 5000,
        languageCode: "vi" // Vietnamese
    );

    foreach (var poi in pois)
    {
        Markers.Add(new PoiMarker
        {
            Id = poi.Id,
            Title = poi.Title,
            Address = new Location(poi.Latitude, poi.Longitude)
        });
    }
}
```

### **3. Display Localizations**

```csharp
public class PoiDetailPage : ContentPage
{
    public void LoadPoi(int poiId, string language = "en")
    {
        // API automatically returns localized content
        var poi = await _poiApiService.GetByIdAsync(poiId, language);
        
        TitleLabel.Text = poi.Title;              // Localized title
        SubtitleLabel.Text = poi.Subtitle;        // Localized subtitle
        DescriptionLabel.Text = poi.Description;  // Localized description
    }
}
```

### **4. Audio Guide Integration**

```csharp
public class NowPlayingViewModel : INotifyPropertyChanged
{
    public async Task PlayAudioGuideAsync(int poiId, string language)
    {
        var audio = await _poiApiService.GetAudioAsync(poiId, language);
        
        if (!string.IsNullOrEmpty(audio.AudioUrl))
        {
            await _audioService.PlayAsync(audio.AudioUrl);
            ShowTranscript(audio.Transcript);
        }
    }
}
```

### **5. Geofencing - Auto-Trigger Audio**

```csharp
public class PoiGeofenceService : IPoiGeofenceService
{
    public async Task CheckGeofenceAsync(double latitude, double longitude)
    {
        // Get nearby POIs
        var nearbyPois = await _poiApiService.GetNearbyPoisAsync(
            latitude: latitude,
            longitude: longitude,
            radiusMeters: 150 // Typical geofence radius
        );

        foreach (var poi in nearbyPois)
        {
            // Automatically trigger audio when user enters geofence
            await _audioService.PlayAsync(poi.AudioAssets.First().AudioUrl);
        }
    }
}
```

---

## Testing with Postman

### **1. Import Collection**

Create a new Postman collection with these requests:

```json
{
  "name": "TravelApp POI API",
  "item": [
    {
      "name": "Get All POIs (Vietnamese)",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/pois?lang=vi"
      }
    },
    {
      "name": "Get POI by ID",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/pois/1?lang=en"
      }
    },
    {
      "name": "Get Nearby POIs (HCM)",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/pois?lat=10.7725&lng=106.6992&radius=5000&lang=vi"
      }
    },
    {
      "name": "Get Audio Guide",
      "request": {
        "method": "GET",
        "url": "{{baseUrl}}/api/pois/1/audio?lang=en"
      }
    }
  ]
}
```

### **2. Set Environment Variables**

```
baseUrl = http://localhost:5000
```

---

## Data Summary

### **HCM Food Tour (3 locations)**

| # | Location | Coordinates | Image |
|---|----------|------------|-------|
| 1 | 🏪 Chợ Bến Thành | 10.7725, 106.6992 | Market |
| 2 | 🍜 Phở Vĩnh Khánh | 10.7660, 106.7090 | Pho |
| 3 | 🚢 Bến Bạch Đằng | 10.7558, 106.7062 | Wharf |

### **Hanoi Food Tour (3 locations)**

| # | Location | Coordinates | Image |
|---|----------|------------|-------|
| 4 | 🏯 Chùa Một Cột | 21.0294, 105.8352 | Temple |
| 5 | 🏘️ Phố Hàng Xanh | 21.0285, 105.8489 | Street |
| 6 | 🛍️ Phố Hàng Dâu | 21.0273, 105.8506 | Street |

---

## Features Included

✅ **Multi-Language Support** (English + Vietnamese)
✅ **Audio Guides** (12 audio files)
✅ **Geofencing** (Automatic location-based triggers)
✅ **Localizations** (Translated content)
✅ **Images** (Unsplash free images)
✅ **Transcripts** (Full audio transcription)
✅ **Pagination** (Efficient data loading)
✅ **Location Coordinates** (Accurate GPS coordinates)
✅ **Duration** (Time estimates for each location)

---

## Troubleshooting

### **Migration fails?**
```powershell
# Drop and recreate database
Update-Database -Migration 0
Update-Database
```

### **No data showing?**
```sql
-- Verify seeded data
SELECT COUNT(*) as PoiCount FROM POI;
SELECT COUNT(*) as AudioCount FROM Audio;
```

### **API returns 404?**
```bash
# Ensure connection string is correct in appsettings.json
# Database should be on: localhost (SQL Server 2019+)
```

---

## Next Steps

1. ✅ Apply migrations
2. ✅ Test API endpoints with Postman
3. ✅ Update frontend API calls
4. ✅ Test geofencing functionality
5. ✅ Deploy to production

---

Happy touring! 🎉
