using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

var outputPath = ResolveOutputPath();
var generatedDate = DateTime.UtcNow.ToString("dd/MM/yyyy");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

using var stream = File.Create(outputPath);
using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
var mainPart = doc.AddMainDocumentPart();
mainPart.Document = new Document(new Body());
var body = mainPart.Document.Body!;

var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
stylesPart.Styles = BuildStyles();

void Add(params OpenXmlElement[] elements)
{
    foreach (var e in elements)
        body.AppendChild(e.CloneNode(true));
}

Paragraph BulletPara(string text)
{
    var p = new Paragraph();
    p.AppendChild(new Run(new Text("• " + text) { Space = SpaceProcessingModeValues.Preserve }));
    return p;
}

// --- Trang bìa ---
Add(
    Para("TÀI LIỆU YÊU CẦU SẢN PHẨM (PRD)", bold: true, sizeHalfPoints: 32, justify: false),
    Para("Hệ thống TravelApp", bold: true, sizeHalfPoints: 28, justify: false),
    Para("Ứng dụng du lịch – POI – Tour có hướng dẫn âm thanh đa ngôn ngữ", sizeHalfPoints: 22, justify: false),
    EmptyPara(),
    Para("Phiên bản tài liệu: 1.0", justify: false),
    Para($"Ngày: {generatedDate}", justify: false),
    Para("Mục đích: Báo cáo / trình bày đồ án – đánh giá kiến trúc & kiểm thử", justify: false),
    Para("Nguồn phân tích: Mã nguồn repository TravelApp (API, Admin, Mobile, Infrastructure)", justify: false),
    PageBreak()
);

// --- Mục lục (cập nhật trong Word: References > Update Table) ---
Add(
    Heading("MỤC LỤC", 1),
    Para("Trong Microsoft Word: chọn dòng này và chèn Mục lục tự động (Table of Contents) nếu cần đánh số trang chính xác. Tài liệu đã dùng kiểu Heading 1/2 để hỗ trợ sinh TOC.", italic: true),
    PageBreak()
);

// --- 1. Tóm tắt điều hành ---
Add(
    Heading("1. Tóm tắt điều hành", 1),
    Para("TravelApp là hệ thống gồm ba thành phần chính: (1) TravelApp.Api — ASP.NET Core Web API cung cấp xác thực JWT, truy vấn POI (điểm tham quan) có phân trang và lọc theo bán kính địa lý, truy vấn tour đã xuất bản và API quản trị tour/người dùng; (2) TravelApp.Admin.Web — ứng dụng web MVC dùng cookie authentication với tài khoản cấu hình (appsettings) để quản trị nội dung thông qua HttpClient gọi API; (3) TravelApp.Mobile — ứng dụng .NET MAUI cho người dùng cuối: khám phá tour, xem chi tiết, phát âm thanh theo vị trí (geofence) khi đi theo lộ trình."),
    Para("Cơ sở dữ liệu SQL Server được truy cập qua Entity Framework Core; mật khẩu người dùng băm bằng BCrypt. Seed dữ liệu demo (POI, tour, audio, tài khoản đăng nhập) được thực hiện khi khởi động API trong môi trường phát triển."),
    Para("Tài liệu này mô tả yêu cầu sản phẩm suy ra từ hiện trạng mã nguồn, ưu tiên sơ đồ sequence cho các luồng chính và bộ test case phục vụ báo cáo/kiểm định.")
);

// --- 2. Bối cảnh & vấn đề ---
Add(
    Heading("2. Bối cảnh và vấn đề nghiệp vụ", 1),
    Para("Du khách cần lộ trình rõ ràng, nội dung đa ngôn ngữ và trải nghiệm hands-free (audio tự động khi đến gần điểm). Đơn vị vận hành cần công cụ quản trị POI/tour và (một phần) người dùng hệ thống. TravelApp tách lớp Application (DTO, interface dịch vụ), Infrastructure (triển khai EF, Auth) và Api/Mobile/Admin để dễ mở rộng."),
    Para("Ràng buộc hiện tại ghi nhận trong mã nguồn: refresh token ở AuthService mang tính demo (không gắn user thật); API POI có thao tác Create/Update/Delete công khai — trong triển khai thực tế cần bảo vệ bằng phân quyền admin.")
);

// --- 3. Mục tiêu & phạm vi ---
Add(
    Heading("3. Mục tiêu sản phẩm và phạm vi", 1),
    Heading("3.1. Mục tiêu", 2),
    Para("Cung cấp backend ổn định cho ứng dụng mobile và admin; hỗ trợ định danh người dùng mobile bằng JWT; cung cấp dữ liệu POI/tour đa ngôn ngữ; cho phép phát audio theo ngôn ngữ và vị trí khi người dùng thực hiện tour."),
    Heading("3.2. Phạm vi trong phiên bản phân tích", 2),
    Para("Gồm các project: TravelApp.Api, TravelApp.Application, TravelApp.Domain, TravelApp.Infrastructure, TravelApp.Admin.Web, TravelApp.Mobile. Không bao gồm hạ tầng triển khai CI/CD, giám sát production, hay ứng dụng App Store chi tiết."),
    Heading("3.3. Ngoài phạm vi (đề xuất roadmap)", 2),
    Para("Lưu trữ refresh token server-side; OAuth bên thứ ba; thanh toán in-app; CMS media đầy đủ; bản đồ offline vector; analytics nâng cao.")
);

// --- 4. Stakeholders ---
Add(
    Heading("4. Các bên liên quan", 1),
    BulletPara("Người dùng ứng dụng mobile: khám phá tour/POI, đăng nhập, nghe hướng dẫn."),
    BulletPara("Quản trị viên nội dung: thao tác qua Admin Web (cấu hình credential), đồng bộ với API."),
    BulletPara("Đội phát triển / DevOps: vận hành API, connection string, JWT secret, migration DB."),
    BulletPara("Giảng viên / hội đồng đánh giá: đọc PRD, sequence diagram và test case để đối chiếu kiến trúc.")
);

// --- 5. Kiến trúc ---
Add(
    Heading("5. Kiến trúc tổng thể", 1),
    Para("Kiến trúc phân lớp: Presentation (MAUI, Admin MVC, Api controllers) → Application (abstractions + DTO) → Infrastructure (EF Core DbContext, AuthService, query services) → SQL Server. JWT được cấu hình trong Program.cs của Api với issuer/audience/secret."),
    MonoBlock(
        @"[Mobile MAUI] --HTTPS JSON--> [TravelApp.Api]
[Admin MVC]   --HTTPS JSON--> [TravelApp.Api]
                        |
                        v
              [Application layer: DTO + interfaces]
                        |
                        v
              [Infrastructure: EF Core + services]
                        |
                        v
                   [SQL Server]"),
    Para("Endpoint sức khỏe: GET /health trả về trạng thái OK. Open API document map khi môi trường Development.")
);

// --- 6. Thành phần chi tiết ---
Add(
    Heading("6. Mô tả thành phần hệ thống", 1),
    Heading("6.1. TravelApp.Api", 2),
    Para("Controllers: Auth (login, refresh, profile có [Authorize]), Pois (CRUD + get list/detail), Tours (public published), AdminTours, AdminUsers. Middleware: UseAuthentication, UseAuthorization. Khởi động: migrate DB, seed POI/tour demo, đảm bảo cột SpeechText/SpeechTextsJson khi DB legacy."),
    Heading("6.2. TravelApp.Admin.Web", 2),
    Para("Xác thực cookie so khớp AdminCredentialsOptions (UserName, Password, Roles). Gọi TravelAppApiClient với BaseUrl cấu hình để CRUD POI, tour, user qua REST."),
    Heading("6.3. TravelApp.Mobile", 2),
    Para("ViewModels: Explore, TourDetail, Login, Profile, Search, v.v. Dịch vụ: AuthApiClient, TourRouteCatalogService, TourRoutePlaybackService (theo dõi vị trí, chọn waypoint, phát audio), LocalDatabaseService (SQLite hoặc tương đương), AudioService. Shell điều hướng Explore và POI Live."),
    Heading("6.4. Miền dữ liệu (Domain)", 2),
    Para("Thực thể: User, Role, UserRole, Poi, PoiLocalization, PoiAudio, Tour, TourPoi. Quan hệ: Tour neo vào AnchorPoiId; TourPoi sắp xếp thứ tự và khoảng cách giữa các điểm.")
);

// --- 7. Yêu cầu chức năng ---
Add(
    Heading("7. Yêu cầu chức năng (theo mã nguồn)", 1),
    Heading("7.1. Xác thực & hồ sơ", 2),
    Para("FR-AUTH-01: Người dùng gửi email/mật khẩu tới POST /api/auth/login; hệ thống trả JWT, refresh token, expires, user id, roles nếu có."),
    Para("FR-AUTH-02: GET /api/auth/profile yêu cầu Bearer token; trả UserProfileDto."),
    Para("FR-AUTH-03: Refresh token hiện là luồng demo — cần được thay thế trong sản phẩm thương mại."),
    Heading("7.2. POI", 2),
    Para("FR-POI-01: GET /api/pois hỗ trợ lang, pageNumber, pageSize, lat/lng/radius để lọc theo vị trí."),
    Para("FR-POI-02: GET /api/pois/{id} trả chi tiết PoiMobileDto gồm localizations, audio, speech texts."),
    Para("FR-POI-03: POST/PUT/DELETE cho phép tạo/cập nhật/xóa POI — yêu cầu bảo vệ admin trong tương lai."),
    Heading("7.3. Tour", 2),
    Para("FR-TOUR-01: GET /api/tours trả danh sách tour xuất bản; query lang để localize nếu service hỗ trợ."),
    Para("FR-TOUR-02: GET /api/tours/{anchorPoiId} trả TourRouteDto với waypoints và tổng quãng đường."),
    Para("FR-TOUR-03: API admin /api/admin/tours CRUD tour nội bộ."),
    Heading("7.4. Mobile", 2),
    Para("FR-MOB-01: Người dùng đăng nhập qua LoginViewModel; lưu trạng thái đăng nhập và roles."),
    Para("FR-MOB-02: Khám phá tour/POI; mở chi tiết tour; phát lộ trình — TourRoutePlaybackService kích hoạt audio khi vào bán kính (geofence) với cooldown giữa các lần phát.")
);

// --- 8. Sequence diagrams (textual UML) ---
Add(
    Heading("8. Sơ đồ tuần tự (Sequence Diagram) — dạng mô tả UML", 1),
    Para("Phần sau mô phỏng sequence diagram ở dạng văn bản (có thể sao chép sang PlantUML, draw.io hoặc Enterprise Architect). Đây là phần ưu tiên theo yêu cầu báo cáo.")
);

Add(
    Heading("8.1. Đăng nhập Mobile → API (JWT)", 2),
    MonoBlock(
        @"actor User
participant ""MAUI App"" as M
participant ""AuthController"" as C
participant ""AuthService"" as S
participant ""DB"" as D

User -> M: Nhập email/password
M -> C: POST /api/auth/login {email,password}
C -> S: LoginAsync(email,password)
S -> D: Query User + Roles
D --> S: user + hash
S -> S: BCrypt.Verify
alt hợp lệ
  S -> S: GenerateAccessToken (JWT, claims: sub, email, roles)
  S --> C: AuthResultDto
  C --> M: 200 OK + token
  M -> M: Lưu trạng thái đăng nhập / roles
else không hợp lệ
  C --> M: 401 Unauthorized
end")
);

Add(
    Heading("8.2. Lấy danh sách POI có phân trang & lọc địa lý", 2),
    MonoBlock(
        @"participant ""Mobile/Admin"" as Client
participant ""PoisController"" as P
participant ""PoiQueryService"" as Q
participant ""DB"" as D

Client -> P: GET /api/pois?lang=vi&page=1&pageSize=20&lat=&lng=&radius=
P -> Q: GetAllAsync(PoiQueryRequestDto)
Q -> D: Truy vấn POI + localize + audio
D --> Q: dữ liệu + TotalCount
Q --> P: PagedResultDto<PoiMobileDto>
P --> Client: 200 OK JSON")
);

Add(
    Heading("8.3. Lấy tour công khai theo anchor POI", 2),
    MonoBlock(
        @"participant ""Mobile"" as M
participant ""ToursController"" as T
participant ""TourQueryService"" as Q
participant ""DB"" as D

M -> T: GET /api/tours/{anchorPoiId}?lang=vi
T -> Q: GetByAnchorPoiIdAsync(anchorPoiId, lang)
Q -> D: Tour + TourPois + Poi details
D --> Q: graph route + waypoints
Q --> T: TourRouteDto
T --> M: 200 OK hoặc 404")
);

Add(
    Heading("8.4. Phát tour theo vị trí (TourRoutePlaybackService)", 2),
    MonoBlock(
        @"participant ""User"" as U
participant ""LocationTracker"" as L
participant ""PlaybackSvc"" as R
participant ""AudioService"" as A
participant ""LocalDB"" as DB

U -> R: StartAsync(TourRouteDto)
R -> A: StopAsync (reset)
R -> L: StartAsync + subscribe LocationChanged
L --> R: LocationSample (định kỳ)
R -> R: EvaluateLocation (khoảng cách, geofence, cooldown)
alt vào điểm kích hoạt
  R -> A: Phát audio theo ngôn ngữ ưu tiên
  R -> DB: (tuỳ chọn) ghi nhận lịch sử
  R --> U: ActiveWaypointChanged (UI cập nhật)
end
U -> R: StopAsync
R -> L: StopAsync / hủy đăng ký")
);

Add(
    Heading("8.5. Admin Web: thao tác CRUD POI qua API", 2),
    MonoBlock(
        @"actor Admin
participant ""Browser"" as B
participant ""Admin MVC"" as W
participant ""TravelAppApiClient"" as H
participant ""PoisController"" as P

Admin -> B: Submit form POI
B -> W: POST (cookie auth)
W -> H: CreatePoiAsync(UpsertPoiRequestDto)
H -> P: POST /api/pois JSON
P --> H: 201 Created + PoiMobileDto
H --> W: DTO
W --> B: Redirect / thông báo")
);

Add(
    Heading("8.6. Quản trị người dùng (API)", 2),
    MonoBlock(
        @"participant ""Admin Client"" as C
participant ""AdminUsersController"" as U
participant ""UserAdminService"" as S
participant ""DB"" as D

C -> U: GET /api/admin/users
U -> S: GetAllAsync()
S -> D: Query users + roles
D --> S: list
S --> U: UserAdminDto[]
U --> C: 200 OK

C -> U: POST /api/admin/users (create)
U -> S: CreateAsync(request)
alt thành công
  U --> C: 201 Created
else xung đột dữ liệu
  U --> C: 409 Conflict
end")
);

Add(
    Heading("8.7. Kiểm tra sức khỏe dịch vụ", 2),
    MonoBlock(
        @"participant Monitor
participant Api

Monitor -> Api: GET /health
Api --> Monitor: 200 {status, service}")
);

Add(
    Heading("8.8. Làm mới token (refresh) — ghi chú kiến trúc hiện tại", 2),
    Para("Luồng dưới đây phản ánh trách nhiệm của endpoint; cần hoàn thiện lưu trữ refresh token trước khi đưa vào môi trường production.", italic: true),
    MonoBlock(
        @"participant ""Client"" as C
participant ""AuthController"" as A
participant ""AuthService"" as S

C -> A: POST /api/auth/refresh { refreshToken }
A -> S: RefreshTokenAsync(token)
note right of S: Hiện tại: chấp nhận token khác rỗng\nvà phát hành JWT mới (demo)
S --> A: AuthResultDto (access + refresh mới)
A --> C: 200 hoặc 401")
);

Add(
    Heading("8.9. Khám phá trên Mobile (catalog tour)", 2),
    Para("Mobile gọi dịch vụ catalog (ITourRouteCatalogService) để lấy TourRouteDto đã publish; có thể kết hợp ngôn ngữ ưu tiên và cache cục bộ."),
    MonoBlock(
        @"actor User
participant ""ExploreViewModel"" as E
participant ""TourRouteCatalog"" as Cat
participant ""ToursController"" as API
participant ""TourQueryService"" as Q

User -> E: Mở tab Explore / kéo refresh
E -> Cat: Get published routes (async)
Cat -> API: GET /api/tours?lang=...
API -> Q: GetAllPublishedAsync
Q --> API: TourRouteDto[]
API --> Cat: JSON
Cat --> E: bind UI collections
E --> User: Hiển thị card tour / POI")
);

Add(
    Heading("8.10. Offline-first: đọc cache trước, đồng bộ sau", 2),
    MonoBlock(
        @"actor User
participant ""ExploreViewModel"" as VM
participant ""LocalDatabaseService"" as LDB
participant ""PoiApiService"" as API
participant ""SyncWorker"" as SW

User -> VM: Mở ứng dụng khi mạng yếu/mất mạng
VM -> LDB: GetCachedPoisAsync(lang)
LDB --> VM: Cached list
VM --> User: Hiển thị dữ liệu cục bộ ngay lập tức
par có mạng
  VM -> SW: TriggerBackgroundSync()
  SW -> API: GET /api/pois?lang=...
  API --> SW: dữ liệu mới
  SW -> LDB: Upsert cache + timestamp
  SW --> VM: DataUpdated event
  VM --> User: Refresh danh sách
else offline
  VM --> User: Badge ""Offline mode""
end")
);

Add(
    Heading("8.11. Auto audio theo geofence và chống phát lặp", 2),
    MonoBlock(
        @"actor User
participant ""LocationPollingService"" as LP
participant ""PoiGeofenceService"" as GF
participant ""AutoAudioTriggerService"" as AT
participant ""AudioService"" as AS

User -> AT: Start(tour, language)
AT -> LP: Subscribe OnLocationUpdated
LP --> AT: Location(lat,lng)
AT -> GF: Evaluate(poiList, location)
GF --> AT: EnteredPoiIds / ExitedPoiIds
alt có POI mới đi vào vùng kích hoạt
  AT -> AT: Check cooldown + played history
  AT -> AS: PlayAsync(audioUrl, language)
  AS --> AT: Success/Fail event
else không có POI mới
  AT -> AT: No-op
end
User -> AT: Stop()
AT -> LP: Unsubscribe")
);

Add(
    PageBreak(),
    Heading("8.12. Bổ sung: ma trận tương tác theo lớp", 2),
    Para("Để phục vụ báo cáo, bảng sau tóm tắt hướng phụ thuộc giữa các lớp: UI (MAUI) phụ thuộc ViewModels; ViewModels phụ thuộc dịch vụ trừu tượng (IAuthApiClient, ITourRouteCatalogService, ITourRoutePlaybackService); các dịch vụ runtime gọi HttpClient tới Api; Api không gọi ngược Mobile."),
    Para("Đối với Admin Web, controller MVC gọi ITravelAppApiClient — tách biệt hoàn toàn với schema cookie — giúp kiểm thử đơn vị bằng mock HttpMessageHandler nếu cần."),
    Para("Hạ tầng EF Core map thực thể Domain sang bảng SQL; migration và các lệnh Ensure* trong Program.cs xử lý trường hợp cơ sở dữ liệu đã tồn tại từ phiên bản trước (legacy POI table) mà chưa có lịch sử migration EF.")
);

// --- 9. Phi chức năng ---
Add(
    Heading("9. Yêu cầu phi chức năng", 1),
    BulletPara("Hiệu năng: phân trang POI mặc định pageSize=20; client admin gọi pageSize=100 khi đồng bộ."),
    BulletPara("Bảo mật: JWT HS256; HTTPS redirect khi không Development; mật khẩu BCrypt."),
    BulletPara("Khả dụng: endpoint /health cho probe đơn giản."),
    BulletPara("Offline-first: mobile ưu tiên đọc cache cục bộ, đồng bộ nền khi có mạng."),
    BulletPara("Đa ngôn ngữ: tham số lang trên API; POI có localizations và audio theo LanguageCode."),
    BulletPara("Ghi log: Mobile dùng ILogger trong dịch vụ phát tour.")
);

// --- 10. Dữ liệu & API tóm tắt ---
Add(
    Heading("10. Mô hình dữ liệu và API chính", 1),
    Para("Bảng POI lưu tọa độ, bán kính geofence, SpeechText/SpeechTextsJson (mở rộng). Tour liên kết AnchorPoiId và tập TourPoi. PoiAudio chứa URL file âm thanh Azure Blob (theo seed mẫu)."),
    Para("REST tóm tắt: POST /api/auth/login, POST /api/auth/refresh, GET /api/auth/profile (Bearer); GET/POST/PUT/DELETE /api/pois; GET /api/tours, GET /api/tours/{anchorPoiId}; /api/admin/tours, /api/admin/users, ...")
);

// --- 11. Test cases ---
Add(
    Heading("11. Ma trận kiểm thử (Test cases)", 1),
    Para("Bảng dưới đây phục vụ báo cáo: mỗi testcase có mã, mức độ, bước, kết quả mong đợi. Có thể nhập sang TestRail/Azure DevOps."),
    TestCaseTable()
);

Add(
    PageBreak(),
    Heading("11.1. Tiêu chí nghiệm thu (UAT) gợi ý", 2),
    Para("UAT-01: Người dùng mới có thể hoàn tất đăng nhập bằng tài khoản demo trong thời gian ngắn (< 2 phút) và xem được hồ sơ trên mobile."),
    Para("UAT-02: Danh sách POI hiển thị đúng phân trang; khi đổi ngôn ngữ (tham số lang), tiêu đề/mô tả phản ánh dữ liệu localization nếu có."),
    Para("UAT-03: Tour được neo tại AnchorPoiId hiển thị đủ waypoint theo SortOrder; tổng quãng đường TotalDistanceMeters hợp lý với seed."),
    Para("UAT-04: Khi bắt đầu tour, ứng dụng yêu cầu quyền vị trí; nếu từ chối, cần có thông báo rõ ràng (theo thiết kế UI hiện tại)."),
    Para("UAT-05: Admin đăng nhập web thành công với credential cấu hình; thao tác CRUD POI phản hồi qua API và hiển thị lỗi khi BaseUrl sai."),
    Heading("11.2. Ma trận endpoint REST (rút gọn)", 2),
    ApiEndpointTable(),
    Heading("11.3. Hướng dẫn tái tạo sơ đồ sequence trong Word / Visio", 2),
    Para("Bước 1: Sao chép nội dung các khối monospace trong mục 8 vào PlantUML với @startuml/@enduml (hoặc dùng trình tạo sequence online). Bước 2: Xuất PNG/SVG và chèn vào Word (Insert > Pictures). Bước 3: Căn caption Hình X — mô tả luồng. Bước 4: Cập nhật mục lục hình nếu báo cáo yêu cầu."),
    Para("Nếu hội đồng yêu cầu UML chuẩn, nên thay thế khối 'alt/else' bằng combined fragment và ghi rõ điều kiện guard trên nhánh."),
    Para("Đối với luồng phát audio theo vị trí, nên bổ sung tham số: bán kính kích hoạt mặc định (ví dụ 120m trong TourRoutePlaybackService) và cooldown 20 giây để tránh phát lặp khi GPS dao động."),
    Heading("11.4. Kịch bản kiểm thử mobile (chi tiết hành vi)", 2),
    Para("MT-01 — Đăng xuất: từ menu Explore, chọn Sign Out; trạng thái AuthStateService.IsLoggedIn = false; menu hiển thị Sign In."),
    Para("MT-02 — Điều hướng tour: từ card tour, mở TourDetail; dữ liệu cover, mô tả, waypoint load từ API/catalog."),
    Para("MT-03 — Phát thử: chọn waypoint thủ công (SelectWaypointAsync) phải phát audio tương ứng ngôn ngữ nếu file URL hợp lệ."),
    Para("MT-04 — Ổn định: kết thúc tour (StopAsync) phải hủy đăng ký LocationChanged và không còn sự kiện phát sau khi dừng."),
    Para("MT-05 — Lỗi mạng: mô phỏng mất kết nối khi gọi API; ứng dụng không crash — client trả null hoặc danh sách rỗng theo mã TravelAppApiClient."),
    Heading("11.5. Checklist báo cáo đồ án", 2),
    BulletPara("Đính kèm sơ đồ kiến trúc tổng (lớp + deployment)."),
    BulletPara("Trích dẫn endpoint chính và phương thức HTTP."),
    BulletPara("Đính kèm ảnh chụp màn hình mobile (Explore, Tour detail, Login) và admin (danh sách POI)."),
    BulletPara("Ghi rõ hạn chế: refresh token demo, CRUD POI công khai."),
    Para("Ghi chú độ dài: tài liệu được soạn để đạt khoảng 18–24 trang khi in khổ A4, font Times New Roman cỡ 12–13pt, giãn dòng 1,15–1,5; số trang thực tế phụ thuộc vào cỡ lề và bảng biểu Word tự động ngắt trang.", italic: true)
);

// --- 12. Rủi ro ---
Add(
    Heading("12. Rủi ro và giả định", 1),
    BulletPara("Rủi ro: CRUD POI công khai có thể bị lạm dụng — cần khóa API admin và rate limit."),
    BulletPara("Rủi ro: Refresh token demo không an toàn — cần lưu server-side và rotation."),
    BulletPara("Giả định: SQL Server tồn tại và connection string hợp lệ; JWT secret đủ dài trong production."),
    BulletPara("Giả định: Quyền vị trí trên thiết bị thật được người dùng cấp cho mobile.")
);

// --- 13. Lộ trình ---
Add(
    Heading("13. Lộ trình đề xuất", 1),
    Para("Giai đoạn 1: Khóa API chỉnh sửa nội dung bằng JWT role Admin; audit log. Giai đoạn 2: Refresh token bền vững. Giai đoạn 3: Tối ưu đồng bộ offline và CDN cho audio. Giai đoạn 4: Quan sát (OpenTelemetry) và kiểm thử tự động API (Postman/Newman)."),
    PageBreak(),
    Heading("14. Phụ lục — tài khoản demo (Development)", 1),
    Para("Theo ProgramStartupHelpers và XML remarks AuthController: demo@example.com / Demo@123456; khanh@example.com / Khanh@123456; guest@example.com / Guest@123456. Chỉ dùng môi trường phát triển."),
    Heading("15. Từ điển thuật ngữ", 1),
    BulletPara("POI (Point of Interest): điểm tham quan."),
    BulletPara("Tour route: chuỗi waypoint theo SortOrder."),
    BulletPara("Geofence: vùng bán kính quanh tọa độ để kích hoạt audio."),
    Para("— Hết tài liệu —", italic: true)
);

mainPart.Document.Save();
Console.WriteLine("Written: " + outputPath);

// --- helpers ---

static Styles BuildStyles()
{
    return new Styles(
        new DocDefaults(
            new RunPropertiesDefault(
                new RunPropertiesBaseStyle(
                    new RunFonts { Ascii = "Times New Roman" },
                    new FontSize { Val = "24" }))),
        new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Heading1",
            StyleName = new StyleName { Val = "heading 1" },
            BasedOn = new BasedOn { Val = "Normal" },
            NextParagraphStyle = new NextParagraphStyle { Val = "Normal" },
            StyleRunProperties = new StyleRunProperties(
                new Bold(),
                new FontSize { Val = "32" },
                new RunFonts { Ascii = "Times New Roman" })
        },
        new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Heading2",
            StyleName = new StyleName { Val = "heading 2" },
            BasedOn = new BasedOn { Val = "Normal" },
            NextParagraphStyle = new NextParagraphStyle { Val = "Normal" },
            StyleRunProperties = new StyleRunProperties(
                new Bold(),
                new FontSize { Val = "28" },
                new RunFonts { Ascii = "Times New Roman" })
        },
        new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Normal",
            StyleName = new StyleName { Val = "Normal" },
            StyleRunProperties = new StyleRunProperties(
                new FontSize { Val = "24" },
                new RunFonts { Ascii = "Times New Roman" })
        },
        new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Mono",
            StyleName = new StyleName { Val = "Mono" },
            BasedOn = new BasedOn { Val = "Normal" },
            StyleRunProperties = new StyleRunProperties(
                new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" },
                new FontSize { Val = "20" })
        });
}

static Paragraph Heading(string text, int level)
{
    var style = level <= 1 ? "Heading1" : "Heading2";
    var p = new Paragraph();
    p.AppendChild(new ParagraphProperties(new ParagraphStyleId { Val = style }));
    p.AppendChild(new Run(new Text(text)));
    return p;
}

static Paragraph Para(string text, bool bold = false, bool italic = false, int sizeHalfPoints = 24, bool justify = true)
{
    var p = new Paragraph();
    var props = new ParagraphProperties();
    if (justify)
        props.AppendChild(new Justification { Val = JustificationValues.Both });
    p.AppendChild(props);
    var run = new Run();
    if (bold) run.AppendChild(new Bold());
    if (italic) run.AppendChild(new Italic());
    run.AppendChild(new FontSize { Val = sizeHalfPoints.ToString() });
    run.AppendChild(new RunFonts { Ascii = "Times New Roman" });
    run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    p.AppendChild(run);
    return p;
}

static Paragraph EmptyPara() => new(new Run(new Text(" ")));

static Paragraph PageBreak() =>
    new(new Run(new Break { Type = BreakValues.Page }));

static Paragraph MonoBlock(string text)
{
    var p = new Paragraph();
    p.AppendChild(new ParagraphProperties(
        new ParagraphStyleId { Val = "Mono" },
        new SpacingBetweenLines { Before = "120", After = "120" },
        new Indentation { Left = "360" },
        new Justification { Val = JustificationValues.Left }));
    foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
    {
        var r = new Run();
        r.AppendChild(new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" });
        r.AppendChild(new FontSize { Val = "20" });
        r.AppendChild(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(r);
        p.AppendChild(new Break());
    }
    return p;
}

static Table ApiEndpointTable()
{
    var headers = new[] { "Phương thức", "Đường dẫn", "Xác thực", "Mô tả ngắn" };
    var rows = new (string Method, string Path, string Auth, string Note)[]
    {
        ("POST", "/api/auth/login", "Không", "Đăng nhập email/password, trả JWT"),
        ("POST", "/api/auth/refresh", "Không", "Làm mới token (demo)"),
        ("GET", "/api/auth/profile", "Bearer", "Hồ sơ user hiện tại"),
        ("GET", "/api/pois", "Không", "Danh sách POI phân trang + lọc"),
        ("GET", "/api/pois/{id}", "Không", "Chi tiết POI theo id"),
        ("POST", "/api/pois", "Không*", "Tạo POI (*nên khóa admin)"),
        ("PUT", "/api/pois/{id}", "Không*", "Cập nhật POI"),
        ("DELETE", "/api/pois/{id}", "Không*", "Xóa POI"),
        ("GET", "/api/tours", "Không", "Danh sách tour published"),
        ("GET", "/api/tours/{anchorPoiId}", "Không", "Tour route theo POI neo"),
        ("GET", "/api/admin/tours", "Không*", "Danh sách admin (*nên bảo vệ)"),
        ("POST", "/api/admin/tours", "Không*", "Tạo tour"),
        ("PUT", "/api/admin/tours/{id}", "Không*", "Sửa tour"),
        ("DELETE", "/api/admin/tours/{id}", "Không*", "Xóa tour"),
        ("GET", "/api/admin/users", "Không*", "Danh sách user"),
        ("GET", "/api/admin/users/{id}", "Không*", "Chi tiết user"),
        ("GET", "/api/admin/users/roles", "Không*", "Vai trò"),
        ("POST", "/api/admin/users", "Không*", "Tạo user"),
        ("PUT", "/api/admin/users/{id}", "Không*", "Sửa user"),
        ("DELETE", "/api/admin/users/{id}", "Không*", "Xóa user"),
        ("GET", "/health", "Không", "Health check")
    };

    var table = new Table(
        new TableProperties(
            new TableStyle { Val = "TableGrid" },
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" }));

    void AddRow(params string[] cells)
    {
        var tr = new TableRow();
        foreach (var c in cells)
        {
            var cell = new TableCell(
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = "1800" }),
                new Paragraph(new Run(new Text(c) { Space = SpaceProcessingModeValues.Preserve })));
            tr.AppendChild(cell);
        }
        table.AppendChild(tr);
    }

    AddRow(headers);
    foreach (var r in rows)
        AddRow(r.Method, r.Path, r.Auth, r.Note);

    return table;
}

static Table TestCaseTable()
{
    var headers = new[] { "Mã TC", "Nhóm", "Mức", "Tiền điều kiện", "Bước thực hiện", "Kết quả mong đợi" };
    var rows = new (string Id, string Group, string Sev, string Pre, string Steps, string Expected)[]
    {
        ("TC-AUTH-01","Auth","Cao","API đang chạy","POST /api/auth/login với email/password demo hợp lệ","200, có accessToken, userId"),
        ("TC-AUTH-02","Auth","Cao","API đang chạy","POST /api/auth/login sai mật khẩu","401 Unauthorized"),
        ("TC-AUTH-03","Auth","Trung bình","Có access token","GET /api/auth/profile với Authorization Bearer","200, trả id, email, userName"),
        ("TC-AUTH-04","Auth","Trung bình","Không token","GET /api/auth/profile không header","401"),
        ("TC-AUTH-05","Auth","Thấp","Có refresh token bất kỳ","POST /api/auth/refresh","200 (ghi chú: demo, cần kiểm tra logic production)"),
        ("TC-POI-01","POI","Cao","DB có seed","GET /api/pois?pageNumber=1&pageSize=5","200, Items ≤ pageSize, TotalCount ≥ 0"),
        ("TC-POI-02","POI","Cao","Có POI id=1","GET /api/pois/1?lang=vi","200, có title, tọa độ"),
        ("TC-POI-03","POI","Trung bình","—","GET /api/pois/999999","404"),
        ("TC-POI-04","POI","Trung bình","Tọa độ HCM","GET /api/pois?lat=10.77&lng=106.70&radius=5000","Danh sách có DistanceMeters hoặc lọc hợp lý"),
        ("TC-POI-05","POI","Cao","Payload hợp lệ","POST /api/pois tạo POI mới","201 Created, Location header trỏ GET by id"),
        ("TC-POI-06","POI","Trung bình","POI tồn tại","PUT /api/pois/{id}","204 No Content"),
        ("TC-POI-07","POI","Trung bình","POI không tồn tại","DELETE /api/pois/999999","404"),
        ("TC-TOUR-01","Tour","Cao","Seed tour","GET /api/tours","200, danh sách tour published"),
        ("TC-TOUR-02","Tour","Cao","Anchor POI hợp lệ","GET /api/tours/1","200 TourRouteDto, Waypoints > 0"),
        ("TC-TOUR-03","Tour","Trung bình","Anchor không khớp","GET /api/tours/999999","404"),
        ("TC-ADM-01","Admin API","Cao","API chạy","GET /api/admin/tours","200 danh sách admin DTO"),
        ("TC-ADM-02","Admin API","Trung bình","—","POST /api/admin/tours tạo tour (payload hợp lệ)","201 hoặc 400 nếu dữ liệu sai"),
        ("TC-ADM-03","Admin API","Cao","—","GET /api/admin/users","200 danh sách user"),
        ("TC-ADM-04","Admin API","Trung bình","—","POST /api/admin/users trùng email","409 Conflict"),
        ("TC-HEA-01","Hạ tầng","Cao","—","GET /health","200 status OK"),
        ("TC-MOB-01","Mobile","Cao","Thiết bị + API reachable","Mở app, vào Explore","Tải được danh sách/tour"),
        ("TC-MOB-02","Mobile","Cao","Credential demo","Đăng nhập từ LoginPage","Thành công, quay lại màn trước"),
        ("TC-MOB-03","Mobile","Trung bình","Đã bắt đầu tour","Di chuyển mock vị trí vào geofence","Audio phát / waypoint đổi"),
        ("TC-MOB-04","Mobile","Trung bình","Đang phát","Stop tour","Audio dừng, location tracker dừng"),
        ("TC-MOB-05","Mobile","Thấp","Offline DB","Mở thư viện audio offline","Không crash, hiển thị số lượng nếu có"),
        ("TC-MOB-06","Mobile","Cao","Có cache local, mất internet","Mở Explore","Hiển thị dữ liệu cache trước, không chặn UI"),
        ("TC-MOB-07","Mobile","Trung bình","Đang offline rồi có mạng lại","Giữ app mở và chờ sync nền","Danh sách cập nhật mới, không nhân bản dữ liệu"),
        ("TC-MOB-08","Mobile","Cao","Tour đang chạy + vị trí dao động","Di chuyển qua lại mép geofence","Không phát lặp audio liên tục (cooldown hoạt động)"),
        ("TC-MOB-09","Mobile","Trung bình","Đã phát waypoint A","Rời vùng rồi quay lại ngay < cooldown","Không phát lại trước khi hết cooldown"),
        ("TC-MOB-10","Mobile","Trung bình","Offline-first + API lỗi 5xx","Kích hoạt sync nền khi API lỗi","Ứng dụng giữ dữ liệu cache, không crash, ghi log lỗi"),
        ("TC-MOB-11","Mobile","Trung bình","Offline-first + timeout mạng","Trigger đồng bộ với timeout ngắn","Sync fail graceful, thử lại ở lần sync kế tiếp"),
        ("TC-ADMWEB-01","Admin Web","Cao","Cấu hình BaseUrl","Đăng nhập admin cookie đúng","Vào dashboard"),
        ("TC-ADMWEB-02","Admin Web","Cao","Sai password","POST login","Thông báo lỗi tiếng Việt"),
        ("TC-ADMWEB-03","Admin Web","Trung bình","Đã login","Tạo/sửa POI","API phản hồi thành công qua client"),
        ("TC-SEC-01","Bảo mật","Cao","Production config","Không lộ JWT secret trong repo","Secret chỉ ở vault/config"),
        ("TC-SEC-02","Bảo mật","Trung bình","HTTPS","Gọi API production","Redirect/TLS hợp lệ"),
        ("TC-DATA-01","Dữ liệu","Cao","DB trống mới","Khởi động API","Migrate + seed chạy không lỗi"),
        ("TC-DATA-02","Dữ liệu","Trung bình","Legacy DB","Khởi động API","Baseline migration history nếu có bảng POI cũ"),
        ("TC-RUN-01","Runtime Service","Cao","Đã Start TourRoutePlayback","Phát sinh OnLocationUpdated liên tục","Service xử lý event tuần tự, không crash"),
        ("TC-RUN-02","Runtime Service","Trung bình","Đã Stop playback","Giả lập tiếp tục bắn location event","Không còn callback phát audio sau khi stop"),
        ("TC-RUN-03","Runtime Service","Trung bình","Giả lập GPS cập nhật tần suất cao","Bắn 50-100 location events/phút","Không tràn hàng đợi xử lý; app vẫn phản hồi"),
        ("TC-PERF-01","Hiệu năng","Thấp","—","GET /api/pois pageSize=100","Phản hồi chấp nhận được môi trường dev"),
        ("TC-INT-01","Tích hợp","Trung bình","Blob audio URL seed","GET URL audio mp3","HTTP 200 (nếu public)"),
        ("TC-REG-01","Hồi quy","Trung bình","Sau thay đổi DTO","Mobile deserialize TourRouteDto","Không lỗi JSON"),
        ("TC-REG-02","Hồi quy","Trung bình","Sau thay đổi service geofence","Chạy luồng tour đầy đủ từ start->stop","Không vỡ luồng cũ đăng nhập/khám phá/phát audio")
    };

    var table = new Table(
        new TableProperties(
            new TableStyle { Val = "TableGrid" },
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" }));

    void AddRow(params string[] cells)
    {
        var tr = new TableRow();
        foreach (var c in cells)
        {
            var cell = new TableCell(
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = "2000" }),
                new Paragraph(new Run(new Text(c) { Space = SpaceProcessingModeValues.Preserve })));
            tr.AppendChild(cell);
        }
        table.AppendChild(tr);
    }

    AddRow(headers);
    foreach (var r in rows)
        AddRow(r.Id, r.Group, r.Sev, r.Pre, r.Steps, r.Expected);

    return table;
}

static string ResolveOutputPath()
{
    const int maxDepth = 20;
    var depth = 0;
    var current = AppContext.BaseDirectory;
    while (!string.IsNullOrWhiteSpace(current) && depth < maxDepth)
    {
        var docsPath = Path.Combine(current, "docs");
        if (Directory.Exists(docsPath))
            return Path.Combine(docsPath, "TravelApp-PRD-BaoCao.docx");

        var parent = Directory.GetParent(current);
        if (parent is null)
            break;

        current = parent.FullName;
        depth++;
    }

    throw new DirectoryNotFoundException(
        "Cannot find docs directory for PRD output. Không tìm thấy thư mục docs để xuất PRD.");
}
