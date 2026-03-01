import { createApp } from 'vue'
import { createAuth0 } from '@auth0/auth0-vue'
import App from './App.vue'

const auth0Domain = import.meta.env.VITE_AUTH0_DOMAIN
const auth0ClientId = import.meta.env.VITE_AUTH0_CLIENT_ID
const auth0Audience = import.meta.env.VITE_AUTH0_AUDIENCE

const missingConfig = []

if (!auth0Domain) {
	missingConfig.push('VITE_AUTH0_DOMAIN')
}

if (!auth0ClientId) {
	missingConfig.push('VITE_AUTH0_CLIENT_ID')
}

if (!auth0Audience) {
	missingConfig.push('VITE_AUTH0_AUDIENCE')
}

if (missingConfig.length > 0) {
	throw new Error(
		`Missing Auth0 frontend configuration: ${missingConfig.join(', ')}. Add them in Frontend/.env and restart Vite.`
	)
}

const app = createApp(App)

app.use(
	createAuth0({
		domain: auth0Domain,
		clientId: auth0ClientId,
		authorizationParams: {
			audience: auth0Audience,
			redirect_uri: window.location.origin,
			scope: 'openid profile email read:todos write:todos'
		},
		cacheLocation: 'localstorage'
	})
)

app.mount('#app')
