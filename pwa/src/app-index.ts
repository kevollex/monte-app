import { LitElement, css, html } from 'lit';
import { customElement } from 'lit/decorators.js';

import './pages/app-home';
import './components/header';
import './styles/global.css';
import { router } from './router';

import { provide } from '@lit/context';
import { authServiceContext } from './services/auth-service/auth-service-context';
import { AuthService } from './services/auth-service/auth-service';
import { montessoriBoWrapperServiceContext } from './services/montessoribowrapper-service/montessoribowrapper-service-context';
import { MontessoriBoWrapperService } from './services/montessoribowrapper-service/montessoribowrapper-service';
import { onFcmMessage, registerFcmToken } from './firebase';

@customElement('app-index')
export class AppIndex extends LitElement {

  @provide({ context: authServiceContext })
  authService = new AuthService();
  @provide({ context: montessoriBoWrapperServiceContext })
  montessoriBoWrapperService = new MontessoriBoWrapperService();

  static styles = css`
    main {
      padding-left: 16px;
      padding-right: 16px;
      padding-bottom: 16px;
    }
  `;

  firstUpdated() {
    // // Tu VAPID Key de Firebase Console → Cloud Messaging → Certificados Web
    // const VAPID_KEY = 'BOWPLxzD1xp0DfnpEY8Rf4Z0-KblW73hpRtyZhY6MxUuwMdLVNb3H_-mRiyxROFDza3SUNdIeFGGGxcD_8dOQEQ';

    // const deviceID = localStorage.getItem('deviceId') ?? '';
    // if (!deviceID) {
    //   console.warn('⚠️ No se encontró deviceId en localStorage.');
    //   // Registrar dispositivo si no existe
    //   return;
    // }


    // // 1) Registrar SW, pedir permiso y obtener token
    // registerFcmToken(VAPID_KEY)
    //   .then(token => {
    //     console.log('🔑 Token FCM recibido:', token);
    //     // Guarda el token localmente
    //     localStorage.setItem('fcmToken', token);

    //     // 2) Envía token al backend para suscripción
    //     const jwt = localStorage.getItem('jwt') ?? '';
    //     return fetch('https://localhost:7448/notifications/subscribe', {
    //       method: 'POST',
    //       headers: {
    //         'Content-Type': 'application/json',
    //         'Authorization': `Bearer ${jwt}`
    //       },
    //       body: JSON.stringify({ deviceToken: token, deviceType: 'web' })
    //     });
    //   })
    //   .then(res => {
    //     if (!res.ok) throw new Error(`Error HTTP ${res.status}`);
    //     console.log('✅ Dispositivo suscrito correctamente');
    //   })
    //   .catch(err => console.error('Error al registrar/dispositivo:', err));

    // // 3) Manejar notificaciones en primer plano
    // onFcmMessage(payload => {
    //   console.log('📩 manejado en appindex:', payload);
    //   // Aquí podrías lanzar un toast o actualizar tu UI
    // });

    // Re-render si cambia la ruta
    router.addEventListener('route-changed', () => this.requestUpdate());
  }

  render() {
    return html`<main>${router.render()}</main>`;
  }
}

