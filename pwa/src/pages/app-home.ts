import { LitElement, html, css } from 'lit';
import { customElement } from 'lit/decorators.js';

import '../components/card-app';
import '@shoelace-style/shoelace/dist/components/card/card.js';
import '@shoelace-style/shoelace/dist/components/button/button.js';

@customElement('app-home')
export class AppHome extends LitElement {

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

  logout() {
    localStorage.removeItem('usuario');
    window.location.href = '/login';
  }

  render() {
    return html`
    <div class="container">
      <div class="header">
        <img src="/assets/logo.png" class="logo" />
        <img src="/assets/notification.png" class="icon" />
      </div>

      <div class="username">Nombre del padre</div>

      <div class="grid">
        <card-app label="Control Semanal" color="#A8E6CF" textColor="#2E7D32"></card-app>
        <card-app label="Circulares" color="#FFF9B0" textColor="#9C6B00"></card-app>
        <card-app label="Licencias" color="#D9C8F0" textColor="#6A1B9A"></card-app>
        <card-app label="Cartas" color="#FFCDD2" textColor="#B71C1C"></card-app>
      </div>

      <div style="display:flex; justify-content:center;">
        <sl-button @click=${this.logout}>Salir</sl-button>
      </div>
    </div>
    `;
  }
}
