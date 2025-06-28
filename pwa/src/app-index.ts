import { LitElement, css } from 'lit';
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
}
