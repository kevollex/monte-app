import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { consume } from '@lit/context';
import { router, resolveRouterPath } from '../router';
import '@shoelace-style/shoelace/dist/components/input/input.js';
import '@shoelace-style/shoelace/dist/components/button/button.js';

import { authServiceContext } from '../services/auth-service/auth-service-context';
import { AuthService } from '../services/auth-service/auth-service';

@customElement('app-login')
export class AppLogin extends LitElement {
    @consume({ context: authServiceContext })
    private authService?: AuthService;

    @property() email = '';
    @property() password = '';
    @property() error = '';
    @property() jwtToken = '';

    static styles = css`
        :host {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        min-height: 100dvh;
        background-color: var(--app-background,rgb(255, 255, 255));
        box-sizing: border-box;
        padding: 24px;
    }

    img {
        width: 150px;
        margin-bottom: 16px;
        background-color: white;
        border-radius: 12px;
    }

    h2 {
        font-size: 24px;
        font-weight: bold;
        margin-bottom: 24px;
        text-align:center;
    }

    sl-input {
        margin-bottom: 12px;
        width: 100%;
        max-width: 280px;
        --sl-input-border-radius: 12px;
        --sl-input-padding-inline: 12px;
        --sl-input-font-size: 16px;
        --sl-input-background-color: #f5f5f5;
    }

    sl-button::part(base) {
        background-color: #5e8bff;
        color: white;
        border-radius: 16px;
        font-weight: bold;
        font-size: 16px;
        height: 40px;
        margin-top: 25px;
        height: 40px;
        width: 200px;
    }

    sl-button::part(base):hover {
        background-color: #4c7dff;
    }

    .error {
        color: red;
        margin-top: 8px;
        font-size: 14px;
    }
    @meddia (min-width: 600px) {
    img {
        width: 120px;
        }

        h2 {
        font-size: 22px;
        }

        sl-input {
        max-width: 340px;
        }

        sl-button::part(base) {
        max-width: 340px;
        }
    }
    @media (min-width: 1024px) {
        img {
        width: 140px;
        }

        h2 {
        font-size: 24px;
        }

        sl-input {
        max-width: 400px;
        }

        sl-button::part(base) {
        max-width: 400px;
        }
    }
    @media (prefers-color-scheme: dark) {
        :host {
        background-color: #1c1c1c;
        }
    }
    `;

    async firstUpdated() {
      console.log('This is login page');
  }

    async login() {
      this.error = '';
      try {
          const res = await this.authService?.login(this.email, this.password);
          if (res) {
            console.log('Login successful');
            this.jwtToken = res;
            localStorage.setItem('jwt', this.jwtToken);
            
            router.navigate(resolveRouterPath('home'));
          } else {
            this.error = 'Credenciales inválidas.';
          }
      } catch (err) {
          this.error = 'Error al iniciar sesión.';
      }
    }

    render() {
      return html`
        <form
          @submit=${(e: Event) => {
            e.preventDefault();
            this.login();
          }}
          style="display: flex; flex-direction: column; align-items: center; width: 100%;"
        >
          <img src="/assets/logo.png" alt="Logo Colegio Montessori" />
          <h2>Inicio de sesión</h2>

          <sl-input
            label="E-mail"
            type="email"
            .value=${this.email}
            @sl-input=${(e: any) => this.email = e.target.value}
            autocomplete="username"
            required
            autofocus
            style="margin-bottom: 12px;"
          ></sl-input>

          <sl-input
            label="Password"
            type="password"
            .value=${this.password}
            @sl-input=${(e: any) => this.password = e.target.value}
            autocomplete="current-password"
            required
            style="margin-bottom: 12px;"
          ></sl-input>

          ${this.error ? html`<div class="error">${this.error}</div>` : null}

          <sl-button
            variant="primary"
            type="submit"
            style="margin-top: 16px;"
          >Ingresar</sl-button>
        </form>
        `;
    }
}