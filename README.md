# MonteApp

Progressive Web App and its companion API

## Structure

- api - NET 9 Aspire REST API for all backend purposes
- pwa - Node, Typescript and Vite PWA for the end-user

## Development

### Requirements

- Docker engine
- NET 9
- Aspire CLI
  - After NET 9 SDK installation. Run `dotnet tool install --global aspire.cli --prerelease`
- Node 20+
- PWA Builder (optional)
  - Install with `npm i -g @pwabuilder/cli`

### SSL setup

HTTPS mandatory. Trust NET's and generate yours.
```bash
dotnet dev-certs https --trust
mkdir cert
sudo apt install mkcert OR brew install mkcert   # If you have Homebrew
mkcert -install
mkcert -key-file cert/key.pem -cert-file cert/cert.pem localhost
```

### api

```bash
cd api
dotnet run --project MonteApp.AppHost
```

Once run go to the Aspire Dashboard URL shown on the command line output. All MonteApp resources are displayed here, including the `pwa` application.

### pwa

To run the PWA standalone. Run `npm install` the first time.

```bash
cd pwa
npm start
```

## Deployment

Docker compose based deploy with an Aspire CLI generated definition.

> Work in progress. Generated definition errors at the moment. Missing manifest and other configurations apparently.

```bash
cd api/MonteApp.AppHost
aspire publish -o docker-compose-artifacts
cd docker-compose-artifacts
docker compose up
```
