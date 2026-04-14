# Đặc Tả Use Case — TravelApp

**Phiên bản:** 1.0  
**Ngày:** 2026-04-14  
**Hệ thống:** TravelApp (Mobile + API + Admin Web)

---

## 1. Tổng Quan Hệ Thống

TravelApp là ứng dụng du lịch thông minh gồm ba thành phần chính:

| Thành phần | Mô tả |
|---|---|
| **TravelApp.Mobile** | Ứng dụng di động (.NET MAUI) dành cho khách du lịch |
| **TravelApp.Api** | Backend REST API (.NET 9) phục vụ dữ liệu cho mobile và admin |
| **TravelApp.Admin.Web** | Web admin (ASP.NET Core MVC) quản lý nội dung |

### Các tác nhân (Actors)

| Actor | Mô tả |
|---|---|
| **Khách du lịch (Guest)** | Người dùng chưa đăng nhập trên ứng dụng mobile |
| **Người dùng đã đăng nhập (User)** | Người dùng đã xác thực trên ứng dụng mobile |
| **Quản trị viên (Admin)** | Người quản lý nội dung qua web admin |
| **Hệ thống (System)** | Các tác vụ tự động nền (GPS, Geofence, Audio) |

---


## 2. Sơ Đồ Use Case

```
┌─────────────────────────────────────────────────────────┐
│                    TravelApp System                      │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │              Mobile App                           │  │
│  │  UC01 Xem POI gần vị trí     ←── Guest / User    │  │
│  │  UC02 Tìm kiếm điểm đến      ←── Guest / User    │  │
│  │  UC03 Đăng nhập               ←── Guest           │  │
│  │  UC04 Đăng xuất               ←── User            │  │
│  │  UC05 Xem và phát audio POI  ←── User             │  │
│  │  UC06 Bookmark POI            ←── User            │  │
│  │  UC07 Xem lịch sử đã ghé     ←── User            │  │
│  │  UC08 Quét mã QR              ←── Guest / User    │  │
│  │  UC09 Xem bản đồ POI          ←── Guest / User    │  │
│  │  UC10 Xem & chạy Tour         ←── Guest / User    │  │
│  │  UC11 Quản lý hồ sơ cá nhân  ←── User            │  │
│  │  UC12 Quản lý thư viện audio ←── User             │  │
│  │  UC13 Tự động kích hoạt audio ←── System (GPS)    │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │              Admin Web                            │  │
│  │  UC14 Đăng nhập admin         ←── Admin           │  │
│  │  UC15 Quản lý POI             ←── Admin           │  │
│  │  UC16 Quản lý Tour            ←── Admin           │  │
│  │  UC17 Quản lý người dùng     ←── Admin            │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Đặc Tả Chi Tiết Từng Use Case

---

### UC01 — Xem Danh Sách POI Gần Vị Trí

| Trường | Nội dung |
|---|---|
| **Mã** | UC01 |
| **Tên** | Xem danh sách điểm tham quan (POI) gần vị trí hiện tại |
| **Actor** | Khách du lịch, Người dùng đã đăng nhập |
| **Tiền điều kiện** | Ứng dụng đã khởi động; thiết bị có GPS |
| **Hậu điều kiện** | Danh sách POI trong bán kính được hiển thị lên màn hình |

**Luồng sự kiện chính:**

1. Người dùng mở ứng dụng hoặc điều hướng đến trang **POI List**.
2. Hệ thống yêu cầu quyền truy cập vị trí GPS.
3. `LocationPollingService` bắt đầu lấy tọa độ GPS hiện tại của thiết bị.
4. Hệ thống gọi API `GET /api/pois?lat={lat}&lng={lng}&radius={r}&lang={lang}`.
5. API trả về danh sách POI được sắp xếp theo khoảng cách, hỗ trợ phân trang.
6. Ứng dụng hiển thị danh sách POI với tên, mô tả, khoảng cách và hình ảnh.
7. Hệ thống cập nhật danh sách khi người dùng di chuyển đáng kể (debounce + ngưỡng khoảng cách).

**Luồng thay thế:**

- **A1 — Offline:** Nếu không có mạng, hệ thống tải dữ liệu từ bộ nhớ cục bộ SQLite (cache).
- **A2 — Không có GPS:** Hiển thị thông báo yêu cầu bật định vị; không gọi API theo vị trí.

---

### UC02 — Tìm Kiếm Điểm Đến

| Trường | Nội dung |
|---|---|
| **Mã** | UC02 |
| **Tên** | Tìm kiếm điểm đến theo tên hoặc bộ lọc |
| **Actor** | Khách du lịch, Người dùng đã đăng nhập |
| **Tiền điều kiện** | Ứng dụng đã khởi động |
| **Hậu điều kiện** | Kết quả tìm kiếm phù hợp hiển thị trên màn hình |

**Luồng sự kiện chính:**

1. Người dùng nhấn biểu tượng tìm kiếm hoặc mở trang **Search**.
2. Hệ thống hiển thị danh sách **Popular Destinations** mặc định.
3. Người dùng nhập từ khóa vào ô tìm kiếm.
4. Hệ thống gọi API `GET /api/pois` với tham số tên tìm kiếm.
5. Kết quả được hiển thị theo thời gian thực.
6. Người dùng có thể mở bộ lọc để thu hẹp theo loại tour (Tour / Museum / Quest) và xếp hạng.
7. Người dùng chọn một điểm đến để xem chi tiết.

**Luồng thay thế:**

- **A1 — Không có kết quả:** Hiển thị thông báo "Không tìm thấy kết quả".
- **A2 — Offline:** Tìm kiếm trong bộ nhớ cục bộ SQLite.

---

### UC03 — Đăng Nhập

| Trường | Nội dung |
|---|---|
| **Mã** | UC03 |
| **Tên** | Đăng nhập vào ứng dụng bằng Email và Password |
| **Actor** | Khách du lịch |
| **Tiền điều kiện** | Người dùng có tài khoản trong hệ thống |
| **Hậu điều kiện** | Người dùng được xác thực; JWT Access Token và Refresh Token được lưu |

**Luồng sự kiện chính:**

1. Người dùng mở trang **Login**.
2. Người dùng nhập **Email** và **Password**.
3. Người dùng nhấn nút **Sign In**.
4. Hệ thống gọi API `POST /api/auth/login` với thông tin đăng nhập.
5. API xác thực thông tin và trả về `AccessToken`, `RefreshToken`, `ExpiresAtUtc`.
6. `InMemoryTokenStore` lưu token vào bộ nhớ; `AuthStateService` cập nhật trạng thái đăng nhập.
7. Ứng dụng điều hướng về màn hình chính với trạng thái đã đăng nhập.

**Luồng thay thế:**

- **A1 — Sai thông tin đăng nhập:** API trả về `401 Unauthorized`; ứng dụng hiển thị thông báo lỗi.
- **A2 — Mất mạng:** Hiển thị lỗi kết nối.

**Ghi chú:**

- Khi `AccessToken` hết hạn, hệ thống tự động gọi `POST /api/auth/refresh` để làm mới token mà không cần người dùng đăng nhập lại.

---

### UC04 — Đăng Xuất

| Trường | Nội dung |
|---|---|
| **Mã** | UC04 |
| **Tên** | Đăng xuất khỏi ứng dụng |
| **Actor** | Người dùng đã đăng nhập |
| **Tiền điều kiện** | Người dùng đang ở trạng thái đã đăng nhập |
| **Hậu điều kiện** | Token bị thu hồi; trạng thái đăng xuất được áp dụng |

**Luồng sự kiện chính:**

1. Người dùng vào trang **Profile** và nhấn **Sign Out**.
2. Hệ thống gọi API `POST /api/auth/logout` với `RefreshToken` hiện tại.
3. API thu hồi `RefreshToken` trong cơ sở dữ liệu.
4. `AuthStateService` xóa trạng thái đăng nhập; token được xóa khỏi bộ nhớ.
5. Ứng dụng cập nhật UI về trạng thái khách (Guest).

---

### UC05 — Xem Chi Tiết POI và Phát Audio

| Trường | Nội dung |
|---|---|
| **Mã** | UC05 |
| **Tên** | Xem chi tiết điểm tham quan và phát audio thuyết minh |
| **Actor** | Người dùng đã đăng nhập |
| **Tiền điều kiện** | Người dùng đã chọn một POI từ danh sách, bản đồ hoặc tour |
| **Hậu điều kiện** | Audio thuyết minh được phát hoặc tải về thiết bị |

**Luồng sự kiện chính:**

1. Người dùng chọn một POI để xem trang **Tour Detail**.
2. Hệ thống hiển thị tên, mô tả, hình ảnh, nhà cung cấp và nội dung thuyết minh.
3. Người dùng chọn ngôn ngữ thuyết minh (tiếng Việt, tiếng Anh, ...).
4. Người dùng nhấn **Download Tour** để tải audio về máy.
5. `AudioLibraryService` tải file audio từ API hoặc TTS về bộ nhớ cục bộ.
6. Người dùng phát audio qua `AudioPlayerService`; màn hình **Now Playing** hiển thị.
7. Người dùng có thể dừng hoặc quay lại từ màn hình Now Playing.

**Luồng thay thế:**

- **A1 — Audio đã được cache:** Phát trực tiếp từ bộ nhớ cục bộ (offline).
- **A2 — Admin đã nhập SpeechText:** Hệ thống ưu tiên dùng SpeechText đã nhập thay vì TTS tự động.

---

### UC06 — Bookmark POI

| Trường | Nội dung |
|---|---|
| **Mã** | UC06 |
| **Tên** | Đánh dấu/Bỏ đánh dấu yêu thích một POI |
| **Actor** | Người dùng đã đăng nhập |
| **Tiền điều kiện** | Người dùng đang xem chi tiết POI |
| **Hậu điều kiện** | Trạng thái bookmark được lưu vào SQLite cục bộ |

**Luồng sự kiện chính:**

1. Người dùng nhấn biểu tượng bookmark trên trang chi tiết POI.
2. `BookmarkHistoryService` kiểm tra trạng thái bookmark hiện tại.
3. Nếu chưa bookmark: thêm POI vào danh sách bookmark và lưu vào SQLite.
4. Nếu đã bookmark: xóa POI khỏi danh sách bookmark.
5. UI cập nhật biểu tượng bookmark ngay lập tức.
6. Sự kiện `Changed` được phát ra; các ViewModel đăng ký nhận cập nhật tự động.

---

### UC07 — Xem Lịch Sử và Bookmark

| Trường | Nội dung |
|---|---|
| **Mã** | UC07 |
| **Tên** | Xem danh sách POI đã lưu và lịch sử đã ghé thăm |
| **Actor** | Người dùng đã đăng nhập |
| **Tiền điều kiện** | Người dùng đã đăng nhập |
| **Hậu điều kiện** | Danh sách Bookmarks và History hiển thị theo từng tab |

**Luồng sự kiện chính:**

1. Người dùng điều hướng đến trang **Bookmarks & History**.
2. Hệ thống hiển thị tab **Bookmarks** và **History**.
3. Tab Bookmarks: hiển thị danh sách POI đã bookmark từ SQLite.
4. Tab History: hiển thị danh sách POI đã ghé thăm kèm thời gian.
5. Người dùng có thể xóa từng item lịch sử hoặc xóa toàn bộ lịch sử.
6. Người dùng nhấn vào item để xem chi tiết POI.

---

### UC08 — Quét Mã QR Để Xem POI

| Trường | Nội dung |
|---|---|
| **Mã** | UC08 |
| **Tên** | Quét mã QR tại điểm tham quan để xem thông tin tức thì |
| **Actor** | Khách du lịch, Người dùng đã đăng nhập |
| **Tiền điều kiện** | Thiết bị có camera; mã QR tại địa điểm |
| **Hậu điều kiện** | Trang chi tiết POI tương ứng được mở |

**Luồng sự kiện chính:**

1. Người dùng nhấn biểu tượng QR Scan trên ứng dụng.
2. Ứng dụng mở camera và quét mã QR.
3. `QrCodeParserService` phân tích nội dung QR để trích xuất `PoiId`.
4. Hệ thống gọi API `GET /api/pois/{id}` để lấy dữ liệu POI.
5. Trang chi tiết POI mở ra với thông tin tương ứng.

**Luồng thay thế:**

- **A1 — QR không hợp lệ:** Hiển thị thông báo lỗi "Mã QR không nhận ra".
- **A2 — POI không tìm thấy:** API trả về 404; hiển thị thông báo lỗi.

---

### UC09 — Xem Bản Đồ POI

| Trường | Nội dung |
|---|---|
| **Mã** | UC09 |
| **Tên** | Xem vị trí các POI trên bản đồ tương tác |
| **Actor** | Khách du lịch, Người dùng đã đăng nhập |
| **Tiền điều kiện** | Ứng dụng đã khởi động |
| **Hậu điều kiện** | Bản đồ hiển thị với các pin POI; người dùng có thể chọn POI |

**Luồng sự kiện chính:**

1. Người dùng điều hướng đến trang **Map**.
2. Hệ thống gọi API `GET /api/pois` để lấy danh sách POI.
3. Bản đồ hiển thị vị trí người dùng và các pin POI xung quanh.
4. Người dùng nhấn vào một pin để xem tóm tắt thông tin POI.
5. Người dùng nhấn **Open Detail** để chuyển sang trang chi tiết POI.
6. Người dùng có thể nhấn **Refresh** để cập nhật dữ liệu POI trên bản đồ.

---

### UC10 — Xem và Chạy Tour

| Trường | Nội dung |
|---|---|
| **Mã** | UC10 |
| **Tên** | Xem danh sách tour, điều hướng trên bản đồ và phát audio tự động theo lộ trình |
| **Actor** | Khách du lịch, Người dùng đã đăng nhập |
| **Tiền điều kiện** | Có ít nhất một Tour đã published trong hệ thống |
| **Hậu điều kiện** | Tour được bắt đầu; audio phát tự động khi đến điểm |

**Luồng sự kiện chính:**

1. Người dùng vào trang **Explore** và chọn một Tour.
2. Hệ thống gọi API `GET /api/tours` để lấy danh sách tour đã published.
3. Người dùng chọn một tour để xem chi tiết và tải về.
4. Người dùng nhấn **View Tour** để mở màn hình **Tour Map Route**.
5. `TourRouteCatalogService` tải dữ liệu waypoint của tour.
6. `AzureMapsRouteGeometryService` tính toán hình học tuyến đường giữa các POI.
7. Bản đồ hiển thị tuyến đường đầy đủ với các waypoint.
8. `TourRoutePlaybackService` theo dõi tiến trình di chuyển của người dùng.
9. Khi người dùng đến gần waypoint tiếp theo: hệ thống tự động kích hoạt audio thuyết minh.
10. Waypoint được đánh dấu hoàn thành; tiến trình tour cập nhật.

**Luồng thay thế:**

- **A1 — Mất GPS giữa chừng:** Hệ thống dùng vị trí cuối đã biết; cảnh báo người dùng.
- **A2 — Offline:** Tour đã tải về được phát offline; tuyến đường dùng cache.

---

### UC11 — Quản Lý Hồ Sơ Cá Nhân

| Trường | Nội dung |
|---|---|
| **Mã** | UC11 |
| **Tên** | Xem và chỉnh sửa thông tin hồ sơ người dùng |
| **Actor** | Người dùng đã đăng nhập |
| **Tiền điều kiện** | Người dùng đã đăng nhập |
| **Hậu điều kiện** | Thông tin hồ sơ được cập nhật trên server |

**Luồng sự kiện chính:**

1. Người dùng điều hướng đến trang **Profile**.
2. Hệ thống hiển thị tên, email, ảnh đại diện và các tùy chọn tài khoản.
3. Người dùng nhấn **Edit Profile** để mở trang chỉnh sửa.
4. Người dùng cập nhật Họ tên, Mã quốc gia, Số điện thoại.
5. Hệ thống gọi API `PUT /api/auth/profile` (thông qua `ProfileApiClient`) để lưu thay đổi.
6. Ứng dụng hiển thị thông báo thành công và quay lại trang Profile.

**Luồng thay thế:**

- **A1 — Xóa tài khoản:** Người dùng nhấn **Delete Account**; xác nhận và gọi API xóa.

---

### UC12 — Quản Lý Thư Viện Audio

| Trường | Nội dung |
|---|---|
| **Mã** | UC12 |
| **Tên** | Xem, tải về, phát và xóa audio thuyết minh cục bộ |
| **Actor** | Người dùng đã đăng nhập |
| **Tiền điều kiện** | Người dùng đã tải về ít nhất một audio |
| **Hậu điều kiện** | Thư viện audio cập nhật đúng trạng thái |

**Luồng sự kiện chính:**

1. Người dùng vào trang **My Audio Library**.
2. Hệ thống hiển thị danh sách audio với bộ lọc: **All / Downloaded / Pending**.
3. Hiển thị thông tin dung lượng lưu trữ đã dùng.
4. Người dùng nhấn **Download** để tải audio của một POI về máy.
5. Người dùng nhấn **Play** để phát audio ngay lập tức.
6. Người dùng nhấn **Remove** để xóa audio khỏi bộ nhớ cục bộ.
7. Người dùng có thể nhấn **Retry Failed** để thử lại các download thất bại.

---

### UC13 — Tự Động Kích Hoạt Audio Theo Vị Trí (Geofencing)

| Trường | Nội dung |
|---|---|
| **Mã** | UC13 |
| **Tên** | Hệ thống tự động phát audio khi người dùng tiến vào vùng geofence của POI |
| **Actor** | Hệ thống (GPS + Geofence Engine) |
| **Tiền điều kiện** | Tour đang được phát; GPS đang hoạt động; audio POI đã được cache cục bộ |
| **Hậu điều kiện** | Audio thuyết minh phát tự động; lịch sử ghé thăm được ghi lại |

**Luồng sự kiện chính:**

1. `LocationPollingService` liên tục thu thập vị trí GPS theo chu kỳ.
2. `PoiGeofenceService` so sánh vị trí hiện tại với bán kính geofence của từng POI.
3. Khi người dùng bước vào vùng geofence: sự kiện `OnPoiEntered` được phát ra.
4. `AutoAudioTriggerService` nhận sự kiện và kiểm tra điều kiện (chưa phát gần đây, audio có sẵn).
5. `AudioService` phát audio thuyết minh của POI (ưu tiên file cục bộ, fallback TTS).
6. `BookmarkHistoryService` ghi lại POI vào lịch sử ghé thăm.
7. Màn hình **Now Playing** cập nhật tên POI đang phát.

**Luồng thay thế:**

- **A1 — Audio chưa download:** `AudioService` phát qua TTS hoặc stream trực tiếp từ URL.
- **A2 — Đã phát gần đây:** Hệ thống bỏ qua để tránh phát lặp (debounce).

---

### UC14 — Đăng Nhập Admin

| Trường | Nội dung |
|---|---|
| **Mã** | UC14 |
| **Tên** | Quản trị viên đăng nhập vào hệ thống Admin Web |
| **Actor** | Quản trị viên |
| **Tiền điều kiện** | Quản trị viên có tài khoản admin |
| **Hậu điều kiện** | Quản trị viên được xác thực; có thể truy cập các chức năng quản lý |

**Luồng sự kiện chính:**

1. Quản trị viên truy cập trang đăng nhập Admin Web.
2. Nhập thông tin đăng nhập (username/password).
3. Hệ thống xác thực qua `AdminCredentialsOptions` cấu hình trong `appsettings`.
4. Nếu hợp lệ: tạo session/cookie; chuyển hướng đến **Dashboard**.
5. Dashboard hiển thị tổng quan: số POI, Tour, Người dùng.

---

### UC15 — Quản Lý POI (Admin)

| Trường | Nội dung |
|---|---|
| **Mã** | UC15 |
| **Tên** | Tạo, chỉnh sửa, xóa và xem danh sách POI |
| **Actor** | Quản trị viên |
| **Tiền điều kiện** | Quản trị viên đã đăng nhập |
| **Hậu điều kiện** | Dữ liệu POI được cập nhật trong cơ sở dữ liệu |

**Luồng sự kiện chính:**

1. Quản trị viên vào mục **POIs** trên Admin Web.
2. Hệ thống gọi `GET /api/pois` và hiển thị danh sách tất cả POI.
3. **Tạo POI:** Admin nhấn **New**, điền thông tin (tên, mô tả, tọa độ, hình ảnh, ngôn ngữ, SpeechText) và nhấn **Save**. Hệ thống gọi `POST /api/pois`.
4. **Chỉnh sửa POI:** Admin chọn POI, chỉnh sửa thông tin, nhấn **Save**. Hệ thống gọi `PUT /api/pois/{id}`.
5. **Xóa POI:** Admin nhấn **Delete**, xác nhận. Hệ thống gọi `DELETE /api/pois/{id}`.
6. **Nhập SpeechText đa ngôn ngữ:** Admin có thể nhập nội dung thuyết minh bằng nhiều ngôn ngữ và lưu theo từng `languageCode`.

**Luồng thay thế:**

- **A1 — POI đang được dùng trong Tour:** Xóa thất bại; hệ thống trả về lỗi `409 Conflict` với thông báo giải thích.

---

### UC16 — Quản Lý Tour (Admin)

| Trường | Nội dung |
|---|---|
| **Mã** | UC16 |
| **Tên** | Tạo, chỉnh sửa, xóa Tour và gắn POI vào Tour |
| **Actor** | Quản trị viên |
| **Tiền điều kiện** | Quản trị viên đã đăng nhập; đã có POI trong hệ thống |
| **Hậu điều kiện** | Tour được cập nhật; danh sách POI trong tour thay đổi |

**Luồng sự kiện chính:**

1. Quản trị viên vào mục **Tours** trên Admin Web.
2. Hệ thống gọi `GET /api/admin/tours` và hiển thị tất cả tour.
3. **Tạo Tour:** Admin điền tên, mô tả, ngôn ngữ, POI neo (AnchorPoi), ảnh bìa, trạng thái Published. Hệ thống gọi `POST /api/admin/tours`.
4. **Thêm POI vào Tour:** Admin chọn POI từ danh sách và gắn vào tour theo thứ tự và khoảng cách.
5. **Xuất bản / Ẩn Tour:** Admin thay đổi trạng thái `IsPublished` để kiểm soát hiển thị trên mobile.
6. **Xóa Tour:** Admin xác nhận xóa; hệ thống gọi `DELETE /api/admin/tours/{id}`.

---

### UC17 — Quản Lý Người Dùng (Admin)

| Trường | Nội dung |
|---|---|
| **Mã** | UC17 |
| **Tên** | Xem, tạo, chỉnh sửa và xóa tài khoản người dùng |
| **Actor** | Quản trị viên |
| **Tiền điều kiện** | Quản trị viên đã đăng nhập |
| **Hậu điều kiện** | Tài khoản người dùng được cập nhật trong cơ sở dữ liệu |

**Luồng sự kiện chính:**

1. Quản trị viên vào mục **Users** trên Admin Web.
2. Hệ thống gọi `GET /api/admin/users` và hiển thị danh sách người dùng.
3. **Tạo tài khoản:** Admin điền Username, Email, Password, Role. Hệ thống gọi `POST /api/admin/users`.
4. **Chỉnh sửa:** Admin thay đổi thông tin người dùng. Hệ thống gọi `PUT /api/admin/users/{id}`.
5. **Xóa tài khoản:** Admin xác nhận xóa. Hệ thống gọi `DELETE /api/admin/users/{id}`.
6. **Phân quyền:** Admin xem và gán Role từ danh sách `GET /api/admin/users/roles`.

**Luồng thay thế:**

- **A1 — Trùng email/username:** API trả về `409 Conflict`; Admin được thông báo.

---

## 4. Ma Trận Use Case — Actor

| Use Case | Guest | User | Admin | System |
|---|:---:|:---:|:---:|:---:|
| UC01 — Xem POI gần vị trí | ✓ | ✓ | | |
| UC02 — Tìm kiếm điểm đến | ✓ | ✓ | | |
| UC03 — Đăng nhập | ✓ | | | |
| UC04 — Đăng xuất | | ✓ | | |
| UC05 — Xem POI & phát audio | | ✓ | | |
| UC06 — Bookmark POI | | ✓ | | |
| UC07 — Xem lịch sử & bookmark | | ✓ | | |
| UC08 — Quét QR | ✓ | ✓ | | |
| UC09 — Xem bản đồ POI | ✓ | ✓ | | |
| UC10 — Xem & chạy Tour | ✓ | ✓ | | |
| UC11 — Quản lý hồ sơ | | ✓ | | |
| UC12 — Thư viện Audio | | ✓ | | |
| UC13 — Auto Audio (Geofence) | | | | ✓ |
| UC14 — Đăng nhập Admin | | | ✓ | |
| UC15 — Quản lý POI | | | ✓ | |
| UC16 — Quản lý Tour | | | ✓ | |
| UC17 — Quản lý Người dùng | | | ✓ | |

---

## 5. Quan Hệ Giữa Các Use Case

```
UC03 (Đăng nhập)
  └── <<include>> UC04 (Đăng xuất)
  └── <<extend>>  UC11 (Quản lý hồ sơ)
  └── <<extend>>  UC06 (Bookmark)
  └── <<extend>>  UC07 (Lịch sử)
  └── <<extend>>  UC12 (Thư viện Audio)

UC10 (Chạy Tour)
  └── <<include>> UC09 (Bản đồ)
  └── <<include>> UC13 (Auto Geofence Audio)
  └── <<extend>>  UC05 (Phát Audio thủ công)

UC15 (Quản lý POI)
  └── <<include>> UC14 (Đăng nhập Admin)

UC16 (Quản lý Tour)
  └── <<include>> UC14 (Đăng nhập Admin)
  └── <<include>> UC15 (POI phải tồn tại)

UC17 (Quản lý User)
  └── <<include>> UC14 (Đăng nhập Admin)
```

---

*Tài liệu này được tạo tự động dựa trên phân tích source code của dự án TravelApp.*
