const CACHE_NAME = 'travelapp-public-v2';
const PRECACHE = [
  '/',
  '/css/site.css',
  '/js/public-app.js',
  '/manifest.webmanifest'
];

self.addEventListener('install', event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(PRECACHE)));
});

self.addEventListener('activate', event => {
  event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key)))));
});

self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET') {
    return;
  }

  event.respondWith((async () => {
    const cache = await caches.open(CACHE_NAME);
    const requestUrl = new URL(event.request.url);

    if (event.request.destination === 'document') {
      try {
        const response = await fetch(event.request);
        if (response.ok && !requestUrl.search) {
          cache.put(event.request, response.clone());
        }

        return response;
      } catch {
        const cached = await cache.match(event.request);
        return cached || new Response('Offline', { status: 503, headers: { 'Content-Type': 'text/plain' } });
      }
    }

    const cached = await cache.match(event.request);
    if (cached) {
      return cached;
    }

    try {
      const response = await fetch(event.request);
      if (response.ok && (event.request.destination === 'script' || event.request.destination === 'style' || event.request.destination === 'audio')) {
        cache.put(event.request, response.clone());
      }
      return response;
    } catch {
      return cached || new Response('Offline', { status: 503, headers: { 'Content-Type': 'text/plain' } });
    }
  })());
});
