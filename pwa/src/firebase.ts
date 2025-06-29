// src/firebase.ts

import { initializeApp } from 'firebase/app';
import { getMessaging, getToken, onMessage } from 'firebase/messaging';

// Tu configuración de Firebase (del JSON que descargaste)
const firebaseConfig = {
  apiKey: "AIzaSyC1ArA65ErYtjKRkSzhjTC53Sqbj8VvdyQ",
  authDomain: "monteapp-f96cc.firebaseapp.com",
  projectId: "monteapp-f96cc",
  storageBucket: "monteapp-f96cc.firebasestorage.app",
  messagingSenderId: "1036678128041",
  appId: "1:1036678128041:web:1b524526a5cbfcf38864b8",
};

const app = initializeApp(firebaseConfig);
export const messaging = getMessaging(app);

/**
 * Solicita permiso y obtiene el token FCM.
 */
export async function registerFcmToken(vapidKey: string): Promise<string> {
  const status = await Notification.requestPermission();
  if (status !== 'granted') {
    throw new Error('Permiso de notificaciones denegado');
  }
  const token = await getToken(messaging, { vapidKey });
  if (!token) {
    throw new Error('No se pudo generar el token FCM');
  }
  return token;
}

/**
 * Oye mensajes entrantes mientras la PWA está en primer plano.
 */
export function onFcmMessage(callback: (payload: any) => void) {
  onMessage(messaging, callback);
}