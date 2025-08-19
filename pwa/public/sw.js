importScripts(
    'https://storage.googleapis.com/workbox-cdn/releases/7.3.0/workbox-sw.js'
);

// This is your Service Worker, you can put any of your custom Service Worker
// code in this file, above the `precacheAndRoute` line.

// When widget is installed/pinned, push initial state.
self.addEventListener('widgetinstall', (event) => {
    event.waitUntil(updateWidget(event));
});

// When widget is shown, update content to ensure it is up-to-date.
self.addEventListener('widgetresume', (event) => {
    event.waitUntil(updateWidget(event));
});

// When the user clicks an element with an associated Action.Execute,
// handle according to the 'verb' in event.action.
self.addEventListener('widgetclick', (event) => {
if (event.action == "updateName") {
    event.waitUntil(updateName(event));
}
});

// When the widget is uninstalled/unpinned, clean up any unnecessary
// periodic sync or widget-related state.
self.addEventListener('widgetuninstall', (event) => {});

const updateWidget = async (event) => {
// The widget definition represents the fields specified in the manifest.
    const widgetDefinition = event.widget.definition;

    // Fetch the template and data defined in the manifest to generate the payload.
    const payload = {
        template: JSON.stringify(await (await fetch(widgetDefinition.msAcTemplate)).json()),
        data: JSON.stringify(await (await fetch(widgetDefinition.data)).json()),
    };

    // Push payload to widget.
    await self.widgets.updateByInstanceId(event.instanceId, payload);
}

const updateName = async (event) => {
    const name = event.data.json().name;

    // The widget definition represents the fields specified in the manifest.
    const widgetDefinition = event.widget.definition;

    // Fetch the template and data defined in the manifest to generate the payload.
    const payload = {
        template: JSON.stringify(await (await fetch(widgetDefinition.msAcTemplate)).json()),
        data: JSON.stringify({name}),
    };

    // Push payload to widget.
    await self.widgets.updateByInstanceId(event.instanceId, payload);
}

workbox.precaching.precacheAndRoute(self.__WB_MANIFEST || []);

importScripts('https://www.gstatic.com/firebasejs/11.10.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/11.10.0/firebase-messaging-compat.js');

firebase.initializeApp({
    apiKey: "AIzaSyC1ArA65ErYtjKRkSzhjTC53Sqbj8VvdyQ",
    authDomain: "monteapp-f96cc.firebaseapp.com",
    projectId: "monteapp-f96cc",
    storageBucket: "monteapp-f96cc.firebasestorage.app",
    messagingSenderId: "1036678128041",
    appId: "1:1036678128041:web:1b524526a5cbfcf38864b8"
});

const messaging = firebase.messaging();
messaging.onBackgroundMessage(async payload => {
  // 1) ¿Hay alguna ventana visible? Si sí, no mostrar aquí.
  const clientList = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
  const anyFocused = clientList.some(win => win.visibilityState === 'visible');
  if (anyFocused) return;

  // 2) Leemos de payload.data
  const { title, body } = payload.data ?? {};
  self.registration.showNotification(title ?? 'Notificación', {
    body,
    icon: '/assets/logo.png'
  });
});