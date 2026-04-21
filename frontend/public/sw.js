// public/sw.js
// A minimal service worker to satisfy PWA installability criteria
self.addEventListener('install', (event) => {
  // Force the waiting service worker to become the active service worker.
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  // Tell the active service worker to take control of the page immediately.
  event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (event) => {
  // A fetch event listener is required by browsers to trigger the PWA install prompt.
  // We simply pass through the network request.
  return;
});
