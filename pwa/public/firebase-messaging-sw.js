// public/firebase-messaging-sw.js

// Importa la versión “compat” de Firebase para Service Workers
importScripts('https://www.gstatic.com/firebasejs/11.9.1/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/11.9.1/firebase-messaging-compat.js');

// Inicializa tu app con la misma configuración que en el cliente
firebase.initializeApp({
  apiKey: "AIzaSyC1ArA65ErYtjKRkSzhjTC53Sqbj8VvdyQ",
  authDomain: "monteapp-f96cc.firebaseapp.com",
  projectId: "monteapp-f96cc",
  storageBucket: "monteapp-f96cc.firebasestorage.app",
  messagingSenderId: "1036678128041",
  appId: "1:1036678128041:web:1b524526a5cbfcf38864b8"
});

// Obtén el objeto messaging
const messaging = firebase.messaging();

// Maneja mensajes en segundo plano
messaging.onBackgroundMessage((payload) => {
  console.log('[firebase-messaging-sw.js] Received background message ', payload);
  const { title, body } = payload.notification || {};
  self.registration.showNotification(title, {
    body,
    // icon: '/path/to/icon.png'
  });
});