# MonteApp

Progressive Web App and its companion API

## Structure

- api - NET 9 Aspire REST API for all backend purposes
- pwa - Node, Typescript and Vite PWA for the end-user

## Development

### Requirements

- Docker engine
- NET 9
- Node 20+
  - `npm i -g @pwabuilder/cli`

### api

```bash
cd api
dotnet run --project MonteApp.AppHost
```

### pwa

```bash
cd pwa
npm install
pwa start
```
