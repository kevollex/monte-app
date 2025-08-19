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
sudo apt install mkcert OR brew install mkcert   # If you have Homebrew
cd pwa
mkdir cert
mkcert -install
mkcert -key-file cert/key.pem -cert-file cert/cert.pem localhost

dotnet dev-certs https --trust
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
## Firebase configuration

Firebase is used in MonteApp to support push notifications and real-time functionality in both the backend and frontend.

## Backend (MonteApp.NotificationWorker)

The backend uses Firebase Admin SDK to send push notifications via FCM.
	•	Store the real service account file as service-account.json in the root of the MonteApp.NotificationWorker project.
	•	Do not commit this file.
	•	A sample file is provided as reference: service-account.json.example

## Frontend (pwa)

The PWA uses Firebase Messaging to receive push notifications in the browser.

Environment variables

Add the following variables to your .env file:
	•	VITE_VAPID_KEY

These values are used in firebase.ts and app-login.ts to register service workers and request FCM tokens.

A sample environment file is provided: pwa/.env.example

## How to get the credentials

Backend – service-account.json
	1.	Go to Firebase Console
	2.	Select your project > ⚙️ Settings
	3.	Go to the Service accounts tab
	4.	Click Generate new private key
	5.	Save the file as service-account.json in api/MonteApp.NotificationWorker

Frontend – VAPID_KEY
	1.	In Firebase Console, go to Project Settings > Cloud Messaging
	2.	Scroll down to Web configuration
	3.	Copy the public VAPID key
	4.	Paste it into your .env file as VITE_VAPID_KEY