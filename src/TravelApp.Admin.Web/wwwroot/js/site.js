document.addEventListener('click', (event) => {
    const button = event.target.closest('[data-add-row]');
    if (!button) {
        const sidebarToggle = event.target.closest('[data-sidebar-toggle]');
        if (!sidebarToggle) {
            return;
        }

        document.body.classList.toggle('sidebar-open');
        return;
    }

    const targetId = button.getAttribute('data-target');
    const prefix = button.getAttribute('data-prefix');
    if (!targetId || !prefix) {
        return;
    }

    const container = document.getElementById(targetId);
    if (!container) {
        return;
    }

    const rows = container.querySelectorAll('[data-repeat-row]');
    if (rows.length === 0) {
        return;
    }

    const nextIndex = Number.parseInt(container.getAttribute('data-next-index') ?? `${rows.length}`, 10);
    const row = rows[0].cloneNode(true);

    row.querySelectorAll('input, select, textarea').forEach((element) => {
        if (element instanceof HTMLSelectElement) {
            element.selectedIndex = 0;
        } else if (element instanceof HTMLInputElement) {
            if (element.type === 'checkbox' || element.type === 'radio') {
                element.checked = false;
            } else {
                element.value = '';
            }
        } else {
            element.value = '';
        }
    });

    row.innerHTML = row.innerHTML.replace(new RegExp(`${prefix}\\[\\d+\\]`, 'g'), `${prefix}[${nextIndex}]`);
    row.querySelectorAll('input, select, textarea').forEach((element) => {
        const name = element.getAttribute('name');
        if (name) {
            element.setAttribute('name', name.replace(new RegExp(`${prefix}\\[\\d+\\]`, 'g'), `${prefix}[${nextIndex}]`));
        }

        const id = element.getAttribute('id');
        if (id) {
            element.setAttribute('id', id.replace(new RegExp(`${prefix}_(\\d+)__`, 'g'), `${prefix}_${nextIndex}__`));
        }
    });

    container.appendChild(row);
    container.setAttribute('data-next-index', `${nextIndex + 1}`);
});

document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
        document.body.classList.remove('sidebar-open');
    }
});

const updateLivePreview = (root) => {
    if (!root) {
        return;
    }

    const bindings = [
        { selector: '[data-live-preview="title"]', fallback: 'Tour Preview' },
        { selector: '[data-live-preview="subtitle"]', fallback: 'Nhập thông tin để xem preview' },
        { selector: '[data-live-preview="location"]', fallback: '—' },
        { selector: '[data-live-preview="category"]', fallback: '—' },
        { selector: '[data-live-preview="language"]', fallback: '—' },
        { selector: '[data-live-preview="anchor-poi"]', fallback: '—' },
        { selector: '[data-live-preview="route-assets"]', fallback: '—' },
        { selector: '[data-live-preview="latlng"]', fallback: '—' },
        { selector: '[data-live-preview="image"]', fallback: 'https://placehold.co/800x600/png?text=Tour+Preview', isImage: true },
        { selector: '[data-live-preview="published-badge"]', fallback: 'Draft' },
        { selector: '[data-live-preview="radius"]', fallback: '0 m' },
        { selector: '[data-live-preview="speech-lang"]', fallback: '—' },
        { selector: '[data-live-preview="speech-text"]', fallback: 'Nội dung speech sẽ hiển thị ở đây' }
    ];

    bindings.forEach((binding) => {
        const target = root.querySelector(binding.selector);
        if (!target) {
            return;
        }

        const sourceName = target.getAttribute('data-live-preview-source');
        const source = sourceName ? root.querySelector(`[data-live-preview-input="${sourceName}"]`) : null;
        if (!source) {
            return;
        }

        const value = (source.value ?? '').trim();
        if (binding.isImage) {
            target.setAttribute('src', value || binding.fallback);
            return;
        }

        if (target.tagName === 'SPAN' || target.tagName === 'DIV' || target.tagName === 'STRONG') {
            if (target.getAttribute('data-live-preview-format') === 'latlng') {
                const lat = (root.querySelector('[data-live-preview-input="Latitude"]')?.value ?? '').trim();
                const lng = (root.querySelector('[data-live-preview-input="Longitude"]')?.value ?? '').trim();
                target.textContent = lat && lng ? `${lat}, ${lng}` : binding.fallback;
                return;
            }

            if (target.getAttribute('data-live-preview-format') === 'meters') {
                target.textContent = value ? `${value} m` : binding.fallback;
                return;
            }

            if (source.tagName === 'SELECT') {
                const selectedText = source.selectedOptions?.[0]?.text?.trim();
                target.textContent = selectedText || value || binding.fallback;
                return;
            }

            target.textContent = value || binding.fallback;
        }
    });

    const published = root.querySelector('[data-live-preview-input="IsPublished"]');
    const publishedBadge = root.querySelector('[data-live-preview="published-badge"]');
    if (published && publishedBadge) {
        publishedBadge.textContent = published.checked ? 'Published' : 'Draft';
    }

    const routeAssets = root.querySelector('[data-live-preview="route-assets"]');
    if (routeAssets) {
        const audioCount = root.querySelectorAll('[data-repeat-group="AudioAssets"] input[name*="AudioAssets"][name$="AudioUrl"]');
        const speechCount = root.querySelectorAll('[data-repeat-group="SpeechTexts"] textarea[name*="SpeechTexts"][name$="Text"]');
        const audioFilled = Array.from(audioCount).filter((x) => (x.value ?? '').trim()).length;
        const speechFilled = Array.from(speechCount).filter((x) => (x.value ?? '').trim()).length;
        routeAssets.textContent = `${audioFilled} audio · ${speechFilled} text`;
    }
};

const syncAnchorPoiDetails = (root) => {
    if (!root) {
        return;
    }

    const select = root.querySelector('[data-anchor-poi-select="true"]');
    if (!select) {
        return;
    }

    const dataScript = root.querySelector('script[type="application/json"][data-anchor-poi-details]');
    if (!dataScript) {
        return;
    }

    let items = [];
    try {
        items = JSON.parse(dataScript.textContent ?? '[]');
    } catch {
        return;
    }

    const selectedId = Number.parseInt(select.value ?? '0', 10);
    const selected = items.find((item) => Number.parseInt(`${item.id ?? item.Id ?? 0}`, 10) === selectedId);
    if (!selected) {
        return;
    }

    const setValue = (name, value) => {
        const element = root.querySelector(`[data-live-preview-input="${name}"]`);
        if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement || element instanceof HTMLSelectElement) {
            element.value = value ?? '';
        }

        root.querySelectorAll(`[data-anchor-poi-summary="${name}"]`).forEach((target) => {
            if (name === 'Latitude' || name === 'Longitude') {
                target.textContent = value === null || value === undefined || value === '' ? '—' : `${value}`;
                return;
            }

            target.textContent = value ?? '—';
        });
    };

    setValue('Title', selected.title ?? selected.Title ?? '');
    setValue('Subtitle', selected.subtitle ?? selected.Subtitle ?? '');
    setValue('Description', selected.description ?? selected.Description ?? '');
    setValue('Location', selected.location ?? selected.Location ?? '');
    setValue('Latitude', selected.latitude ?? selected.Latitude ?? '');
    setValue('Longitude', selected.longitude ?? selected.Longitude ?? '');
    setValue('Category', selected.category ?? selected.Category ?? '');
    setValue('ImageUrl', selected.imageUrl ?? selected.ImageUrl ?? '');
    setValue('CoverImageUrl', selected.imageUrl ?? selected.ImageUrl ?? '');

    updateLivePreview(root);
};

const updatePoiPreviewCard = (card, row) => {
    if (!card) {
        return;
    }

    const placeholderImage = 'https://placehold.co/800x600/png?text=POI+Preview';
    const get = (name, fallback = '—') => (row?.getAttribute(`data-poi-${name}`) ?? '').trim() || fallback;

    const title = card.querySelector('[data-poi-preview="title"]');
    if (title) title.textContent = get('title', 'Chưa có POI');

    const subtitle = card.querySelector('[data-poi-preview="subtitle"]');
    if (subtitle) subtitle.textContent = get('subtitle', 'Tạo POI đầu tiên để xem preview');

    const image = card.querySelector('[data-poi-preview="image"]');
    if (image instanceof HTMLImageElement) {
        image.src = get('image', placeholderImage);
    }

    const category = card.querySelector('[data-poi-preview="category"]');
    if (category) category.textContent = get('category');

    const language = card.querySelector('[data-poi-preview="language"]');
    if (language) language.textContent = get('language');

    const radius = card.querySelector('[data-poi-preview="radius"]');
    if (radius) radius.textContent = `${get('radius', '0')} m`;

    const location = card.querySelector('[data-poi-preview="location"]');
    if (location) location.textContent = get('location');

    const latlng = card.querySelector('[data-poi-preview="latlng"]');
    if (latlng) {
        const lat = get('lat', '');
        const lng = get('lng', '');
        latlng.textContent = lat && lng ? `${lat}, ${lng}` : '—';
    }

    const speech = card.querySelector('[data-poi-preview="speech"]');
    if (speech) speech.textContent = get('speech', '—');

    const speechLang = card.querySelector('[data-poi-preview="speech-lang"]');
    if (speechLang) speechLang.textContent = get('speech-lang', '—');

    const badge = card.querySelector('[data-poi-preview="badge"]');
    if (badge) badge.textContent = row ? 'Featured' : 'No result';
};

const updatePoiIndex = (root) => {
    if (!root) {
        return;
    }

    const searchInput = root.querySelector('[data-poi-search]');
    const rows = Array.from(root.querySelectorAll('[data-poi-row]'));
    const card = root.querySelector('[data-poi-feature-card]');
    const noResults = root.querySelector('[data-poi-no-results]');
    if (!searchInput || rows.length === 0) {
        return;
    }

    const query = (searchInput.value ?? '').trim().toLowerCase();
    let firstVisible = null;

    rows.forEach((row) => {
        const haystack = (row.getAttribute('data-search-text') ?? '').toLowerCase();
        const isVisible = !query || haystack.includes(query);
        row.hidden = !isVisible;
        if (isVisible && firstVisible === null) {
            firstVisible = row;
        }
    });

    if (noResults) {
        noResults.hidden = firstVisible !== null;
    }

    updatePoiPreviewCard(card, firstVisible);
};

const updateUserPreviewCard = (card, row) => {
    if (!card) {
        return;
    }

    const get = (name, fallback = '—') => (row?.getAttribute(`data-user-${name}`) ?? '').trim() || fallback;

    const avatar = card.querySelector('[data-user-preview="avatar"]');
    if (avatar) {
        avatar.textContent = (get('name', 'U').charAt(0) || 'U').toUpperCase();
    }

    const name = card.querySelector('[data-user-preview="name"]');
    if (name) {
        name.textContent = get('name', 'No users');
    }

    const email = card.querySelector('[data-user-preview="email"]');
    if (email) {
        email.textContent = get('email', 'Tạo user đầu tiên để xem preview');
    }

    const status = card.querySelector('[data-user-preview="status"]');
    if (status) {
        status.textContent = get('status', 'Chưa có dữ liệu');
    }

    const badge = card.querySelector('[data-user-preview="badge"]');
    if (badge) {
        badge.textContent = row ? 'Featured' : 'No result';
    }

    const nameText = card.querySelector('[data-user-preview="name-text"]');
    if (nameText) {
        nameText.textContent = get('name');
    }

    const emailText = card.querySelector('[data-user-preview="email-text"]');
    if (emailText) {
        emailText.textContent = get('email');
    }

    const statusText = card.querySelector('[data-user-preview="status-text"]');
    if (statusText) {
        statusText.textContent = get('status');
    }

    const roles = card.querySelector('[data-user-preview="roles"]');
    if (roles) {
        const roleNames = (row?.getAttribute('data-user-roles') ?? '')
            .split('|')
            .map((x) => x.trim())
            .filter(Boolean);

        roles.innerHTML = roleNames.length === 0
            ? '—'
            : roleNames.map((role) => `<span class="user-role-chip">${role}</span>`).join('');
    }
};

const updateUserIndex = (root) => {
    if (!root) {
        return;
    }

    const searchInput = root.querySelector('[data-user-search]');
    const rows = Array.from(root.querySelectorAll('[data-user-row]'));
    const card = root.querySelector('[data-user-feature-card]');
    const noResults = root.querySelector('[data-user-no-results]');
    if (!searchInput || rows.length === 0) {
        return;
    }

    const query = (searchInput.value ?? '').trim().toLowerCase();
    let firstVisible = null;

    rows.forEach((row) => {
        const haystack = (row.getAttribute('data-search-text') ?? '').toLowerCase();
        const isVisible = !query || haystack.includes(query);
        row.hidden = !isVisible;
        if (isVisible && firstVisible === null) {
            firstVisible = row;
        }
    });

    if (noResults) {
        noResults.hidden = firstVisible !== null;
    }

    updateUserPreviewCard(card, firstVisible);
};

const updateUserPreviewCard = (card, row) => {
    if (!card) {
        return;
    }

    const get = (name, fallback = '—') => (row?.getAttribute(`data-user-${name}`) ?? '').trim() || fallback;

    const avatar = card.querySelector('[data-user-preview="avatar"]');
    if (avatar) avatar.textContent = (get('name', 'U').charAt(0) || 'U').toUpperCase();

    const name = card.querySelector('[data-user-preview="name"]');
    if (name) name.textContent = get('name', 'No users');

    const email = card.querySelector('[data-user-preview="email"]');
    if (email) email.textContent = get('email', 'Tạo user đầu tiên để xem preview');

    const status = card.querySelector('[data-user-preview="status"]');
    if (status) status.textContent = get('status', 'Chưa có dữ liệu');

    const badge = card.querySelector('[data-user-preview="badge"]');
    if (badge) badge.textContent = row ? 'Featured' : 'No result';

    const statusText = card.querySelector('[data-user-preview="status-text"]');
    if (statusText) statusText.textContent = get('status');

    const nameText = card.querySelector('[data-user-preview="name-text"]');
    if (nameText) nameText.textContent = get('name');

    const emailText = card.querySelector('[data-user-preview="email-text"]');
    if (emailText) emailText.textContent = get('email');

    const roles = card.querySelector('[data-user-preview="roles"]');
    if (roles) {
        const roleNames = (row?.getAttribute('data-user-roles') ?? '').split('|').map((x) => x.trim()).filter(Boolean);
        roles.innerHTML = roleNames.length === 0
            ? '—'
            : roleNames.map((role) => `<span class="user-role-chip">${role}</span>`).join('');
    }
};

const updateUserIndex = (root) => {
    if (!root) {
        return;
    }

    const searchInput = root.querySelector('[data-user-search]');
    const rows = Array.from(root.querySelectorAll('[data-user-row]'));
    const card = root.querySelector('[data-user-feature-card]');
    const noResults = root.querySelector('[data-user-no-results]');
    if (!searchInput || rows.length === 0) {
        return;
    }

    const query = (searchInput.value ?? '').trim().toLowerCase();
    let firstVisible = null;

    rows.forEach((row) => {
        const haystack = (row.getAttribute('data-search-text') ?? '').toLowerCase();
        const isVisible = !query || haystack.includes(query);
        row.hidden = !isVisible;
        if (isVisible && firstVisible === null) {
            firstVisible = row;
        }
    });

    if (noResults) {
        noResults.hidden = firstVisible !== null;
    }

    updateUserPreviewCard(card, firstVisible);
};

document.addEventListener('input', (event) => {
    const target = event.target;
    if (!(target instanceof HTMLElement)) {
        return;
    }

    const poiSearch = target.closest('[data-poi-search]');
    if (poiSearch) {
        const root = poiSearch.closest('[data-poi-index]');
        updatePoiIndex(root);
        return;
    }

    const userSearch = target.closest('[data-user-search]');
    if (userSearch) {
        const root = userSearch.closest('[data-user-index]');
        updateUserIndex(root);
        return;
    }

    const userSearch = target.closest('[data-user-search]');
    if (userSearch) {
        const root = userSearch.closest('[data-user-index]');
        updateUserIndex(root);
        return;
    }

    const form = target.closest('form[data-live-preview-form]');
    if (!form) {
        return;
    }

    syncAnchorPoiDetails(form);
    updateLivePreview(form);
});

document.addEventListener('change', (event) => {
    const target = event.target;
    if (!(target instanceof HTMLElement)) {
        return;
    }

    const poiSearch = target.closest('[data-poi-search]');
    if (poiSearch) {
        const root = poiSearch.closest('[data-poi-index]');
        updatePoiIndex(root);
        return;
    }

    const userSearch = target.closest('[data-user-search]');
    if (userSearch) {
        const root = userSearch.closest('[data-user-index]');
        updateUserIndex(root);
        return;
    }

    const userSearch = target.closest('[data-user-search]');
    if (userSearch) {
        const root = userSearch.closest('[data-user-index]');
        updateUserIndex(root);
        return;
    }

    const form = target.closest('form[data-live-preview-form]');
    if (!form) {
        return;
    }

    syncAnchorPoiDetails(form);
    updateLivePreview(form);
});

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('form[data-live-preview-form]').forEach((form) => {
        syncAnchorPoiDetails(form);
        updateLivePreview(form);
    });
    document.querySelectorAll('[data-poi-index]').forEach((root) => updatePoiIndex(root));
    document.querySelectorAll('[data-user-index]').forEach((root) => updateUserIndex(root));
});
