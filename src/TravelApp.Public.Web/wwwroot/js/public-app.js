(function () {
    const state = window.travelappPublicState || {};
    const sessionKey = 'travelapp-public-session-v1';
    const historyListElement = document.getElementById('history-list');
    const audio = document.getElementById('public-audio');
    const speakButton = document.getElementById('speak-button');
    const mapElement = document.getElementById('tour-map');
    const poiListElement = document.getElementById('poi-list-items');
    const poiSheetElement = document.getElementById('poi-sheet');
    const poiSheetTitle = document.getElementById('poi-sheet-title');
    const poiSheetSubtitle = document.getElementById('poi-sheet-subtitle');
    const poiSheetMeta = document.getElementById('poi-sheet-meta');
    const poiSheetClose = document.getElementById('poi-sheet-close');
    const poiSheetPlayAudio = document.getElementById('sheet-play-audio');
    const poiSheetWarning = document.getElementById('poi-sheet-warning');
    const sheetOpenRoute = document.getElementById('sheet-open-route');
    const sheetDirections = document.getElementById('sheet-directions');
    const routeOpenButton = document.getElementById('route-open-button');
    const autoPlayToggle = document.getElementById('auto-play-toggle');
    const homeSearchInput = document.getElementById('home-search-input');
    const homeSearchSummary = document.getElementById('home-search-summary');
    const homeFeaturedGrid = document.getElementById('home-featured-grid');
    const homeEmptyState = document.getElementById('home-empty-state');
    const publicSessionId = document.getElementById('public-session-id');
    const publicBookmarksCount = document.getElementById('public-bookmarks-count');
    const publicHistoryCount = document.getElementById('public-history-count');
    const bookmarkToggleButton = document.getElementById('bookmark-toggle-button');
    const activityClearButton = document.getElementById('activity-clear-button');
    const bookmarksTabButton = document.getElementById('bookmarks-tab-button');
    const historyTabButton = document.getElementById('history-tab-button');
    const bookmarksTabCount = document.getElementById('bookmarks-tab-count');
    const historyTabCount = document.getElementById('history-tab-count');
    const bookmarksView = document.getElementById('bookmarks-view');
    const historyView = document.getElementById('history-view');
    const bookmarksList = document.getElementById('bookmarks-list');
    const bookmarksEmpty = document.getElementById('bookmarks-empty');
    const historyEmpty = document.getElementById('history-empty');
    const navItems = Array.from(document.querySelectorAll('.public-nav-item'));
    const pageStatusElement = document.getElementById('page-status');
    const pageErrorElement = document.getElementById('page-error');
    const scannerDeviceChip = document.getElementById('scanner-device-chip');

    const autoPlayRouteKey = 'travelapp-public-auto-play-route-v1';
    const bookmarksKey = 'travelapp-public-bookmarks-v1';
    const i18n = window.travelappPublicI18n || {};
    const featuredTours = Array.isArray(window.travelappFeaturedTours) ? window.travelappFeaturedTours : [];

    let map = null;
    let selectedPoint = null;
    let markerByPoiId = new Map();
    let sheetAudioInstance = null;
    let autoPlayRouteEnabled = readJson(autoPlayRouteKey, false) === true;
    let initialTtsAutoPlayed = false;

    function t(key, fallback, ...args) {
        const template = i18n[key] || fallback;
        return String(template).replace(/\{(\d+)\}/g, (_, index) => String(args[Number(index)] ?? ''));
    }

    function pickFirstNonEmpty(obj, keys, fallback = '') {
        if (!obj) return fallback;
        for (const key of keys) {
            const value = obj[key];
            if (typeof value === 'string' && value.trim()) {
                return value.trim();
            }
            if (typeof value === 'number' && Number.isFinite(value)) {
                return String(value);
            }
        }
        return fallback;
    }

    function getTourRouteKey(tour) {
        return tour?.id ?? tour?.Id ?? tour?.tourId ?? tour?.TourId ?? '';
    }

    function buildTourUrl(tour) {
        const tourId = getTourRouteKey(tour);
        if (!tourId) {
            return '/';
        }

        const lang = (state.languageCode || 'vi-VN');
        return `/?tourId=${encodeURIComponent(tourId)}&lang=${encodeURIComponent(lang)}`;
    }

    function normalizeSearch(value) {
        return String(value || '')
            .trim()
            .toLowerCase()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '');
    }

    function renderHomeFeaturedTours(filterText = '') {
        if (!homeFeaturedGrid) {
            return;
        }

        const query = normalizeSearch(filterText);
        const filtered = featuredTours.filter(tour => {
            const searchText = normalizeSearch([
                pickFirstNonEmpty(tour, ['name', 'Name', 'title', 'Title']),
                pickFirstNonEmpty(tour, ['subtitle', 'Subtitle']),
                pickFirstNonEmpty(tour, ['description', 'Description']),
                pickFirstNonEmpty(tour, ['location', 'Location'])
            ].join(' '));

            return !query || searchText.includes(query);
        });

        if (homeSearchSummary) {
            const total = featuredTours.length;
            homeSearchSummary.textContent = query
                ? `${filtered.length}/${total} ${t('search', 'Search')} ${t('publicTour', 'Public tour')}`
                : `${total} ${t('publicTour', 'Public tour')}`;
        }

        homeFeaturedGrid.innerHTML = filtered.length === 0
            ? ''
            : filtered.map(tour => {
                const title = pickFirstNonEmpty(tour, ['name', 'Name', 'title', 'Title'], t('publicTour', 'Public tour'));
                const subtitle = pickFirstNonEmpty(tour, ['subtitle', 'Subtitle', 'description', 'Description'], t('publicDataOnly', 'Public data only'));
                const imageUrl = pickFirstNonEmpty(tour, ['coverImageUrl', 'CoverImageUrl', 'imageUrl', 'ImageUrl'], 'https://placehold.co/1200x800/png?text=TravelApp');
                const location = pickFirstNonEmpty(tour, ['location', 'Location'], t('notesDescription', 'Public data only'));
                const waypoints = tour?.waypoints ?? tour?.Waypoints ?? [];
                const waypointCount = Array.isArray(waypoints) ? waypoints.length : 0;

                return `
                    <a class="featured-tour-card" href="${escapeAttr(buildTourUrl(tour))}">
                        <div class="featured-tour-image">
                            <img src="${escapeAttr(imageUrl)}" alt="${escapeAttr(title)}" loading="lazy" />
                        </div>
                        <div class="featured-tour-body">
                            <div class="featured-tour-head">
                                <h3>${escapeHtml(title)}</h3>
                                <span class="soft-badge">${escapeHtml(String(waypointCount))} ${escapeHtml(t('poiLabel', 'POI'))}</span>
                            </div>
                            <p>${escapeHtml(subtitle)}</p>
                            <div class="featured-tour-meta">
                                <span>${escapeHtml(location)}</span>
                                <span>${escapeHtml(t('openRoute', 'Open route'))}</span>
                            </div>
                        </div>
                    </a>`;
            }).join('');

        if (homeEmptyState) {
            homeEmptyState.hidden = filtered.length > 0;
            homeEmptyState.textContent = query
                ? t('noPublicContent', 'No public content yet. Scan a QR code to begin.')
                : t('publicPageReceivedQr', 'The public page receives `poiId` or `tourId` from QR deep links, shows public content, and records analytics.');
        }
    }

    function setPageStatus(message) {
        if (pageStatusElement) {
            pageStatusElement.textContent = message;
            pageStatusElement.hidden = !message;
        }
    }

    function showPageError(message) {
        if (pageErrorElement) {
            pageErrorElement.textContent = message;
            pageErrorElement.hidden = !message;
        }
    }

    function hideSkeleton() {
        const skeleton = document.getElementById('loading-skeleton');
        if (skeleton) {
            skeleton.hidden = true;
        }
    }

    function setScannerDeviceChip(text, title) {
        if (!scannerDeviceChip) {
            return;
        }

        if (!text) {
            scannerDeviceChip.hidden = true;
            scannerDeviceChip.textContent = '';
            scannerDeviceChip.removeAttribute('title');
            return;
        }

        scannerDeviceChip.textContent = text;
        scannerDeviceChip.hidden = false;
        if (title) {
            scannerDeviceChip.title = title;
        } else {
            scannerDeviceChip.removeAttribute('title');
        }
    }

    function shortId(value) {
        return String(value || '').replace(/-/g, '').slice(0, 6).toUpperCase();
    }

    function getBrowserName() {
        const userAgent = navigator.userAgent || '';
        if (navigator.userAgentData?.brands?.length) {
            const brand = navigator.userAgentData.brands.find(x => x.brand && x.brand !== 'Not_A Brand')?.brand;
            if (brand) {
                return brand;
            }
        }

        if (/EdgA?/i.test(userAgent)) return 'Edge';
        if (/Chrome/i.test(userAgent) && !/Edg/i.test(userAgent)) return 'Chrome';
        if (/Firefox/i.test(userAgent)) return 'Firefox';
        if (/Safari/i.test(userAgent) && !/Chrome/i.test(userAgent)) return 'Safari';
        return '';
    }

    async function resolveScannerDeviceLabel() {
        if (!state.poiId && !state.tourId) {
            setScannerDeviceChip('', '');
            return;
        }

        try {
            const uaData = navigator.userAgentData;
            if (uaData?.getHighEntropyValues) {
                const hints = await uaData.getHighEntropyValues(['model', 'platform', 'platformVersion']);
                const deviceName = hints.model || hints.platform || navigator.platform || '';
                const browserName = getBrowserName();
                const label = [deviceName, browserName ? `• ${browserName}` : ''].filter(Boolean).join(' ').trim();
                const chipText = `${t('scannerDevice', 'Thiết bị quét')}: ${label || t('scannerDeviceUnknown', 'Unknown')}`;
                const chipTitle = [hints.platform, hints.platformVersion ? `v${hints.platformVersion}` : '', browserName].filter(Boolean).join(' · ');
                setScannerDeviceChip(chipText, chipTitle);
                return;
            }

            const legacy = navigator.userAgent || navigator.platform || '';
            const browserName = getBrowserName();
            const chipText = `${t('scannerDevice', 'Thiết bị quét')}: ${legacy ? legacy.split('(')[0].trim() : t('scannerDeviceUnknown', 'Unknown')}${browserName ? ` • ${browserName}` : ''}`;
            setScannerDeviceChip(chipText, legacy);
        } catch {
            setScannerDeviceChip(`${t('scannerDevice', 'Thiết bị quét')}: ${t('scannerDeviceUnknown', 'Unknown')}`);
        }
    }

    function readJson(key, fallback) {
        try {
            const raw = localStorage.getItem(key);
            return raw ? JSON.parse(raw) : fallback;
        } catch {
            return fallback;
        }
    }

    function writeJson(key, value) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
        } catch {
        }
    }

    async function syncActivityFromServer() {
        const lang = encodeURIComponent(state.languageCode || 'vi-VN');
        const bookmarkResponse = await fetch(`/Api/Bookmarks?lang=${lang}`);
        if (bookmarkResponse.ok) {
            const bookmarks = await bookmarkResponse.json();
            writeBookmarks(Array.isArray(bookmarks) ? bookmarks : []);
        }

        const historyResponse = await fetch(`/Api/History?lang=${lang}`);
        if (historyResponse.ok) {
            const history = await historyResponse.json();
            writeJson(getHistoryKey(), Array.isArray(history) ? history : []);
        }
    }

    async function callActivityApi(url, method) {
        try {
            const response = await fetch(url, { method });
            return response.ok;
        } catch {
            return false;
        }
    }

    function getSessionId() {
        let sessionId = sessionStorage.getItem(sessionKey);
        if (!sessionId) {
            sessionId = crypto.randomUUID();
            sessionStorage.setItem(sessionKey, sessionId);
        }
        return sessionId;
    }

    function getDeviceId() {
        const key = 'travelapp-public-device-v1';
        let deviceId = localStorage.getItem(key);
        if (!deviceId) {
            deviceId = crypto.randomUUID();
            localStorage.setItem(key, deviceId);
        }
        return deviceId;
    }

    function getGuestId() {
        const key = 'travelapp-public-guest-v1';
        let guestId = sessionStorage.getItem(key);
        if (!guestId) {
            guestId = crypto.randomUUID();
            sessionStorage.setItem(key, guestId);
        }
        return guestId;
    }

    function getHistoryKey() {
        return 'travelapp-public-history-v1';
    }

    function readBookmarks() {
        return readJson(bookmarksKey, []);
    }

    function writeBookmarks(items) {
        writeJson(bookmarksKey, items);
    }

    function getBookmarkLink(item) {
        const poiId = item?.poiId || null;
        const tourId = item?.tourId || null;
        const lang = item?.languageCode || state.languageCode || 'vi-VN';

        if (poiId) {
            return `/?poiId=${encodeURIComponent(poiId)}&lang=${encodeURIComponent(lang)}`;
        }

        if (tourId) {
            return `/?tourId=${encodeURIComponent(tourId)}&lang=${encodeURIComponent(lang)}`;
        }

        return '/';
    }

    function getCurrentBookmarkTarget(point = null) {
        const selected = point || selectedPoint || getPointByPoiId(state.poiId);
        if (selected) {
            return {
                poiId: selected.poiId,
                tourId: state.tourId || null,
                title: selected.title || state.title || t('poiLabel', 'POI'),
                subtitle: selected.subtitle || state.subtitle || '',
                location: state.location || selected.location || '',
                imageUrl: state.imageUrl || '',
                languageCode: state.languageCode || 'vi-VN',
                link: selected.link || getBookmarkLink({ poiId: selected.poiId, tourId: state.tourId, languageCode: state.languageCode })
            };
        }

        if (state.poiId || state.tourId) {
            return {
                poiId: state.poiId || null,
                tourId: state.tourId || null,
                title: state.title || t('poiLabel', 'POI'),
                subtitle: state.subtitle || '',
                location: state.location || '',
                imageUrl: state.imageUrl || '',
                languageCode: state.languageCode || 'vi-VN',
                link: getBookmarkLink(state)
            };
        }

        return null;
    }

    function isBookmarked(poiId) {
        return readBookmarks().some(item => Number(item.poiId) === Number(poiId));
    }

    function toggleCurrentBookmark(point = null) {
        const target = getCurrentBookmarkTarget(point);
        if (!target?.poiId) {
            return;
        }

        const bookmarks = readBookmarks();
        const index = bookmarks.findIndex(item => Number(item.poiId) === Number(target.poiId));
        if (index >= 0) {
            bookmarks.splice(index, 1);
        } else {
            bookmarks.unshift({
                poiId: target.poiId,
                tourId: target.tourId || null,
                title: target.title,
                subtitle: target.subtitle,
                location: target.location,
                imageUrl: target.imageUrl,
                languageCode: target.languageCode,
                savedAtUtc: new Date().toISOString(),
                link: target.link
            });
        }

        writeBookmarks(bookmarks.slice(0, 100));
        renderActivityPanel();
        syncBookmarkButtonState();
        const remoteUrl = index >= 0
            ? `/Api/Bookmarks?poiId=${encodeURIComponent(String(target.poiId))}`
            : `/Api/Bookmarks?poiId=${encodeURIComponent(String(target.poiId))}`;
        void callActivityApi(remoteUrl, index >= 0 ? 'DELETE' : 'POST');
    }

    async function track(eventType, metadata) {
        const payload = {
            eventType,
            source: 2,
            userId: null,
            guestId: getGuestId(),
            deviceId: getDeviceId(),
            sessionId: getSessionId(),
            poiId: state.poiId || null,
            tourId: state.tourId || null,
            metadataJson: metadata ? JSON.stringify(metadata) : null,
            occurredAtUtc: new Date().toISOString()
        };

        try {
            await fetch('/Api/Track', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
        } catch {
        }
    }

    function addHistoryItem(item) {
        const history = readJson(getHistoryKey(), []);
        const next = [item, ...history.filter(x => !(x.poiId === item.poiId && x.languageCode === item.languageCode))].slice(0, 12);
        writeJson(getHistoryKey(), next);
        renderActivityPanel();
        if (item?.poiId != null) {
            void callActivityApi(`/Api/History?poiId=${encodeURIComponent(String(item.poiId))}`, 'POST');
        }
    }

    function clearHistory() {
        try {
            localStorage.removeItem(getHistoryKey());
        } catch {
        }

        renderActivityPanel();
        void callActivityApi('/Api/History?handler=All', 'DELETE');
    }

    function removeBookmark(poiId) {
        const bookmarks = readBookmarks().filter(item => Number(item.poiId) !== Number(poiId));
        writeBookmarks(bookmarks);
        renderActivityPanel();
        void callActivityApi(`/Api/Bookmarks?poiId=${encodeURIComponent(String(poiId))}`, 'DELETE');
    }

    function getBookmarks() {
        return readBookmarks();
    }

    function renderBookmarks() {
        const bookmarks = getBookmarks();
        if (publicBookmarksCount) {
            publicBookmarksCount.textContent = String(bookmarks.length);
        }

        if (bookmarksTabCount) {
            bookmarksTabCount.textContent = String(bookmarks.length);
        }

        if (!bookmarksList) {
            return;
        }

        bookmarksList.innerHTML = bookmarks.length === 0
            ? ''
            : bookmarks.map(item => `
                <article class="activity-item">
                    <div class="activity-thumb"><img src="${escapeAttr(item.imageUrl || 'https://placehold.co/160x160/png?text=POI')}" alt="${escapeAttr(item.title || t('poiLabel', 'POI'))}" loading="lazy" /></div>
                    <div class="activity-body">
                        <div class="activity-title-row">
                            <strong>${escapeHtml(item.title || t('poiLabel', 'POI'))}</strong>
                            <span class="poi-pill">${escapeHtml((item.languageCode || '').toUpperCase())}</span>
                        </div>
                        <div class="activity-subtitle">${escapeHtml(item.subtitle || item.location || '')}</div>
                        <div class="activity-meta">${escapeHtml(new Date(item.savedAtUtc).toLocaleString())}</div>
                    </div>
                    <div class="activity-actions">
                        <a class="map-action map-action-primary" href="${escapeAttr(item.link || getBookmarkLink(item))}">${escapeHtml(t('open', 'Open'))}</a>
                        <button type="button" class="map-action" data-remove-bookmark="${escapeAttr(String(item.poiId))}">${escapeHtml(t('unsave', 'Unsave'))}</button>
                    </div>
                </article>`).join('');

        bookmarksList.querySelectorAll('[data-remove-bookmark]').forEach(button => {
            button.addEventListener('click', () => removeBookmark(Number(button.getAttribute('data-remove-bookmark'))));
        });

        if (bookmarksEmpty) {
            bookmarksEmpty.hidden = bookmarks.length > 0;
        }
    }

    function renderHistory() {
        const history = readJson(getHistoryKey(), []);
        if (publicHistoryCount) {
            publicHistoryCount.textContent = String(history.length);
        }

        if (historyTabCount) {
            historyTabCount.textContent = String(history.length);
        }

        if (publicSessionId) {
            publicSessionId.textContent = shortId(getSessionId());
        }

        if (!historyListElement) {
            return;
        }

        historyListElement.innerHTML = history.length === 0
            ? ''
            : history.map(item => `
                <article class="activity-item">
                    <div class="activity-thumb"><img src="${escapeAttr(item.imageUrl || 'https://placehold.co/160x160/png?text=POI')}" alt="${escapeAttr(item.title || t('poiLabel', 'POI'))}" loading="lazy" /></div>
                    <div class="activity-body">
                        <div class="activity-title-row">
                            <strong>${escapeHtml(item.title || t('poiLabel', 'POI'))}</strong>
                            <span class="poi-pill">${escapeHtml((item.languageCode || '').toUpperCase())}</span>
                        </div>
                        <div class="activity-subtitle">${escapeHtml(item.subtitle || item.location || '')}</div>
                        <div class="activity-meta">${escapeHtml(new Date(item.playedAtUtc).toLocaleString())}</div>
                    </div>
                    <div class="activity-actions">
                        <a class="map-action map-action-primary" href="${escapeAttr(getBookmarkLink(item))}">${escapeHtml(t('open', 'Open'))}</a>
                        <button type="button" class="map-action" data-remove-history="${escapeAttr(String(item.poiId || ''))}">${escapeHtml(t('remove', 'Remove'))}</button>
                    </div>
                </article>`).join('');

        historyListElement.querySelectorAll('[data-remove-history]').forEach(button => {
            button.addEventListener('click', () => {
                const poiId = Number(button.getAttribute('data-remove-history'));
                if (!Number.isFinite(poiId)) {
                    return;
                }

                const history = readJson(getHistoryKey(), []).filter(item => Number(item.poiId) !== poiId);
                writeJson(getHistoryKey(), history);
                renderActivityPanel('history');
                void callActivityApi(`/Api/History?poiId=${encodeURIComponent(String(poiId))}`, 'DELETE');
            });
        });

        if (historyEmpty) {
            historyEmpty.hidden = history.length > 0;
        }
    }

    function setActivityTab(tab) {
        const activeTab = tab === 'history' ? 'history' : 'bookmarks';
        writeJson('travelapp-public-activity-tab-v1', activeTab);

        if (bookmarksTabButton) {
            bookmarksTabButton.classList.toggle('active', activeTab === 'bookmarks');
            bookmarksTabButton.setAttribute('aria-selected', String(activeTab === 'bookmarks'));
        }

        if (historyTabButton) {
            historyTabButton.classList.toggle('active', activeTab === 'history');
            historyTabButton.setAttribute('aria-selected', String(activeTab === 'history'));
        }

        if (bookmarksView) bookmarksView.hidden = activeTab !== 'bookmarks';
        if (historyView) historyView.hidden = activeTab !== 'history';
    }

    function renderActivityPanel(tab = null) {
        renderBookmarks();
        renderHistory();
        syncBookmarkButtonState();
        setActivityTab(tab || readJson('travelapp-public-activity-tab-v1', 'bookmarks'));
    }

    function syncBookmarkButtonState() {
        if (!bookmarkToggleButton) {
            return;
        }

        const target = getCurrentBookmarkTarget();
        if (!target?.poiId) {
            bookmarkToggleButton.hidden = true;
            return;
        }

        bookmarkToggleButton.hidden = false;
        const bookmarked = isBookmarked(target.poiId);
        bookmarkToggleButton.textContent = bookmarked ? t('bookmarked', 'Bookmarked') : t('bookmarkCurrent', 'Bookmark current');
        bookmarkToggleButton.classList.toggle('map-action-primary', !bookmarked);
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function escapeAttr(value) {
        return escapeHtml(value).replaceAll('`', '&#96;');
    }

    function getSpeechVoices() {
        if (!('speechSynthesis' in window) || typeof window.speechSynthesis.getVoices !== 'function') {
            return [];
        }

        return window.speechSynthesis.getVoices().filter(Boolean);
    }

    function normalizeLanguageCode(languageCode) {
        return String(languageCode || 'vi').trim().toLowerCase();
    }

    function setAutoPlayRouteEnabled(enabled) {
        autoPlayRouteEnabled = Boolean(enabled);
        writeJson(autoPlayRouteKey, autoPlayRouteEnabled);

        if (autoPlayToggle) {
            autoPlayToggle.setAttribute('aria-pressed', String(autoPlayRouteEnabled));
            autoPlayToggle.textContent = autoPlayRouteEnabled ? t('autoPlayRouteOn', 'Auto-play route: On') : t('autoPlayRouteOff', 'Auto-play route: Off');
        }

        setPageStatus(autoPlayRouteEnabled ? t('autoPlayRouteEnabled', 'Auto-play route is on.') : t('autoPlayRouteDisabled', 'Auto-play route is off.'));
    }

    function toggleAutoPlayRoute() {
        setAutoPlayRouteEnabled(!autoPlayRouteEnabled);
    }

    function getOrderedPoints() {
        return Array.isArray(state.mapPoints)
            ? state.mapPoints.slice().sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
            : [];
    }

    function getPointByPoiId(poiId) {
        if (poiId == null) {
            return null;
        }

        return getOrderedPoints().find(point => point.poiId === poiId) || null;
    }

    function getNextPoint(point) {
        if (!point) {
            return null;
        }

        const points = getOrderedPoints();
        const index = points.findIndex(x => x.poiId === point.poiId);
        if (index < 0 || index + 1 >= points.length) {
            return null;
        }

        return points[index + 1];
    }

    function syncStateWithPoint(point) {
        if (!point) {
            return;
        }

        state.poiId = point.poiId;
        state.title = point.title || state.title;
        state.speechText = point.speechText || state.speechText;
        syncBookmarkButtonState();
    }

    function autoAdvanceFromPoint(point) {
        if (!state.hasTour || !autoPlayRouteEnabled) {
            return;
        }

        const nextPoint = getNextPoint(point);
        if (!nextPoint) {
            setPageStatus(t('autoPlayRouteFinished', 'You have finished the tour.'));
            return;
        }

        syncStateWithPoint(nextPoint);
        setPageStatus(t('autoPlayRouteNext', 'Moving to the next POI: {0}', nextPoint.title || 'POI'));
        setSelectedPoint(nextPoint, false);
        window.setTimeout(() => playSheetAudio(nextPoint, true), 150);
    }

    function pickSpeechVoice(languageCode) {
        const voices = getSpeechVoices();
        if (voices.length === 0) {
            return null;
        }

        const normalized = normalizeLanguageCode(languageCode);
        const candidates = [
            normalized,
            normalized.replace('_', '-'),
            normalized.split('-')[0],
            'vi',
            'vi-vn'
        ].filter(Boolean);

        const byExactLanguage = candidates
            .flatMap(code => voices.filter(voice => normalizeLanguageCode(voice.lang) === code))
            .find(Boolean);

        if (byExactLanguage) {
            return byExactLanguage;
        }

        const byPrefixLanguage = candidates
            .flatMap(code => voices.filter(voice => normalizeLanguageCode(voice.lang).startsWith(code)))
            .find(Boolean);

        if (byPrefixLanguage) {
            return byPrefixLanguage;
        }

        const vietnameseVoice = voices.find(voice =>
            normalizeLanguageCode(voice.lang).startsWith('vi') ||
            /vietnam/i.test(voice.name));

        if (vietnameseVoice) {
            return vietnameseVoice;
        }

        return voices[0] || null;
    }

    function speakText(text, languageCode, onEnded) {
        if (!text || !('speechSynthesis' in window)) {
            return false;
        }

        const utterance = new SpeechSynthesisUtterance(text);
        const normalizedLanguage = normalizeLanguageCode(languageCode);
        const voice = pickSpeechVoice(normalizedLanguage);

        utterance.lang = voice?.lang || (normalizedLanguage.startsWith('vi') ? 'vi-VN' : normalizedLanguage || 'vi-VN');
        if (voice) {
            utterance.voice = voice;
        }

        utterance.rate = 1;
        utterance.pitch = 1;
        utterance.volume = 1;
        if (typeof onEnded === 'function') {
            utterance.onend = onEnded;
            utterance.onerror = onEnded;
        }

        speechSynthesis.cancel();
        speechSynthesis.speak(utterance);
        return true;
    }

    function getAudioWarningMessage(point) {
        if (!point || point.audioUrl) {
            return '';
        }

        return point.speechText
            ? t('ttsWarning', 'No audio for this language yet. Using TTS.')
            : t('noAudioWarning', 'No audio for this language yet.');
    }

    function speakFallback(onEnded) {
        speakText(state.speechText, state.languageCode, onEnded);
    }

    function tryAutoPlayInitialTts() {
        if (initialTtsAutoPlayed || !state.speechText || (!state.poiId && !state.tourId)) {
            return;
        }

        if (!speakText(state.speechText, state.languageCode, () => autoAdvanceFromPoint(getPointByPoiId(state.poiId)))) {
            setPageStatus(t('ttsAutoplayBlocked', "The browser hasn't allowed autoplay yet. Please press Read in browser."));
            return;
        }

        initialTtsAutoPlayed = true;
        setPageStatus(t('ttsAutoplaying', 'Auto-playing TTS content.'));
        void track(state.hasTour ? 4 : 3, { action: 'tts-autoplay' });
        addHistoryItem({
            poiId: state.poiId,
            tourId: state.tourId,
            title: state.title,
            subtitle: state.subtitle,
            location: state.location,
            imageUrl: state.imageUrl,
            languageCode: state.languageCode,
            playedAtUtc: new Date().toISOString()
        });
    }

    function playSheetAudio(point, autoAdvance = false) {
        if (!point) {
            return;
        }

        syncStateWithPoint(point);

        if (point.speechText) {
            speakText(point.speechText, state.languageCode, () => autoAdvanceFromPoint(point));
            return;
        }

        if (sheetAudioInstance) {
            try {
                sheetAudioInstance.pause();
                sheetAudioInstance.src = '';
            } catch {
            }
        }

        if (point.audioUrl) {
            sheetAudioInstance = new Audio(point.audioUrl);
            sheetAudioInstance.preload = 'metadata';
            sheetAudioInstance.onended = () => autoAdvanceFromPoint(point);
            sheetAudioInstance.play().catch(() => {
                if (point.speechText) {
                    speakText(point.speechText, state.languageCode, () => autoAdvanceFromPoint(point));
                }
            });
            return;
        }

        // No speech text and no audio URL.
    }

    function renderMap() {
        const points = Array.isArray(state.mapPoints) ? state.mapPoints : [];
        if (!mapElement || points.length === 0) {
            if (points.length === 0) {
                setPageStatus(t('noPublicContent', 'No public content yet. Scan a QR code to begin.'));
            }
            return;
        }

        if (!window.L) {
            mapElement.innerHTML = `<div class="map-fallback">${escapeHtml(t('mapUnavailable', 'The map is loading or unavailable in this browser.'))}</div>`;
            showPageError(t('mapLoadFailed', 'Could not load the map library. Using fallback mode.'));
            return;
        }

        map = L.map(mapElement, {
            scrollWheelZoom: false,
            zoomControl: true,
            preferCanvas: true
        });

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        const bounds = [];
        const activePoiId = state.poiId;
        markerByPoiId = new Map();

        points
            .slice()
            .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
            .forEach(point => {
                const isActive = point.isActive || point.poiId === activePoiId;
                const icon = L.divIcon({
                    className: 'travelapp-map-marker-shell',
                    html: `<div class="travelapp-map-marker ${isActive ? 'active' : ''}">${escapeHtml(String(point.sortOrder || '•'))}</div>`,
                    iconSize: [34, 34],
                    iconAnchor: [17, 17],
                    popupAnchor: [0, -14]
                });

                const marker = L.marker([point.latitude, point.longitude], { icon }).addTo(map);
                markerByPoiId.set(point.poiId, marker);
                const popupHtml = `
                    <div class="travelapp-map-popup">
                        <strong>${escapeHtml(point.title || t('poiLabel', 'POI'))}</strong>
                        <div class="popup-subtitle">${escapeHtml(point.subtitle || '')}</div>
                        <a class="popup-link" href="${escapeAttr(point.link)}">${escapeHtml(t('openPoi', 'Open POI'))}</a>
                    </div>`;

                marker.bindPopup(popupHtml, { maxWidth: 220 });
                marker.on('click', () => {
                    track(1, { action: 'map-marker-tap', poiId: point.poiId });
                    setSelectedPoint(point);
                    marker.openPopup();
                });

                bounds.push([point.latitude, point.longitude]);
            });

        if (bounds.length > 1) {
            L.polyline(bounds, { color: '#d31963', weight: 4, opacity: 0.8 }).addTo(map);
            map.fitBounds(bounds, { padding: [30, 30] });
        } else {
            map.setView(bounds[0], 15);
        }

        setTimeout(() => map.invalidateSize(), 0);

        const initialPoint = points.find(point => point.poiId === activePoiId) || points[0];
        if (initialPoint) {
            setSelectedPoint(initialPoint, false);
        }

        renderPoiList(points);
        setPageStatus(t('publicDataReady', 'Public content is ready.'));
    }

    function renderPoiList(points) {
        if (!poiListElement) {
            return;
        }

        poiListElement.innerHTML = points.length === 0
            ? `<div class="map-fallback">${escapeHtml(t('noValidPoiOnMap', 'No valid POIs to display on the map.'))}</div>`
            : points
                .slice()
                .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0))
                .map(point => `
                    <button type="button" class="poi-list-item ${selectedPoint && selectedPoint.poiId === point.poiId ? 'active' : ''}" data-poi-id="${point.poiId}">
                        <div class="poi-list-top">
                            <div class="poi-list-title">${escapeHtml(point.title || t('poiLabel', 'POI'))}</div>
                            <span class="poi-pill">#${escapeHtml(String(point.sortOrder || '•'))}</span>
                        </div>
                        <div class="poi-list-subtitle">${escapeHtml(point.subtitle || '')}</div>
                        <div class="poi-list-meta">
                            <span class="poi-pill">${escapeHtml(point.latitude.toFixed(4))}, ${escapeHtml(point.longitude.toFixed(4))}</span>
                            <span class="poi-pill">${escapeHtml(t('openPoi', 'Open POI'))}</span>
                        </div>
                    </button>`)
                .join('');

        poiListElement.querySelectorAll('[data-poi-id]').forEach(button => {
            button.addEventListener('click', () => {
                const poiId = Number(button.getAttribute('data-poi-id'));
                const point = points.find(x => x.poiId === poiId);
                if (point) {
                    setSelectedPoint(point);
                    const marker = markerByPoiId.get(poiId);
                    marker?.openPopup();
                    track(1, { action: 'poi-list-tap', poiId });
                }
            });
        });
    }

    if (autoPlayToggle) {
        autoPlayToggle.addEventListener('click', toggleAutoPlayRoute);
    }

    if (bookmarkToggleButton) {
        bookmarkToggleButton.addEventListener('click', () => {
            toggleCurrentBookmark();
        });
    }

    if (activityClearButton) {
        activityClearButton.addEventListener('click', () => {
            clearHistory();
            setPageStatus(t('publicDataReady', 'Public content is ready.'));
        });
    }

    if (bookmarksTabButton) {
        bookmarksTabButton.addEventListener('click', () => setActivityTab('bookmarks'));
    }

    if (historyTabButton) {
        historyTabButton.addEventListener('click', () => setActivityTab('history'));
    }

    function setSelectedPoint(point, shouldTrack = true) {
        selectedPoint = point;
        syncStateWithPoint(point);
        if (poiSheetElement) {
            poiSheetElement.classList.add('is-open');
            poiSheetElement.setAttribute('aria-hidden', 'false');
        }

        if (poiSheetTitle) {
            poiSheetTitle.textContent = point.title || t('poiLabel', 'POI');
        }

        if (poiSheetSubtitle) {
            poiSheetSubtitle.textContent = point.subtitle || '';
        }

        if (poiSheetMeta) {
            poiSheetMeta.innerHTML = `
                <span class="poi-pill">${escapeHtml(t('poiLabel', 'POI'))} #${escapeHtml(String(point.poiId))}</span>
                <span class="poi-pill">${escapeHtml(point.latitude.toFixed(5))}, ${escapeHtml(point.longitude.toFixed(5))}</span>
                <span class="poi-pill">${escapeHtml(state.languageCode || 'vi').toUpperCase()}</span>`;
        }

        if (poiSheetPlayAudio) {
            const canPlayAudio = Boolean(point.audioUrl || point.speechText);
            poiSheetPlayAudio.style.display = canPlayAudio ? 'inline-flex' : 'none';
            poiSheetPlayAudio.textContent = point.audioUrl ? t('playAudio', 'Play audio') : t('speakInBrowser', 'Read in browser');
            poiSheetPlayAudio.onclick = async () => {
                await track(state.hasTour ? 4 : 3, { action: point.audioUrl ? 'sheet-audio-play' : 'sheet-tts-play', poiId: point.poiId });
                playSheetAudio(point);
            };
        }

        if (poiSheetWarning) {
            const warning = getAudioWarningMessage(point);
            poiSheetWarning.textContent = warning;
            poiSheetWarning.hidden = !warning;
        }

        if (sheetOpenRoute) {
            sheetOpenRoute.onclick = () => {
                window.location.href = point.link;
            };
        }

        if (sheetDirections) {
            sheetDirections.onclick = () => {
                window.open(buildDirectionsUrl(point), '_blank', 'noopener,noreferrer');
            };
        }

        if (routeOpenButton) {
            routeOpenButton.onclick = () => {
                window.location.href = point.link;
            };
        }

        if (shouldTrack) {
            track(1, { action: 'poi-sheet-open', poiId: point.poiId });
        }

        refreshPoiListSelection();
    }

    function refreshPoiListSelection() {
        if (!poiListElement || !selectedPoint) {
            return;
        }

        poiListElement.querySelectorAll('.poi-list-item').forEach(item => {
            const poiId = Number(item.getAttribute('data-poi-id'));
            item.classList.toggle('active', poiId === selectedPoint.poiId);
        });
    }

    function buildDirectionsUrl(point) {
        const destination = `${point.latitude},${point.longitude}`;
        return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}`;
    }

    function setActiveNavItem(targetId) {
        if (!targetId || navItems.length === 0) {
            return;
        }

        navItems.forEach(item => {
            const href = item.getAttribute('href') || '';
            const isActive = href === `#${targetId}`;
            item.classList.toggle('active', isActive);
            item.setAttribute('aria-current', isActive ? 'page' : 'false');
        });
    }

    function initializeSectionNavigation() {
        if (navItems.length === 0) {
            return;
        }

        const sectionIds = ['overview-panel', 'search-panel', 'profile-panel', 'activity-panel', 'featured-panel', 'tour-map-panel', 'audio-panel', 'history-panel'];
        const sections = sectionIds
            .map(id => document.getElementById(id))
            .filter(Boolean);

        if (sections.length === 0) {
            return;
        }

        const observer = 'IntersectionObserver' in window
            ? new IntersectionObserver(entries => {
                const visible = entries
                    .filter(entry => entry.isIntersecting)
                    .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];

                if (visible?.target?.id) {
                    setActiveNavItem(visible.target.id);
                }
            }, { threshold: [0.25, 0.4, 0.6], rootMargin: '-10% 0px -55% 0px' })
            : null;

        sections.forEach(section => {
            section.style.scrollMarginTop = '16px';
            observer?.observe(section);
        });

        if (!observer) {
            const updateFromScroll = () => {
                const current = sections.find(section => {
                    const rect = section.getBoundingClientRect();
                    return rect.top <= window.innerHeight * 0.35 && rect.bottom >= window.innerHeight * 0.25;
                });

                if (current?.id) {
                    setActiveNavItem(current.id);
                }
            };

            window.addEventListener('scroll', updateFromScroll, { passive: true });
            updateFromScroll();
            return;
        }

        const initialSection = sections.find(section => {
            const rect = section.getBoundingClientRect();
            return rect.top >= 0 && rect.top < window.innerHeight * 0.45;
        }) || sections[0];

        if (initialSection?.id) {
            setActiveNavItem(initialSection.id);
        }
    }

    function closePoiSheet() {
        if (!poiSheetElement) {
            return;
        }

        poiSheetElement.classList.remove('is-open');
        poiSheetElement.setAttribute('aria-hidden', 'true');
    }

    if (state.poiId || state.tourId) {
        track(5, { entry: 'qr-open' });
        if (state.poiId) {
            track(1, { entry: 'view-poi' });
        }
        if (state.tourId) {
            track(2, { entry: 'view-tour' });
        }
    }

    if (!state.poiId && !state.tourId) {
        setPageStatus(t('noPublicContent', 'No public content yet. Scan a QR code to begin.'));
    }

    if (audio) {
        audio.addEventListener('play', async () => {
            await track(state.hasTour ? 4 : 3, { action: 'audio-play' });
            addHistoryItem({
                poiId: state.poiId,
                tourId: state.tourId,
                title: state.title,
                subtitle: state.subtitle,
                location: state.location,
                imageUrl: state.imageUrl,
                languageCode: state.languageCode,
                playedAtUtc: new Date().toISOString()
            });
        });
    }

    if (speakButton) {
        speakButton.addEventListener('click', async () => {
            speakFallback(() => autoAdvanceFromPoint(getPointByPoiId(state.poiId)));
            void track(state.hasTour ? 4 : 3, { action: 'tts-play' });
            addHistoryItem({
                poiId: state.poiId,
                tourId: state.tourId,
                title: state.title,
                subtitle: state.subtitle,
                location: state.location,
                imageUrl: state.imageUrl,
                languageCode: state.languageCode,
                playedAtUtc: new Date().toISOString()
            });
        });
    }

    if (poiSheetClose) {
        poiSheetClose.addEventListener('click', closePoiSheet);
    }

    try {
        void resolveScannerDeviceLabel();
        setAutoPlayRouteEnabled(autoPlayRouteEnabled);
        renderMap();
        renderActivityPanel();
        renderHomeFeaturedTours(homeSearchInput?.value ?? '');
        if (publicSessionId) {
            publicSessionId.textContent = shortId(getSessionId());
        }
        if (homeSearchInput) {
            homeSearchInput.addEventListener('input', () => renderHomeFeaturedTours(homeSearchInput.value));
        }
        initializeSectionNavigation();
        if (state.poiId || state.tourId) {
            setPageStatus(t('publicDataReady', 'Public content is ready.'));
        }
        void syncActivityFromServer().finally(() => renderActivityPanel());
        window.setTimeout(tryAutoPlayInitialTts, 250);
        hideSkeleton();
    } catch {
        showPageError(t('loadingPublicError', 'There is an error loading public content.'));
        setPageStatus(t('loadingPublicError', 'There is an error loading public content.'));
        hideSkeleton();
    }
})();
