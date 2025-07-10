// src/firebase.ts
import { initializeApp } from 'firebase/app';
import { getMessaging, getToken, onMessage } from 'firebase/messaging';

const firebaseConfig = {
    apiKey: "AIzaSyC1ArA65ErYtjKRkSzhjTC53Sqbj8VvdyQ",
    authDomain: "monteapp-f96cc.firebaseapp.com",
    projectId: "monteapp-f96cc",
    storageBucket: "monteapp-f96cc.firebasestorage.app",
    messagingSenderId: "1036678128041",
    appId: "1:1036678128041:web:1b524526a5cbfcf38864b8",
};

const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

/**
 * Registra el service worker (sw.js) y pide permiso y token FCM.
 */
export async function registerFcmToken(vapidKey: string): Promise<string> {
  // 1) registra tu SW (el mismo sw.js que usa Workbox)
    const registration = await navigator.serviceWorker.register('/sw.js');
  // 2) pide permiso
    const status = await Notification.requestPermission();
    if (status !== 'granted') {
    throw new Error('Permiso de notificaciones denegado');
    }
  // 3) pide el token
    const token = await getToken(messaging, { vapidKey, serviceWorkerRegistration: registration });
    if (!token) throw new Error('No se pudo generar el token FCM');
    return token;
}

/**
 * Maneja notificaciones en primer plano.
 */
export function onFcmMessage(callback: (payload: any) => void) {
    onMessage(messaging, payload => {
    console.log('☝️ Notificación en foreground:', payload);
    const { title, body } = payload.notification!;
    new Notification(title ?? 'Notificación', { body, icon: '/assets/logo.png' });
    callback(payload);
    });
}