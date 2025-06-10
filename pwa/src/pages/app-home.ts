import { LitElement, css, html } from 'lit';
import { property, customElement } from 'lit/decorators.js';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';
import { resolveRouterPath } from '../router';

import { consume } from '@lit/context';
import { type AbsencesService, absencesServiceContext } from '../services/absences-service/absences-service-context'; 

import '@shoelace-style/shoelace/dist/components/card/card.js';
import '@shoelace-style/shoelace/dist/components/button/button.js';
import '@shoelace-style/shoelace/dist/components/input/input.js';

import { styles } from '../styles/shared-styles';

@customElement('app-home')
export class AppHome extends LitElement {
  @consume({ context: absencesServiceContext }) 
  private absencesService?: AbsencesService;

  // For more information on using properties and state in lit
  // check out this link https://lit.dev/docs/components/properties/
  @property() message = 'Welcome!';

  static styles = [
    styles,
    css`
    #welcomeBar {
      display: flex;
      justify-content: center;
      align-items: center;
      flex-direction: column;
    }

    #welcomeCard,
    #infoCard {
      padding: 18px;
      padding-top: 0px;
    }

    sl-card::part(footer) {
      display: flex;
      justify-content: flex-end;
    }

    @media(min-width: 750px) {
      sl-card {
        width: 70vw;
      }
    }


    @media (horizontal-viewport-segments: 2) {
      #welcomeBar {
        flex-direction: row;
        align-items: flex-start;
        justify-content: space-between;
      }

      #welcomeCard {
        margin-right: 64px;
      }
    }
  `];

  @property() sqlServerInfoResult = 'Loading...';

  async firstUpdated() {
    // this method is a lifecycle event in lit
    // for more info check out the lit docs https://lit.dev/docs/components/lifecycle/
    console.log('This is your home page');
    // TODO Delete this when we are done testing. Just here to test the API call.
    try {
      const result = await this.absencesService?.getSqlServerInfo();
      this.sqlServerInfoResult = JSON.stringify(result, null, 2);
      
    } catch (error: any) {
      if (error.response) {
        this.sqlServerInfoResult = `Error: ${error.response.statusText}`;
      } else {
        this.sqlServerInfoResult = 'Request failed.';
      }
    }
    console.log(this.sqlServerInfoResult);
    console.log('✅');
  }

  share() {
    if ((navigator as any).share) {
      (navigator as any).share({
        title: 'PWABuilder pwa-starter',
        text: 'Check out the PWABuilder pwa-starter!',
        url: 'https://github.com/pwa-builder/pwa-starter',
      });
    }
  }

  @property() licenciasResult = '';
  @property() email = '';
  @property() password = '';

  async getLicenciasPoC() {
    if (!this.email || !this.password) {
      this.licenciasResult = 'Please enter email and password.';
      return;
    }
    this.licenciasResult = 'Loading...';
    try {
      const res = await this.absencesService?.getLicenciasPoC(this.email, this.password);
      this.licenciasResult = res ?? '';
    } catch (e: any) {
      if (e.response) {
        this.licenciasResult = `Error: ${e.response.statusText}`;
      } else {
        this.licenciasResult = 'Request failed.';
      }
    }
  }

  render() {
    return html`
      <app-header></app-header>

      <main>
        <div id="welcomeBar">
          <sl-card id="welcomeCard">
            <div slot="header">
              <h2>${this.message}</h2>
            </div>

            <p>
              For more information on the PWABuilder pwa-starter, check out the
              <a href="https://docs.pwabuilder.com/#/starter/quick-start">
                documentation</a>.
            </p>

            <p id="mainInfo">
              Welcome to the
              <a href="https://pwabuilder.com">PWABuilder</a>
              pwa-starter! Be sure to head back to
              <a href="https://pwabuilder.com">PWABuilder</a>
              when you are ready to ship this PWA to the Microsoft Store, Google Play
              and the Apple App Store!
            </p>

            ${'share' in navigator
              ? html`<sl-button slot="footer" variant="default" @click="${this.share}">
                        <sl-icon slot="prefix" name="share"></sl-icon>
                        Share this Starter!
                      </sl-button>`
              : null}
          </sl-card>

          <sl-card id="infoCard">
            <h2>Technology Used</h2>

            <ul>
              <li>
                <a href="https://www.typescriptlang.org/">TypeScript</a>
              </li>

              <li>
                <a href="https://lit.dev">lit</a>
              </li>

              <li>
                <a href="https://shoelace.style/">Shoelace</a>
              </li>

              <li>
                <a href="https://github.com/thepassle/app-tools/blob/master/router/README.md"
                  >App Tools Router</a>
              </li>
            </ul>
          </sl-card>

          <sl-card id="sqlServerCard">
            <h2>Test SQL Server Endpoint</h2>
            <p>${this.sqlServerInfoResult}</p>
          </sl-card>
          
          <sl-card id="licenciasCard">
          <h2>Test LicenciasPoC Endpoint</h2>
          <sl-input
            label="Email"
            type="email"
            .value=${this.email}
            @sl-input=${(e: any) => { this.email = e.target.value; }}
            style="margin-bottom: 8px;"
          ></sl-input>
          <sl-input
            label="Password"
            type="password"
            .value=${this.password}
            @sl-input=${(e: any) => { this.password = e.target.value; }}
            style="margin-bottom: 8px;"
          ></sl-input>
          <sl-button variant="primary" @click=${this.getLicenciasPoC}>Get Licencias</sl-button>
          <div style="white-space: pre-wrap; margin-top: 8px; border: 1px solid #ccc; padding: 8px; border-radius: 4px;">
            ${unsafeHTML(this.licenciasResult)}
          </div>
        </sl-card>

          <sl-button href="${resolveRouterPath('about')}" variant="primary">Navigate to About</sl-button>
        </div>
      </main>
    `;
  }
}
