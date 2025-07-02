import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { consume } from '@lit/context';
import { router, resolveRouterPath } from '../router';
import '@shoelace-style/shoelace/dist/components/button/button.js';

import { authServiceContext } from '../services/auth-service/auth-service-context';
import { AuthService } from '../services/auth-service/auth-service';

@customElement('subsystem-view-header')
export class SubsystemViewHeader extends LitElement {
  @consume({ context: authServiceContext })
  private authService?: AuthService;
  
  @property({ type: String }) title = '';
  
  static styles = css`
    .header {
      display: flex;
      align-items: center;
      padding: 12px 16px;
      background: #f7f7f7;
      border-bottom: 1px solid #e0e0e0;
    }
    .back-btn {
      margin-right: 16px;
    }
    .logout-btn {
      margin-left: auto;
      background: #e53935;
      color: #fff;
      border-radius: 20px;
      font-weight: bold;
    }
    @media (prefers-color-scheme: dark) {
      .header {
        background: #232323;
        border-bottom: 1px solid #333;
      }
    }
  `;

  private handleBack() {
    router.navigate(resolveRouterPath());
  }

  private async handleLogout() {
    try {
      await this.authService?.logout();
      localStorage.removeItem('jwt');
      router.navigate(resolveRouterPath('login'));
    } catch (error) {
      alert('Error al cerrar sesión');
    }
  }

  render() {
    return html`
      <div class="header">
        <sl-button class="back-btn" size="small" @click=${this.handleBack}>
          ← Volver
        </sl-button>
        <span>${this.title}</span>
        <sl-button
          class="logout-btn"
          size="small"
          variant="danger"
          @click=${this.handleLogout}
        >
          Cerrar sesión
        </sl-button>
      </div>
    `;
  }
}