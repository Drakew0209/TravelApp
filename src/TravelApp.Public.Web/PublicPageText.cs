using System.Globalization;

namespace TravelApp.Public.Web;

public sealed class PublicPageText
{
    public static PublicPageText Current => new();

    private static string T(string vi, string ja, string de, string en = "")
        => PublicText.T(vi, ja, de, en);

    public string HeroBadge => T("Trải nghiệm QR công khai", "公開 QR 体験", "Öffentliche QR-Erfahrung", "Public QR experience");
    public string Profile => T("Hồ sơ & ngôn ngữ", "プロフィールと言語", "Profil & Sprache", "Profile & language");
    public string CurrentLanguage => T("Ngôn ngữ hiện tại", "現在の言語", "Aktuelle Sprache", "Current language");
    public string Session => T("Phiên duyệt", "セッション", "Sitzung", "Session");
    public string BookmarksCount => T("Đã lưu", "保存済み", "Gespeichert", "Saved");
    public string ListeningHistoryCount => T("Lịch sử nghe", "聴取履歴", "Hörverlauf", "Listening history");
    public string Account => T("Tài khoản", "アカウント", "Konto", "Account");
    public string Login => T("Đăng nhập", "ログイン", "Anmelden", "Login");
    public string Register => T("Đăng ký", "登録", "Registrieren", "Register");
    public string Logout => T("Đăng xuất", "ログアウト", "Abmelden", "Logout");
    public string Email => T("Email", "メール", "E-Mail", "Email");
    public string Password => T("Mật khẩu", "パスワード", "Passwort", "Password");
    public string FullName => T("Họ và tên", "氏名", "Vollständiger Name", "Full name");
    public string NoAccountYet => T("Chưa có tài khoản?", "アカウントをお持ちでないですか？", "Noch kein Konto?", "No account yet?");
    public string HaveAccountAlready => T("Đã có tài khoản?", "すでにアカウントをお持ちですか？", "Haben Sie bereits ein Konto?", "Already have an account?");
    public string SignInToSyncBookmarksAndHistory => T("Đăng nhập để đồng bộ bookmarks và lịch sử nghe trên web.", "ログインすると、Web 上のブックマークと視聴履歴を同期できます。", "Melden Sie sich an, um Lesezeichen und Hörverlauf im Web zu synchronisieren.", "Sign in to sync bookmarks and listening history on the web.");
    public string RegisterToKeepASeparateWebAccount => T("Đăng ký để có một tài khoản web public riêng biệt.", "公開 Web 用の独立したアカウントを作成できます。", "Registrieren Sie sich für ein separates Public-Web-Konto.", "Register to keep a separate public web account.");
    public string BookmarksHistoryTitle => T("Bookmarks / History", "ブックマーク / 履歴", "Lesezeichen / Verlauf", "Bookmarks / history");
    public string BookmarksHistoryDescription => T("Lưu POI yêu thích và xem lịch sử nghe giống trên mobile.", "モバイルと同じように POI を保存し、視聴履歴を確認できます。", "Speichern Sie bevorzugte POIs und sehen Sie den Hörverlauf wie auf dem Mobile-Gerät.", "Save favorite POIs and view listening history like on mobile.");
    public string BookmarksTab => T("Bookmarks", "ブックマーク", "Lesezeichen", "Bookmarks");
    public string HistoryTab => T("History", "履歴", "Verlauf", "History");
    public string Open => T("Mở", "開く", "Öffnen", "Open");
    public string ClearHistory => T("Xóa lịch sử", "履歴を消去", "Verlauf löschen", "Clear history");
    public string Remove => T("Xóa", "削除", "Entfernen", "Remove");
    public string Unsave => T("Bỏ lưu", "保存解除", "Aus Liste entfernen", "Unsave");
    public string BookmarkCurrent => T("Lưu hiện tại", "現在を保存", "Aktuell speichern", "Bookmark current");
    public string Bookmarked => T("Đã lưu", "保存済み", "Gespeichert", "Bookmarked");
    public string PoiLabel => T("POI", "POI", "POI", "POI");
    public string TourLabel => T("Tour", "ツアー", "Tour", "Tour");
    public string LoadingPublicContent => T("Đang tải nội dung public...", "公開コンテンツを読み込み中...", "Öffentliche Inhalte werden geladen...", "Loading public content...");
    public string MapTitle => T("Bản đồ tour", "ツアーマップ", "Tourkarte", "Tour map");
    public string MapDescription => T("Chạm marker để mở bottom sheet chi tiết POI.", "マーカーをタップすると POI の詳細シートを開きます。", "Tippen Sie auf einen Marker, um das POI-Detailblatt zu öffnen.", "Tap a marker to open the POI detail sheet.");
    public string ViewList => T("Xem danh sách", "一覧を見る", "Liste ansehen", "View list");
    public string Search => T("Tìm kiếm", "検索", "Suchen", "Search");
    public string ViewRoute => T("Xem tuyến", "ルートを見る", "Route ansehen", "View route");
    public string MapAriaLabel => MapTitle;
    public string PoiListTitle => T("Danh sách POI", "POI 一覧", "POI-Liste", "POI list");
    public string PoiInRoute => T("POI trong tuyến public.", "公開ルート内の POI。", "POIs in der öffentlichen Route.", "POIs in the public route.");
    public string PoiDetailsTitle => T("Chi tiết POI", "POI 詳細", "POI-Details", "POI details");
    public string Close => T("Đóng", "閉じる", "Schließen", "Close");
    public string PlayAudio => T("Phát audio", "オーディオ再生", "Audio abspielen", "Play audio");
    public string OpenRoute => T("Mở route", "ルートを開く", "Route öffnen", "Open route");
    public string Directions => T("Chỉ đường", "ルート案内", "Wegbeschreibung", "Directions");
    public string ScanQrToOpenContent => T("Quét QR để mở nội dung", "QR をスキャンしてコンテンツを開く", "QR scannen, um Inhalte zu öffnen", "Scan QR to open content");
    public string PublicPageReceivesQr => T("Trang public nhận `poiId` hoặc `tourId` từ QR/deep-link, hiển thị nội dung công khai và ghi nhận analytics.", "公開ページは QR/deep-link から `poiId` または `tourId` を受け取り、公開コンテンツを表示して analytics を記録します。", "Die öffentliche Seite empfängt `poiId` oder `tourId` aus QR/Deep-Link, zeigt öffentliche Inhalte an und zeichnet Analytics auf.", "The public page receives `poiId` or `tourId` from QR/deep links, shows public content, and records analytics.");
    public string ListenAudio => T("Nghe audio", "音声を聴く", "Audio anhören", "Listen to audio");
    public string ChooseLanguagePlayAudio => T("Chọn ngôn ngữ phù hợp rồi phát audio. Lịch sử nghe chỉ lưu trên phiên duyệt hiện tại.", "適切な言語を選んで音声を再生します。視聴履歴は現在のブラウジングセッションにのみ保存されます。", "Wählen Sie die passende Sprache und spielen Sie Audio ab. Der Hörverlauf wird nur in der aktuellen Sitzung gespeichert.", "Choose a suitable language and play audio. Listening history is saved only in the current browsing session.");
    public string SpeakInBrowser => T("Đọc bằng trình duyệt", "ブラウザで読み上げ", "Im Browser vorlesen", "Read in browser");
    public string FallbackBadge => T("fallback", "フォールバック", "Fallback", "fallback");
    public string ScannerDevice => T("Thiết bị quét", "スキャン端末", "Scan-Gerät", "Scan device");
    public string ScannerDeviceUnknown => T("Chưa xác định", "未確認", "Unbekannt", "Unknown");
    public string TtsContent => T("Nội dung TTS", "TTS コンテンツ", "TTS-Inhalt", "TTS content");
    public string AutoPlayRouteOff => T("Tự động phát tuyến: Tắt", "自動再生ルート: オフ", "Automatische Routenwiedergabe: Aus", "Auto-play route: Off");
    public string AudioOriginal => T("Audio gốc", "元の音声", "Originalaudio", "Original audio");
    public string NoAudioForSelectedLanguage => T("Hiện chưa có audio cho ngôn ngữ đã chọn.", "選択した言語の音声はまだありません。", "Für die ausgewählte Sprache ist noch kein Audio verfügbar.", "No audio is available yet for the selected language.");
    public string AudioNotReadyForLanguage => T("Audio chưa sẵn sàng cho ngôn ngữ này, đang dùng text-to-speech của trình duyệt.", "この言語の音声はまだ準備できていないため、ブラウザの text-to-speech を使用しています。", "Audio ist für diese Sprache noch nicht verfügbar, daher wird die Browser-Sprachausgabe verwendet.", "Audio isn't ready for this language yet, so the browser text-to-speech is being used.");
    public string TourPublic => T("Tour public", "公開ツアー", "Öffentliche Tour", "Public tour");
    public string TapPoiForAudio => T("Chạm từng POI để mở đúng nội dung và nghe audio tương ứng.", "各 POI をタップすると、対応するコンテンツと音声が開きます。", "Tippen Sie auf jeden POI, um den passenden Inhalt und das zugehörige Audio zu öffnen.", "Tap each POI to open the correct content and matching audio.");
    public string ListeningHistory => T("Lịch sử nghe", "聴取履歴", "Hörverlauf", "Listening history");
    public string ListeningHistoryDescription => T("Lưu theo phiên duyệt hiện tại, cập nhật ngay khi phát audio.", "現在のブラウジングセッションに保存され、音声再生時に即座に更新されます。", "Wird in der aktuellen Sitzung gespeichert und sofort beim Abspielen aktualisiert.", "Saved in the current browsing session and updated immediately when audio plays.");
    public string Notes => T("Ghi chú", "備考", "Hinweis", "Notes");
    public string NotesDescription => T("Analytics scan/view/play được đẩy về API để hiển thị trong dashboard admin.", "スキャン / 閲覧 / 再生の analytics は API に送信され、管理ダッシュボードに表示されます。", "Analytics für Scan/Ansicht/Play werden an die API gesendet und im Admin-Dashboard angezeigt.", "Scan/view/play analytics are sent to the API and shown in the admin dashboard.");
    public string PublicDataOnly => T("Public data only", "公開データのみ", "Nur öffentliche Daten", "Public data only");
    public string MobileFriendly => T("Mobile friendly", "モバイル対応", "Mobilfreundlich", "Mobile friendly");
    public string OfflineCache => T("Offline cache", "オフラインキャッシュ", "Offline-Cache", "Offline cache");

    public IReadOnlyDictionary<string, string> ToI18nDictionary()
    {
        return new Dictionary<string, string>
        {
            ["loadingPublicContent"] = LoadingPublicContent,
            ["scanQrToOpenContent"] = ScanQrToOpenContent,
            ["publicPageReceivedQr"] = PublicPageReceivesQr,
            ["publicDataReady"] = T("Nội dung public đã sẵn sàng.", "公開コンテンツの準備ができました。", "Die öffentlichen Inhalte sind bereit.", "Public content is ready."),
            ["loadingPublicError"] = T("Đang gặp lỗi khi tải nội dung public.", "公開コンテンツの読み込みで問題が発生しています。", "Beim Laden der öffentlichen Inhalte ist ein Fehler aufgetreten.", "There is an error loading public content."),
            ["noPublicContent"] = T("Chưa có nội dung công khai. Hãy quét QR để bắt đầu trải nghiệm.", "公開コンテンツはまだありません。QR をスキャンして開始してください。", "Es sind noch keine öffentlichen Inhalte vorhanden。Bitte scannen Sie einen QR-Code, um zu beginnen。", "No public content yet. Scan a QR code to begin."),
            ["mapUnavailable"] = T("Bản đồ đang tải hoặc không khả dụng trên trình duyệt này.", "地図は読み込み中か、このブラウザでは利用できません。", "Die Karte wird geladen oder ist in diesem Browser nicht verfügbar.", "The map is loading or unavailable in this browser."),
            ["mapLoadFailed"] = T("Không thể tải thư viện bản đồ. Đang dùng chế độ fallback.", "地図ライブラリを読み込めません。フォールバックモードを使用します。", "Die Kartenbibliothek konnte nicht geladen werden。Fallback-Modus wird verwendet。", "Could not load the map library. Using fallback mode."),
            ["noValidPoiOnMap"] = T("Chưa có POI hợp lệ để hiển thị trên bản đồ.", "地図に表示できる有効な POI がありません。", "Keine gültigen POIs für die Kartenanzeige vorhanden。", "No valid POIs to display on the map."),
            ["noAudioForSelectedLanguage"] = NoAudioForSelectedLanguage,
            ["audioNotReadyForLanguage"] = AudioNotReadyForLanguage,
            ["chooseLanguagePlayAudio"] = ChooseLanguagePlayAudio,
            ["speakInBrowser"] = SpeakInBrowser,
            ["audioOriginal"] = AudioOriginal,
            ["ttsContent"] = TtsContent,
            ["autoPlayRouteOff"] = AutoPlayRouteOff,
            ["autoPlayRouteOn"] = T("Tự động phát tuyến: Bật", "自動再生ルート: オン", "Automatische Routenwiedergabe: An", "Auto-play route: On"),
            ["autoPlayRouteEnabled"] = T("Tự động phát tuyến đang bật.", "自動再生ルートが有効です。", "Die automatische Routenwiedergabe ist aktiviert.", "Auto-play route is on."),
            ["autoPlayRouteDisabled"] = T("Tự động phát tuyến đang tắt.", "自動再生ルートが無効です。", "Die automatische Routenwiedergabe ist deaktiviert.", "Auto-play route is off."),
            ["autoPlayRouteFinished"] = T("Đã nghe hết tour.", "ツアーを最後まで再生しました。", "Die Tour wurde vollständig angehört。", "You have finished the tour."),
            ["autoPlayRouteNext"] = T("Đang chuyển sang POI tiếp theo: {0}", "次の POI に移動しています: {0}", "Wechsel zum nächsten POI: {0}", "Moving to the next POI: {0}"),
            ["ttsAutoplayBlocked"] = T("Trình duyệt chưa cho phép phát tự động. Hãy nhấn Đọc bằng trình duyệt.", "ブラウザが自動再生を許可していません。『ブラウザで読み上げ』を押してください。", "Der Browser erlaubt keine automatische Wiedergabe。Bitte auf ‚Im Browser vorlesen‘ klicken。", "The browser hasn't allowed autoplay yet. Please press Read in browser."),
            ["ttsAutoplaying"] = T("Đang phát tự động nội dung TTS.", "TTS コンテンツを自動再生しています。", "TTS-Inhalt wird automatisch abgespielt。", "Auto-playing TTS content."),
            ["historyNone"] = T("Chưa có lịch sử nghe.", "聴取履歴はまだありません。", "Noch kein Hörverlauf。", "No listening history yet."),
            ["playAudio"] = PlayAudio,
            ["openRoute"] = OpenRoute,
            ["directions"] = Directions,
            ["openPoi"] = T("Mở POI", "POI を開く", "POI öffnen", "Open POI"),
            ["poiLabel"] = PoiLabel,
            ["overview"] = T("Tổng quan", "概要", "Übersicht", "Overview"),
            ["search"] = Search,
            ["map"] = T("Bản đồ", "地図", "Karte", "Map"),
            ["listen"] = T("Nghe", "再生", "Anhören", "Listen"),
            ["history"] = T("Lịch sử", "履歴", "Verlauf", "History"),
            ["scanQrExperience"] = HeroBadge,
            ["publicTour"] = TourPublic,
            ["listeningHistory"] = ListeningHistory,
            ["notes"] = Notes,
            ["publicDataOnly"] = PublicDataOnly,
            ["mobileFriendly"] = MobileFriendly,
            ["offlineCache"] = OfflineCache,
            ["profile"] = Profile,
            ["account"] = Account,
            ["login"] = Login,
            ["register"] = Register,
            ["logout"] = Logout,
            ["email"] = Email,
            ["password"] = Password,
            ["fullName"] = FullName,
            ["noAccountYet"] = NoAccountYet,
            ["haveAccountAlready"] = HaveAccountAlready,
            ["signInToSyncBookmarksAndHistory"] = SignInToSyncBookmarksAndHistory,
            ["registerToKeepASeparateWebAccount"] = RegisterToKeepASeparateWebAccount,
            ["bookmarksCount"] = BookmarksCount,
            ["bookmarksHistoryTitle"] = BookmarksHistoryTitle,
            ["bookmarksHistoryDescription"] = BookmarksHistoryDescription,
            ["bookmarksTab"] = BookmarksTab,
            ["historyTab"] = HistoryTab,
            ["open"] = Open,
            ["fallback"] = FallbackBadge,
            ["remove"] = Remove,
            ["unsave"] = Unsave,
            ["bookmarkCurrent"] = BookmarkCurrent,
            ["bookmarked"] = Bookmarked,
            ["noAudioWarning"] = T("Chưa có audio cho ngôn ngữ này.", "この言語の音声はまだありません。", "Für diese Sprache ist noch kein Audio verfügbar。", "No audio for this language yet."),
            ["ttsWarning"] = T("Chưa có audio cho ngôn ngữ này. Đang dùng TTS.", "この言語の音声はまだありません。TTS を使用しています。", "Für diese Sprache ist noch kein Audio verfügbar。TTS wird verwendet。", "No audio for this language yet. Using TTS."),
            ["poiDetails"] = PoiDetailsTitle,
            ["close"] = Close
        };
    }
}
