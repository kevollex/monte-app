import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';

@customElement('card-app')
export class CardApp extends LitElement {
    @property() label = '';
    @property() color = '#ccc';
    @property() textColor = '#000';
    @property() icon = ''; // nombre del ícono (opcional)
    @property() route = ''; // para navegación futura

    static styles = css`
    .card {
        width: 140px;
        height: 120px;
        border-radius: 16px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
        font-weight: bold;
        font-size: 16px;
        cursor: pointer;
        transition: transform 0.2s ease;
        text-align: center;
    }

    .card:hover {
        transform: scale(1.05);
    }

    .icon {
        font-size: 24px;
        margin-bottom: 8px;
    }
    `;

    render() {
    return html`
        <div class="card" style="background:${this.color}; color:${this.textColor};">
        <div class="icon">${this.icon}</div>
        <div>${this.label}</div>
    </div>
    `;
    }
}