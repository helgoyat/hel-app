import { createApp } from 'vue'
import { createAuth0 } from '@auth0/auth0-vue'
import App from './App.vue'

const app = createApp(App)

app.use(
	createAuth0({
		domain: import.meta.env.VITE_AUTH0_DOMAIN,
		clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
		authorizationParams: {
			audience: import.meta.env.VITE_AUTH0_AUDIENCE,
			redirect_uri: window.location.origin,
			scope: 'openid profile email read:todos write:todos'
		},
		cacheLocation: 'localstorage',
		useRefreshTokens: true
	})
)

app.mount('#app')
