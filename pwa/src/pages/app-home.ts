import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { consume } from '@lit/context';
import { router, resolveRouterPath } from '../router';
import '../components/card-app';
import '@shoelace-style/shoelace/dist/components/card/card.js';
import '@shoelace-style/shoelace/dist/components/button/button.js';

import { authServiceContext } from '../services/auth-service/auth-service-context';
import { AuthService } from '../services/auth-service/auth-service';

import { montessoriBoWrapperServiceContext } from '../services/montessoribowrapper-service/montessoribowrapper-service-context';
import { MontessoriBoWrapperService } from '../services/montessoribowrapper-service/montessoribowrapper-service';

@customElement('app-home')
export class AppHome extends LitElement {
  @consume({ context: authServiceContext })
  private authService?: AuthService;

  @consume({ context: montessoriBoWrapperServiceContext })
  private montessoriBoWrapperService?: MontessoriBoWrapperService;

  @property() username = 'Nombre del padre';
  @property() subSystems: any[] = []; // TODO: define a type for subsystem

  private subSystemsCardsStyles: Record<string, { label: string; color: string; textColor: string }> = {
    "Licencias": { label: "Licencias", color: "#D9C8F0", textColor: "#6A1B9A" }
    // TODO: add rest of the subsystems with their styles
  };

  static styles = css`
    :host {
    display: flex;
    justify-content: center;
    align-items: flex-start;
    min-height: 100dvh;
    background-color: var(--app-background, rgb(255, 255, 255));
    box-sizing: border-box;
    padding: 24px;
  }
    .container {
    width: 100%;
    max-width: 1024px;
    margin: 0 auto;
    display: flex;
    flex-direction: column;
    align-items: space-between;
  }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .logo {
      width: 70px;
      background-color: white;
      border-radius: 30px;
    }

    .icon {
      width: 40px;
      cursor: pointer;
      background-color: #5e8bff;
      border-radius: 50%;
    }

    .username {
      font-size: 20px;
      font-weight: 600;
      text-align: center;
      margin-bottom: 24px;
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 24px;
      width: 100%;
      justify-items: center;
    }

      sl-button::part(base) {
  background-color: #5e8bff;
  color: white;
  border-radius: 16px;
  font-weight: bold;
  font-size: 16px;
  height: 40px;
  margin-top: 50px;
  height: 40px;
  width: 200px;
}

sl-button::part(base):hover {
  background-color: #4c7dff;
}
    @media (prefers-color-scheme: dark) {
  :host {
    background-color: #1c1c1c;
  }

  .username {
    color: white;
  }
}

    @media (min-width: 1024px) {
      .grid {
        gap: 24px;
      }
    }
  `;

  async firstUpdated() {
    try {
      // TODO: Maybe call in another event like connectedCallback
      const response = await this.montessoriBoWrapperService?.getHomeData();
      this.username = response.username;
      this.subSystems = response.subSystems;
    } catch (e) {
      // handle error (optional)
    }
  }
  
  async logout() {
    await this.authService?.logout();
    localStorage.removeItem('jwt');
    router.navigate(resolveRouterPath('login'));
    // window.location.href = '/login';
  }

  render() {
    return html`
    <main>
        <div class="container">
        <div class="header">
            <img src="/assets/logo.png" class="logo" />
            <img src="/assets/notification.png" class="icon" />
        </div>

        <div class="username">${this.username}</div>
        <div class="grid">
            <!-- TODO: define a type for subsystem -->
            ${this.subSystems.map(
              (subsystem: any) => {
                const cardProps = this.subSystemsCardsStyles[subsystem.name] || {};
                
                return html`
                <card-app
                  label="${subsystem.name}",
                  color="${cardProps.color || ''}"
                  textColor="${cardProps.textColor || ''}"
                  @click=${() => router.navigate(subsystem.route)}
                ></card-app>
              `;}
            )}

        <div style="display:flex; justify-content:center;">
            <sl-button @click=${this.logout}>Salir</sl-button>
        </div>
        </div>
    </main>
    `;
  }
}
