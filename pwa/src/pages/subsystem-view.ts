import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import { consume } from '@lit/context';
import '../components/browser-view';
import '../components/subsystem-view-header';
import '@shoelace-style/shoelace/dist/components/button/button.js';

import { montessoriBoWrapperServiceContext } from '../services/montessoribowrapper-service/montessoribowrapper-service-context';
import { MontessoriBoWrapperService } from '../services/montessoribowrapper-service/montessoribowrapper-service';

@customElement('subsystem-view')
export class SubsystemView extends LitElement {
  @property({ type: String }) htmlContent = '';
  @property({ type: String }) label = '';
  @property({ type: String }) subsystemName = '';

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

  private getSubsystemTitle(label: string): string {
    // Map of subsystem IDs to names, matching MontessoriBoWrapperService.cs
    const subsystemNames: Record<string, string> = {
      "control-semanal": "Control Semanal",
      "cartas-recibidas": "Cartas Recibidas",
      "circulares": "Circulares",
      "licencias": "Licencias",
    };
    return subsystemNames[label] ?? "Subsistema Desconocido 🕵";
  }

  async firstUpdated() {
    if (!this.montessoriBoWrapperService) {
      console.error('MontessoriBoWrapperService is not available');
      return;
    }
    this.subsystemName = this.getSubsystemTitle(this.label);
    this.htmlContent = await this.montessoriBoWrapperService.getPage(this.label);
  }

  render() {
    return html`
      <subsystem-view-header .title=${this.subsystemName}></subsystem-view-header>
      </div>
      <div class="content">
        <browser-view .htmlContent=${this.htmlContent}></browser-view>
      </div>
    `;
  }
}