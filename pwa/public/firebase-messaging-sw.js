// Import the functions you need from the SDKs you need
import { initializeApp } from "https://www.gstatic.com/firebasejs/11.9.1/firebase-app-compat.js";
import { initializeApp } from "https://www.gstatic.com/firebasejs/11.9.1/firebase-messaging-compat.js";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
    apiKey: "AIzaSyC1ArA65ErYtjKRkSzhjTC53Sqbj8VvdyQ",
    authDomain: "monteapp-f96cc.firebaseapp.com",
    projectId: "monteapp-f96cc",
    storageBucket: "monteapp-f96cc.firebasestorage.app",
    messagingSenderId: "1036678128041",
    appId: "1:1036678128041:web:1b524526a5cbfcf38864b8",
    measurementId: "G-F0P79F3RV9"
};

// Initialize Firebase
firebase.initializeApp(firebaseConfig);
const messaging = firebase.messaging();

messaging.onBackgroundMessage(payload => {
    console.log("Recibiste un mensaje mientras estabas ausente");
//previo a mostrar la notificacion.
    const notidicationTitle = payload.notification.title;
    const notificationOptions = {
        body: payload.notification.body,
        icon: "https://monteapp.com/assets/images/logo.png",
    }

    return self.registration.showNotification(notidicationTitle, notificationOptions);
})
