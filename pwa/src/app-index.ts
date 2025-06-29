import { LitElement, css, html } from 'lit';
import { customElement } from 'lit/decorators.js';

import './pages/app-home';
import './components/header';
import './styles/global.css';
import { router } from './router';


import { provide } from '@lit/context';
import { authServiceContext } from './services/auth-service/auth-service-context';
import { AuthService } from './services/auth-service/auth-service';
import { absencesServiceContext } from './services/absences-service/absences-service-context';
import { AbsencesService } from './services/absences-service/absences-service';
import { registerFcmToken, onFcmMessage } from './firebase';
@customElement('app-index')
export class AppIndex extends LitElement {

  @provide({ context: authServiceContext })
  authService = new AuthService();
  @provide({ context: absencesServiceContext })
  absencesService = new AbsencesService();

  static styles = css`
    main {
      padding-left: 16px;
      padding-right: 16px;
      padding-bottom: 16px;
    }
  `;

  firstUpdated() {
    // Tu VAPID Key obtenida de Firebase Console > Cloud Messaging > Certificados push web
    const VAPID_KEY = 'BOWPLxzD1xp0DfnpEY8Rf4Z0-KblW73hpRtyZhY6MxUuwMdLVNb3H_-mRiyxROFDza3SUNdIeFGGGxcD_8dOQEQ';

    registerFcmToken(VAPID_KEY)
      .then(token => {
        console.log('Token FCM recibido:', token);
        // Lanza el registro en tu API
        return fetch('https://localhost:7448/devices/register', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ FcmToken: token })
        });
      })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
      })
      .then(({ deviceId }) => {
        console.log('Device registrado con ID:', deviceId);
        // Guárdalo para usarlo luego al enviar notificaciones
        localStorage.setItem('deviceId', String(deviceId));
      })
      .catch(err => console.error('Error registrando device:', err));

    // Reactualiza si tu router cambia de ruta
    router.addEventListener('route-changed', () => this.requestUpdate());
  }

  render() {
    return html`<main>${router.render()}</main>`;
  }
}