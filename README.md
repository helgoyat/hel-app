# HelApp

This workspace is split into:

- `Backend`: ASP.NET Core Web API
- `Frontend`: Vue 3 app (Vite)

## Run both from root

```bash
npm install
npm run install:frontend
npm run dev
```

This starts:

- backend at `http://localhost:5001`
- frontend at `http://localhost:5173`

## Auth0 setup

The app now requires authentication and permissions on `/api/todos` endpoints:
- `read:todos` for `GET /api/todos`
- `write:todos` for `POST`, `PUT`, `DELETE /api/todos`

1. In Auth0, create:
	- one **Single Page Application** (for Vue frontend)
	- one **API** (for backend audience)
2. In your Auth0 API:
	- Enable **RBAC**
	- Enable **Add Permissions in the Access Token**
	- Create permissions: `read:todos`, `write:todos`
3. Assign permissions to users:
	- create a role (for example `todo-user`) with `read:todos` + `write:todos`
	- assign this role to the user account you use for login
4. Configure your SPA in Auth0:
	- **Allowed Callback URLs**: `http://localhost:5173`
	- **Allowed Logout URLs**: `http://localhost:5173`
	- **Allowed Web Origins**: `http://localhost:5173`
5. Configure frontend env:
	- copy `Frontend/.env.example` to `Frontend/.env`
	- set `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`
6. Configure backend settings in `Backend/appsettings.json`:

```json
"Auth0": {
  "Domain": "your-tenant.eu.auth0.com",
  "Audience": "https://helapp-api"
}
```

You can also set these via environment variables:
- `Auth0__Domain`
- `Auth0__Audience`

## SQLite database (for DB Browser)

- DB file path: `Backend/Data/helapp.db`
- The file is created automatically when backend starts.
- In DB Browser for SQLite, use **Open Database** and select `Backend/Data/helapp.db`.

## Run backend

```bash
cd Backend
dotnet run --urls http://localhost:5001
```

## Run frontend

```bash
cd Frontend
npm install
npm run dev
```

Then open `http://localhost:5173`.
