// pwa/src/services/firebase.ts
import { getAnalytics, type Analytics } from "firebase/analytics";
import { initializeApp, type FirebaseApp } from "firebase/app";
import { getMessaging, type Messaging }    from "firebase/messaging";

// ——————————
// 1) Tu configuración de Firebase (Sustituye con la tuya)
// ——————————
const firebaseConfig = {
    apiKey: "AIzaSyC1ArA65ErYtjKRkSzhjTC53Sqbj8VvdyQ",
    authDomain: "monteapp-f96cc.firebaseapp.com",
    projectId: "monteapp-f96cc",
    storageBucket: "monteapp-f96cc.firebasestorage.app",
    messagingSenderId: "1036678128041",
    appId: "1:1036678128041:web:1b524526a5cbfcf38864b8"
};

// ——————————
// 2) Inicializa la app y el cliente de mensajería
// ——————————
export const firebaseApp: FirebaseApp = initializeApp(firebaseConfig);
export const messaging: Messaging   = getMessaging(firebaseApp);
export const analytics: Analytics = getAnalytics(firebaseApp);