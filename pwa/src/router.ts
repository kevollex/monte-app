// docs for router https://github.com/thepassle/app-tools/blob/master/router/README.md

import { html } from 'lit';

if (!(globalThis as any).URLPattern) {
  await import("urlpattern-polyfill");
}

import { Router } from '@thepassle/app-tools/router.js';

// @ts-ignore
import { title } from '@thepassle/app-tools/router/plugins/title.js';

import './pages/app-home.js';
import './pages/app-login.js';
import './pages/subsystem-view.js';

const baseURL: string = (import.meta as any).env.BASE_URL;

const routes = [
  {
    path: resolveRouterPath(),
    title: 'Home',
    render: () => html`<app-home></app-home>`
  },
  {
    path: resolveRouterPath('login'),
    title: 'Login',
    plugins: [
      {
        name: 'CheckAuthenticationForLoginPageGuard',
        shouldNavigate: () => ({
          condition: () => {
              const jwt = localStorage.getItem('jwt');
              if (!jwt) {
                return true;
              }
              return false;
          },
          redirect: resolveRouterPath('home'),
        })
      }
    ],
    render: () => html`<app-login></app-login>`,
  },
  {
    path: resolveRouterPath('subsystem'),
    title: 'Submódulo',
    render: () => html`<subsystem-view></subsystem-view>`,
  },
];

  export const router = new Router({
      routes,
      plugins : [
        {
          name: 'CheckAuthenticationForAllPagesGuard',
          shouldNavigate: (context) => ({
            condition: () => {
              const url = new URL(context.url, window.location.origin);
              if (url.pathname.endsWith('login')) return true;
              const jwt = localStorage.getItem('jwt');
              if (!jwt) {
                return false;
              }
              return true;
            },
            redirect: resolveRouterPath('login'),
          }),
        }
      ]
  });

  // This function will resolve a path with whatever Base URL was passed to the vite build process.
  // Use of this function throughout the starter is not required, but highly recommended, especially if you plan to use GitHub Pages to deploy.
  // If no arg is passed to this function, it will return the base URL.

  export function resolveRouterPath(unresolvedPath?: string) {
    var resolvedPath = baseURL;
    if(unresolvedPath) {
      resolvedPath = resolvedPath + unresolvedPath;
    }

    return resolvedPath;
  }
