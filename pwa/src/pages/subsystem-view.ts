import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { consume } from '@lit/context';
import { router, resolveRouterPath } from '../router';
import '../components/browser-view';
import '@shoelace-style/shoelace/dist/components/button/button.js';

import { montessoriBoWrapperServiceContext } from '../services/montessoribowrapper-service/montessoribowrapper-service-context';
import { MontessoriBoWrapperService } from '../services/montessoribowrapper-service/montessoribowrapper-service';


@customElement('subsystem-view')
export class SubsystemView extends LitElement {
  @property({ type: String }) htmlContent = '';

  @consume({ context: montessoriBoWrapperServiceContext })
  private montessoriBoWrapperService?: MontessoriBoWrapperService;

  // @property() licenciasResult? = '';

  static styles = css`
    :host {
      display: block;
      min-height: 100dvh;
      background: var(--app-background, #fff);
    }
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
    .content {
      padding: 0;
      height: calc(100dvh - 48px);
      display: flex;
      flex-direction: column;
    }
    browser-view {
      flex: 1 1 0;
      width: 100%;
      height: 100%;
    }
    @media (prefers-color-scheme: dark) {
      .header {
        background: #232323;
        border-bottom: 1px solid #333;
      }
    }
  `;

  async firstUpdated() {
    if (!this.montessoriBoWrapperService) {
      console.error('MontessoriBoWrapperService is not available');
      return;
    }
    this.htmlContent = await this.montessoriBoWrapperService?.getLicenciasPage();
  }

  render() {
    return html`
      <div class="header">
        <sl-button class="back-btn" size="small" @click=${() => router.navigate(resolveRouterPath())}>
          ← Volver
        </sl-button>
        <span>Submódulo</span>
      </div>
      <div class="content">
        <browser-view .htmlContent=${this.htmlContent}></browser-view>
      </div>
    `;
  }
}