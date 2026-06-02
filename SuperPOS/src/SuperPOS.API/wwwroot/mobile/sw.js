const CACHE_NAME = 'superpos-mobile-v6';

self.addEventListener('install', e => {
  self.skipWaiting();
});

self.addEventListener('activate', e => {
  e.waitUntil(clients.claim());
});

self.addEventListener('fetch', e => {
  const url = new URL(e.request.url);
  // Redirigir peticiones estáticas locales de la app con estrategia network-first
  if (url.pathname.includes('/mobile/')) {
    e.respondWith(
      fetch(e.request).catch(() => caches.match(e.request))
    );
  }
});
