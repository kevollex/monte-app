import { LitElement, css } from 'lit';
import { customElement } from 'lit/decorators.js';

import './pages/app-home';
import './components/header';
import './styles/global.css';
import { router } from './router';

import { provide } from '@lit/context';
import { absencesServiceContext } from './services/absences-service/absences-service-context';
import { AbsencesService } from './services/absences-service/absences-service';
// import firebase
import { messaging } from './services/firebase';
import { getToken, onMessage } from 'firebase/messaging';

@customElement('app-index')
export class AppIndex extends LitElement {

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

    if ('serviceWorker' in navigator) {
      navigator.serviceWorker
        .register('/firebase-messaging-sw.js')
        .then(reg => console.log('FCM SW registered:', reg.scope))
        .catch(err => console.error('SW registration failed:', err));
    }

    // 2) Pedir permiso y obtener token
    this.registerForPush();

    // 3) Mostrar notificaciones en foreground
    onMessage(messaging, payload => {
      const { title, body } = payload.notification!;
      new Notification(title ?? 'Notificación', { body: body ?? '' });
    });

    router.addEventListener('route-changed', () => {
      if ("startViewTransition" in document) {
        (document as any).startViewTransition(() => this.requestUpdate());
      }
      else {
        this.requestUpdate();
      }
    });
  }

  render() {
    // router config can be round in src/router.ts
    return router.render();
  }
 private async registerForPush() {
    try {
      const permission = await Notification.requestPermission();
      if (permission !== 'granted') {
        console.warn('Push permission denied');
        return;
      }
      const token = await getToken(messaging, {
        vapidKey: 'BOWPLxzD1xp0DfnpEY8Rf4Z0-KblW73hpRtyZhY6MxUuwMdLVNb3H_-mRiyxROFDza3SUNdIeFGGGxcD_8dOQEQ'
      });
      console.log('FCM Token:', token);

      // 4) Enviamos el token al backend
      await fetch('/devices/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId: 1,   // o el ID real del usuario
          token
        })
      });
    } catch (e) {
      console.error('Error registrando para push:', e);
    }
  }
}
