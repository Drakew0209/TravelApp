# 📊 TravelApp - Complete Database & API Reference

## Table of Contents
1. [Database Schema Diagram](#database-schema-diagram)
2. [Complete Seed Data](#complete-seed-data)
3. [API Response Examples](#api-response-examples)
4. [Frontend Integration Examples](#frontend-integration-examples)

---

## Database Schema Diagram

```
┌─────────────────────────────────────────────────┐
│                   POI                            │
├─────────────────────────────────────────────────┤
│ PK  Id: INT                                      │
│     Title: NVARCHAR(256)                        │
│     Subtitle: NVARCHAR(512)                     │
│     Description: NVARCHAR(MAX)                  │
│     Category: NVARCHAR(128)                     │
│     Location: NVARCHAR(512)                     │
│     ImageUrl: NVARCHAR(1024)                    │
│     Latitude: DECIMAL(10,8)                     │
│     Longitude: DECIMAL(10,8)                    │
│     GeofenceRadiusMeters: FLOAT = 100           │
│     Duration: NVARCHAR(100)                     │
│     Provider: NVARCHAR(256)                     │
│     Credit: NVARCHAR(1024)                      │
│     PrimaryLanguage: NVARCHAR(10) = 'en'        │
│     CreatedAtUtc: DATETIME2                     │
│     UpdatedAtUtc: DATETIME2                     │
└─────────────────────────────────────────────────┘
            │                       │
            │ (1:Many)              │ (1:Many)
            ▼                       ▼
┌──────────────────────────────────┐  ┌──────────────────────────────────┐
│  POI_Localization                │  │  Audio                           │
├──────────────────────────────────┤  ├──────────────────────────────────┤
│ PK  Id: INT                       │  │ PK  Id: INT                      │
│ FK  PoiId: INT                    │  │ FK  PoiId: INT                   │
│     LanguageCode: NVARCHAR(10)   │  │     LanguageCode: NVARCHAR(10)  │
│     Title: NVARCHAR(256)         │  │     AudioUrl: NVARCHAR(1024)    │
│     Subtitle: NVARCHAR(512)      │  │     Transcript: NVARCHAR(MAX)   │
│     Description: NVARCHAR(MAX)   │  │     IsGenerated: BIT = 0        │
│                                   │  │     CreatedAtUtc: DATETIME2     │
│ UQ (PoiId, LanguageCode)          │  │                                  │
└──────────────────────────────────┘  └──────────────────────────────────┘
```

---

## Complete Seed Data

### POI Table (6 Records)

```json
{
  "pois": [
    {
      "id": 1,
      "title": "Chợ Bến Thành",
      "subtitle": "Food Tour HCM - Starting Point",
      "description": "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn với đa dạng hàng hóa và đặc biệt là các quán ăn địa phương.",
      "category": "Food Tour",
      "location": "Chợ Bến Thành, Quận 1, TPHCM",
      "imageUrl": "https://images.unsplash.com/photo-1555521760-cb7ebb6a9c62?w=800&h=600&fit=crop",
      "latitude": 10.7725,
      "longitude": 106.6992,
      "geofenceRadiusMeters": 150,
      "duration": "45 min",
      "provider": "TravelApp",
      "credit": "Photo from Unsplash",
      "primaryLanguage": "en"
    },
    {
      "id": 2,
      "title": "Phở Vĩnh Khánh",
      "subtitle": "Food Tour HCM - Pho Experience",
      "description": "Quán phở nổi tiếng với nước dùng được ninh từ 12h, phục vụ phở bò ngon nhất Quận 4. Được nhiều du khách lựa chọn trong tour ẩm thực.",
      "category": "Food Tour",
      "location": "Phố Vĩnh Khánh, Quận 4, TPHCM",
      "imageUrl": "https://images.unsplash.com/photo-1565030826693-9d4595707d90?w=800&h=600&fit=crop",
      "latitude": 10.7660,
      "longitude": 106.7090,
      "geofenceRadiusMeters": 100,
      "duration": "30 min",
      "provider": "TravelApp",
      "credit": "Photo from Unsplash",
      "primaryLanguage": "en"
    },
    {
      "id": 3,
      "title": "Bến Bạch Đằng",
      "subtitle": "Food Tour HCM - Ending Point",
      "description": "Kết thúc tour tại bến Bạch Đằng. Thưởng thức các đặc sản Sài Gòn và tận hưởng không khí bình minh trên bến sông.",
      "category": "Food Tour",
      "location": "Bến Bạch Đằng, Quận 1, TPHCM",
      "imageUrl": "https://images.unsplash.com/photo-1504674900769-8c8f2e7e4a3e?w=800&h=600&fit=crop",
      "latitude": 10.7558,
      "longitude": 106.7062,
      "geofenceRadiusMeters": 150,
      "duration": "30 min",
      "provider": "TravelApp",
      "credit": "Photo from Unsplash",
      "primaryLanguage": "en"
    },
    {
      "id": 4,
      "title": "Chùa Một Cột",
      "subtitle": "Food Tour Hanoi - Starting Point",
      "description": "Điểm khởi đầu của tour ẩm thực Hà Nội. Chùa Một Cột là một di tích lịch sử quan trọng, nằm gần khu phố cổ Hà Nội.",
      "category": "Food Tour",
      "location": "Chùa Một Cột, Quận Ba Đình, Hà Nội",
      "imageUrl": "https://images.unsplash.com/photo-1511632765486-a01980e01a18?w=800&h=600&fit=crop",
      "latitude": 21.0294,
      "longitude": 105.8352,
      "geofenceRadiusMeters": 150,
      "duration": "45 min",
      "provider": "TravelApp",
      "credit": "Photo from Unsplash",
      "primaryLanguage": "en"
    },
    {
      "id": 5,
      "title": "Phố Hàng Xanh",
      "subtitle": "Food Tour Hanoi - Local Cuisine",
      "description": "Phố Hàng Xanh là một trong những phố cổ nổi tiếng của Hà Nội với các quán ăn truyền thống. Nơi đây bán các đặc sản ẩm thực Hà Nội như bánh mỳ, chả cá, etc.",
      "category": "Food Tour",
      "location": "Phố Hàng Xanh, Quận Hoàn Kiếm, Hà Nội",
      "imageUrl": "https://images.unsplash.com/photo-1555939594-58d7cb561d1b?w=800&h=600&fit=crop",
      "latitude": 21.0285,
      "longitude": 105.8489,
      "geofenceRadiusMeters": 100,
      "duration": "45 min",
      "provider": "TravelApp",
      "credit": "Photo from Unsplash",
      "primaryLanguage": "en"
    },
    {
      "id": 6,
      "title": "Phố Hàng Dâu",
      "subtitle": "Food Tour Hanoi - Ending Point",
      "description": "Kết thúc tour tại phố Hàng Dâu. Nơi đây nổi tiếng với các cửa hàng bán lụa truyền thống và các quán ăn địa phương.",
      "category": "Food Tour",
      "location": "Phố Hàng Dâu, Quận Hoàn Kiếm, Hà Nội",
      "imageUrl": "https://images.unsplash.com/photo-1555521760-cb7ebb6a9c62?w=800&h=600&fit=crop",
      "latitude": 21.0273,
      "longitude": 105.8506,
      "geofenceRadiusMeters": 150,
      "duration": "30 min",
      "provider": "TravelApp",
      "credit": "Photo from Unsplash",
      "primaryLanguage": "en"
    }
  ]
}
```

---

## API Response Examples

### **1. Get All POIs - English**

**Request:**
```
GET /api/pois?lang=en&pageNumber=1&pageSize=10
```

**Response (200 OK):**
```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 6,
  "items": [
    {
      "id": 1,
      "title": "Chợ Bến Thành",
      "subtitle": "Food Tour HCM - Starting Point",
      "description": "Starting point of HCM food tour. Ben Thanh Market is one of Saigon's most famous traditional markets...",
      "languageCode": "en",
      "primaryLanguage": "en",
      "imageUrl": "https://images.unsplash.com/photo-1555521760-cb7ebb6a9c62?w=800&h=600&fit=crop",
      "location": "Chợ Bến Thành, Quận 1, TPHCM",
      "latitude": 10.7725,
      "longitude": 106.6992,
      "distanceMeters": 0,
      "geofenceRadiusMeters": 150,
      "category": "Food Tour",
      "audioAssets": [
        {
          "id": 1,
          "languageCode": "en",
          "audioUrl": "https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-en.mp3",
          "transcript": "Welcome to Ben Thanh Market, the heart of Saigon shopping...",
          "isGenerated": false
        }
      ]
    }
  ]
}
```

### **2. Get POI by ID - Vietnamese**

**Request:**
```
GET /api/pois/1?lang=vi
```

**Response (200 OK):**
```json
{
  "id": 1,
  "title": "Chợ Bến Thành",
  "subtitle": "Tour Ẩm Thực HCM - Điểm Khởi Đầu",
  "description": "Điểm khởi đầu của tour ẩm thực HCM. Chợ Bến Thành là một trong những chợ truyền thống nổi tiếng nhất Sài Gòn...",
  "languageCode": "vi",
  "primaryLanguage": "en",
  "imageUrl": "https://images.unsplash.com/photo-1555521760-cb7ebb6a9c62?w=800&h=600&fit=crop",
  "location": "Chợ Bến Thành, Quận 1, TPHCM",
  "latitude": 10.7725,
  "longitude": 106.6992,
  "distanceMeters": null,
  "geofenceRadiusMeters": 150,
  "category": "Food Tour",
  "audioAssets": [
    {
      "id": 7,
      "languageCode": "vi",
      "audioUrl": "https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-vi.mp3",
      "transcript": "Chào mừng đến Chợ Bến Thành, trái tim mua sắm của Sài Gòn...",
      "isGenerated": false
    }
  ]
}
```

### **3. Get Nearby POIs (Geofencing)**

**Request:**
```
GET /api/pois?lat=10.7725&lng=106.6992&radius=5000&lang=en
```

**Response (200 OK):**
```json
{
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 3,
  "items": [
    {
      "id": 1,
      "title": "Chợ Bến Thành",
      "distanceMeters": 0,
      ...
    },
    {
      "id": 2,
      "title": "Phở Vĩnh Khánh",
      "distanceMeters": 874.5,
      ...
    },
    {
      "id": 3,
      "title": "Bến Bạch Đằng",
      "distanceMeters": 1485.2,
      ...
    }
  ]
}
```

---

## Frontend Integration Examples

### **1. Load POIs in ExploreViewModel**

```csharp
public class ExploreViewModel : INotifyPropertyChanged
{
    private readonly IPoiApiService _poiApiService;
    private readonly ILocationProvider _locationProvider;
    
    public ObservableCollection<PoiModel> Pois { get; } = new();

    public ExploreViewModel(
        IPoiApiService poiApiService,
        ILocationProvider locationProvider)
    {
        _poiApiService = poiApiService;
        _locationProvider = locationProvider;
    }

    public async Task LoadPoisAsync(string language = "en")
    {
        try
        {
            // Get current location
            var location = await _locationProvider.GetCurrentLocationAsync();
            
            // Fetch nearby POIs within 5km
            var pois = await _poiApiService.GetPoisAsync(
                latitude: location.Latitude,
                longitude: location.Longitude,
                radiusMeters: 5000,
                languageCode: language,
                pageNumber: 1,
                pageSize: 50
            );

            Pois.Clear();
            foreach (var poi in pois)
            {
                Pois.Add(new PoiModel
                {
                    Id = poi.Id,
                    Title = poi.Title,
                    Subtitle = poi.Subtitle,
                    ImageUrl = poi.ImageUrl,
                    Location = poi.Location,
                    Duration = "30 min", // TODO: Add to DTO
                    Distance = poi.DistanceMeters.HasValue 
                        ? $"{(poi.DistanceMeters / 1000):F1} km"
                        : "Unknown",
                    Description = poi.Description,
                    Provider = "TravelApp"
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading POIs: {ex.Message}");
        }
    }
}
```

### **2. Display POI Details**

```csharp
public class TourDetailViewModel : INotifyPropertyChanged
{
    private readonly IPoiApiService _poiApiService;
    private PoiMobileDto _poiDto;

    public async Task LoadPoiAsync(int poiId, string language = "en")
    {
        _poiDto = await _poiApiService.GetByIdAsync(poiId, language);
        
        if (_poiDto != null)
        {
            Title = _poiDto.Title;
            Subtitle = _poiDto.Subtitle;
            Description = _poiDto.Description;
            ImageUrl = _poiDto.ImageUrl;
            Location = _poiDto.Location;
            
            // Update UI
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Subtitle));
            OnPropertyChanged(nameof(Description));
        }
    }

    public async Task PlayAudioGuideAsync()
    {
        if (_poiDto?.AudioAssets.Count > 0)
        {
            var audioAsset = _poiDto.AudioAssets[0];
            await _audioService.PlayAsync(audioAsset.AudioUrl);
        }
    }
}
```

### **3. Geofencing - Auto-Trigger Audio**

```csharp
public class PoiGeofenceService : IPoiGeofenceService
{
    private readonly IPoiApiService _poiApiService;
    private readonly IAudioService _audioService;
    private readonly ILocationProvider _locationProvider;
    
    private HashSet<int> _triggeredPois = new();

    public async Task MonitorGeofenceAsync()
    {
        while (true)
        {
            try
            {
                var location = await _locationProvider.GetCurrentLocationAsync();
                
                // Get nearby POIs (within 500m for geofencing)
                var nearbyPois = await _poiApiService.GetPoisAsync(
                    latitude: location.Latitude,
                    longitude: location.Longitude,
                    radiusMeters: 500,
                    languageCode: "en"
                );

                foreach (var poi in nearbyPois)
                {
                    // Check if user just entered geofence
                    if (!_triggeredPois.Contains(poi.Id) 
                        && poi.DistanceMeters < poi.GeofenceRadiusMeters)
                    {
                        _triggeredPois.Add(poi.Id);
                        
                        // Auto-play audio guide
                        if (poi.AudioAssets.Count > 0)
                        {
                            var audio = poi.AudioAssets[0];
                            await _audioService.PlayAsync(audio.AudioUrl);
                        }
                        
                        // Notify user
                        OnPoiEntered?.Invoke(poi);
                    }
                }
                
                // Check if user left geofence
                var leftPois = _triggeredPois
                    .Except(nearbyPois.Select(p => p.Id))
                    .ToList();
                
                foreach (var poiId in leftPois)
                {
                    _triggeredPois.Remove(poiId);
                    OnPoiExited?.Invoke(poiId);
                }
                
                // Check every 10 seconds
                await Task.Delay(10000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Geofence error: {ex.Message}");
            }
        }
    }

    public event Action<PoiMobileDto>? OnPoiEntered;
    public event Action<int>? OnPoiExited;
}
```

---

## Summary

✅ **6 POIs** (3 HCM Food Tour + 3 Hanoi Food Tour)
✅ **12 Localizations** (English + Vietnamese)
✅ **12 Audio Guides** (English + Vietnamese)
✅ **Complete API Integration**
✅ **Geofencing Ready**
✅ **Multi-Language Support**

---

Database is now ready for production deployment! 🚀
