<script setup>
import { onMounted, ref, watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'

const { isAuthenticated, isLoading, user, loginWithRedirect, logout, getAccessTokenSilently } = useAuth0()

const todos = ref([])
const newTitle = ref('')
const loading = ref(false)
const error = ref('')
const savingId = ref(null)
const writeAccessDenied = ref(false)

function getErrorMessageByStatus(status, fallbackMessage) {
  if (status === 403) {
    return 'Permission denied (403). Your account is missing the required Auth0 permission.'
  }

  if (status === 401) {
    return 'Authentication required (401). Please log in again.'
  }

  return fallbackMessage
}

async function apiFetch(path, options = {}) {
  const accessToken = await getAccessTokenSilently()

  return fetch(path, {
    ...options,
    headers: {
      ...(options.headers ?? {}),
      Authorization: `Bearer ${accessToken}`
    }
  })
}

async function loadTodos() {
  if (!isAuthenticated.value) {
    todos.value = []
    return
  }

  loading.value = true
  error.value = ''

  try {
    const response = await apiFetch('/api/todos')
    if (!response.ok) {
      error.value = getErrorMessageByStatus(response.status, 'Failed to load todos.')
      return
    }

    const data = await response.json()
    todos.value = data
  } catch {
    error.value = 'Unable to reach backend.'
  } finally {
    loading.value = false
  }
}

async function createTodo() {
  if (writeAccessDenied.value) {
    return
  }

  const title = newTitle.value.trim()
  if (!title) {
    return
  }

  error.value = ''

  try {
    const response = await apiFetch('/api/todos', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title, isDone: false })
    })

    if (!response.ok) {
      if (response.status === 403) {
        writeAccessDenied.value = true
      }

      error.value = getErrorMessageByStatus(response.status, 'Could not create todo.')
      return
    }

    newTitle.value = ''
    await loadTodos()
  } catch {
    if (!error.value) {
      error.value = 'Could not create todo.'
    }
  }
}

async function updateTodo(todo) {
  if (writeAccessDenied.value) {
    return
  }

  const title = todo.title.trim()
  if (!title) {
    error.value = 'Todo title cannot be empty.'
    await loadTodos()
    return
  }

  savingId.value = todo.id
  error.value = ''

  try {
    const response = await apiFetch(`/api/todos/${todo.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title, isDone: todo.isDone })
    })

    if (!response.ok) {
      if (response.status === 403) {
        writeAccessDenied.value = true
      }

      error.value = getErrorMessageByStatus(response.status, 'Could not update todo.')
      return
    }
  } catch {
    if (!error.value) {
      error.value = 'Could not update todo.'
    }
  } finally {
    savingId.value = null
  }
}

async function deleteTodo(id) {
  if (writeAccessDenied.value) {
    return
  }

  error.value = ''

  try {
    const response = await apiFetch(`/api/todos/${id}`, {
      method: 'DELETE'
    })

    if (!response.ok) {
      if (response.status === 403) {
        writeAccessDenied.value = true
      }

      error.value = getErrorMessageByStatus(response.status, 'Could not delete todo.')
      return
    }

    todos.value = todos.value.filter(todo => todo.id !== id)
  } catch {
    if (!error.value) {
      error.value = 'Could not delete todo.'
    }
  }
}

onMounted(async () => {
  if (isAuthenticated.value) {
    await loadTodos()
  }
})

watch(isAuthenticated, async authenticated => {
  if (authenticated) {
    writeAccessDenied.value = false
    await loadTodos()
  } else {
    writeAccessDenied.value = false
    todos.value = []
  }
})
</script>

<template>
  <main>
    <h1>HelApp</h1>

    <template v-if="isLoading">
      <p>Checking authentication...</p>
    </template>

    <template v-else-if="!isAuthenticated">
      <p>Please sign in to manage your todos.</p>
      <button type="button" @click="loginWithRedirect()">Log in with Auth0</button>
    </template>

    <template v-else>
      <p>
        Signed in as {{ user?.name || user?.email || 'user' }}
      </p>
      <button
        type="button"
        @click="logout({ logoutParams: { returnTo: window.location.origin } })"
      >
        Log out
      </button>

      <form @submit.prevent="createTodo">
        <input
          v-model="newTitle"
          type="text"
          placeholder="New todo"
          :disabled="writeAccessDenied"
        />
        <button type="submit" :disabled="writeAccessDenied">Add</button>
      </form>

      <p v-if="loading">Loading...</p>
      <p v-if="error">{{ error }}</p>
      <p v-if="writeAccessDenied">
        Read-only mode: your account can view todos, but cannot modify them. Ask an admin for the `write:todos` permission.
      </p>

      <ul>
        <li v-for="todo in todos" :key="todo.id">
          <input
            :id="`todo-done-${todo.id}`"
            v-model="todo.isDone"
            type="checkbox"
            :disabled="writeAccessDenied"
          />

          <input
            :id="`todo-title-${todo.id}`"
            v-model="todo.title"
            type="text"
            :disabled="writeAccessDenied"
          />

          <button
            type="button"
            :disabled="savingId === todo.id || writeAccessDenied"
            @click="updateTodo(todo)"
          >
            Save
          </button>

          <button
            type="button"
            :disabled="savingId === todo.id || writeAccessDenied"
            @click="deleteTodo(todo.id)"
          >
            Delete
          </button>
        </li>
      </ul>
    </template>
  </main>
</template>
