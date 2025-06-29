import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('browser-view')
export class BrowserView extends LitElement {
  @property({ type: String }) htmlContent = '';

  static styles = css`
    iframe {
      width: 100%;
      height: 600px;
      border: none;
      border-radius: 8px;
      background: white;
    }
  `;

  updated(changedProps: Map<string, any>) {
    if (changedProps.has('htmlContent')) {
      const iframe = this.shadowRoot?.querySelector('iframe');
      if (iframe && this.htmlContent) {
        iframe.srcdoc = this.htmlContent;
      }
    }
  }

  render() {
    return html`<iframe sandbox="allow-forms allow-scripts allow-same-origin"></iframe>`;
  }
}