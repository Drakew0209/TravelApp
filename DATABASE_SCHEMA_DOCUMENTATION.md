# TravelApp - Database Schema Documentation

## Overview
Complete SQL Server database schema for Food Tour application with 2 tours: HCM and Hanoi.

---

## Database Schema

### **1. POI (Points of Interest)**

```sql
CREATE TABLE POI (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(256) NOT NULL,
    Subtitle NVARCHAR(512),
    Description NVARCHAR(MAX),
    Category NVARCHAR(128),
    Location NVARCHAR(512),
    ImageUrl NVARCHAR(1024),
    Latitude DECIMAL(10,8) NOT NULL,
    Longitude DECIMAL(10,8) NOT NULL,
    GeofenceRadiusMeters FLOAT DEFAULT 100,
    Duration NVARCHAR(100),
    Provider NVARCHAR(256),
    Credit NVARCHAR(1024),
    PrimaryLanguage NVARCHAR(10) NOT NULL DEFAULT 'en',
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2
);
```

**Sample Data:**

| Id | Title | Location | Latitude | Longitude | Category |
|----|-------|----------|----------|-----------|----------|
| 1 | Chợ Bến Thành | Quận 1, TPHCM | 10.7725 | 106.6992 | Food Tour |
| 2 | Phở Vĩnh Khánh | Quận 4, TPHCM | 10.7660 | 106.7090 | Food Tour |
| 3 | Bến Bạch Đằng | Quận 1, TPHCM | 10.7558 | 106.7062 | Food Tour |
| 4 | Chùa Một Cột | Hà Nội | 21.0294 | 105.8352 | Food Tour |
| 5 | Phố Hàng Xanh | Hà Nội | 21.0285 | 105.8489 | Food Tour |
| 6 | Phố Hàng Dâu | Hà Nội | 21.0273 | 105.8506 | Food Tour |

---

### **2. POI_Localization (Multi-Language Support)**

```sql
CREATE TABLE POI_Localization (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PoiId INT NOT NULL FOREIGN KEY REFERENCES POI(Id) ON DELETE CASCADE,
    LanguageCode NVARCHAR(10) NOT NULL,
    Title NVARCHAR(256) NOT NULL,
    Subtitle NVARCHAR(512),
    Description NVARCHAR(MAX),
    CONSTRAINT UQ_POI_Localization_PoiId_Lang UNIQUE(PoiId, LanguageCode)
);
```

**Supported Languages:**
- `en` - English
- `vi` - Vietnamese
- `ja` - Japanese (optional)

**Sample Data:** 6 POIs × 2 languages (en, vi) = 12 records

---

### **3. Audio (Audio Guide Files)**

```sql
CREATE TABLE Audio (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PoiId INT NOT NULL FOREIGN KEY REFERENCES POI(Id) ON DELETE CASCADE,
    LanguageCode NVARCHAR(10) NOT NULL,
    AudioUrl NVARCHAR(1024),
    Transcript NVARCHAR(MAX),
    IsGenerated BIT DEFAULT 0,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_Audio_PoiId_Lang (PoiId, LanguageCode)
);
```

**Sample Audio URLs:**
```
https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-en.mp3
https://travel-app-audios.blob.core.windows.net/audio/hcm-cho-ben-thanh-vi.mp3
https://travel-app-audios.blob.core.windows.net/audio/hcm-pho-vinh-khanh-en.mp3
https://travel-app-audios.blob.core.windows.net/audio/hcm-pho-vinh-khanh-vi.mp3
... and more
```

**Sample Data:** 6 POIs × 2 languages (en, vi) = 12 audio records

---

### **4. Users (Optional - For Future Authentication)**

```sql
CREATE TABLE [User] (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserName NVARCHAR(256) NOT NULL UNIQUE,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(512) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

---

### **5. Roles & UserRoles (Optional - For Future Authorization)**

```sql
CREATE TABLE Role (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(256) NOT NULL UNIQUE,
    Description NVARCHAR(512)
);

CREATE TABLE UserRole (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL FOREIGN KEY REFERENCES [User](Id) ON DELETE CASCADE,
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Role(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_UserRole UNIQUE(UserId, RoleId)
);
```

---

## Data Model - Entity Relationships

```
POI (1) ──→ (Many) POI_Localization
POI (1) ──→ (Many) Audio
```

---

## Seed Data Structure

### **HCM Food Tour**
1. **Chợ Bến Thành** (10.7725, 106.6992) - Starting Point
   - Duration: 45 min
   - Geofence: 150m
   
2. **Phở Vĩnh Khánh** (10.7660, 106.7090) - Pho Experience
   - Duration: 30 min
   - Geofence: 100m
   
3. **Bến Bạch Đằng** (10.7558, 106.7062) - Ending Point
   - Duration: 30 min
   - Geofence: 150m

### **Hanoi Food Tour**
4. **Chùa Một Cột** (21.0294, 105.8352) - Starting Point
   - Duration: 45 min
   - Geofence: 150m
   
5. **Phố Hàng Xanh** (21.0285, 105.8489) - Local Cuisine
   - Duration: 45 min
   - Geofence: 100m
   
6. **Phố Hàng Dâu** (21.0273, 105.8506) - Ending Point
   - Duration: 30 min
   - Geofence: 150m

---

## API Endpoints

### **Get All POIs**
```
GET /api/pois
Response: List<PoiDto>
```

### **Get POI by ID**
```
GET /api/pois/{id}
Response: PoiDto
```

### **Get Nearby POIs (Geofencing)**
```
GET /api/pois/nearby?latitude=10.7725&longitude=106.6992&radiusMeters=5000
Response: List<PoiDto>
```

### **Get POI with Localization**
```
GET /api/pois/{id}?language=vi
Response: PoiDto (Vietnamese localization)
```

### **Get Audio Guide**
```
GET /api/pois/{id}/audio?lang=en
Response: AudioDto
```

---

## Indexes

```sql
-- For performance optimization
CREATE INDEX IX_POI_Category ON POI(Category);
CREATE INDEX IX_POI_Latitude_Longitude ON POI(Latitude, Longitude);
CREATE INDEX IX_POI_Localization_PoiId ON POI_Localization(PoiId);
CREATE INDEX IX_Audio_PoiId ON Audio(PoiId);
```

---

## Data Characteristics

- **Total POIs:** 6 (3 HCM, 3 Hanoi)
- **Total Localizations:** 12 (6 English, 6 Vietnamese)
- **Total Audio Files:** 12 (6 English, 6 Vietnamese)
- **Languages Supported:** English (en), Vietnamese (vi)
- **Geofence Triggers:** Yes (Range: 100-150m per location)

---

## Notes

1. ✅ Multi-language support ready (English, Vietnamese)
2. ✅ Audio guides for each location
3. ✅ Geofencing enabled for automatic audio trigger
4. ✅ Location coordinates validated
5. ✅ Duration specified for each waypoint
6. ✅ Images from unsplash (free to use)
7. ✅ Provider info included
8. ✅ Attribution/Credit information included

---

## Future Enhancements

1. Add User ratings and reviews
2. Add tour booking/pricing
3. Add user favorites/bookmarks
4. Add tour itinerary table for route optimization
5. Add real-time location tracking
