// Thin wrapper around the backend API. All calls use relative URLs; Vite proxies
// /api to the backend in dev (see vite.config.ts).

export interface Task {
  id: string
  title: string
  isComplete: boolean
  dueDate: string | null
  createdAt: string
}

const TOKEN_KEY = 'todo_token'

export const getToken = () => localStorage.getItem(TOKEN_KEY)

export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token)
  else localStorage.removeItem(TOKEN_KEY)
}

export const login = (email: string, password: string) =>
  postAuth('/api/auth/login', email, password)

export const register = (email: string, password: string) =>
  postAuth('/api/auth/register', email, password)

async function postAuth(path: string, email: string, password: string): Promise<string> {
  const res = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  if (!res.ok) throw new Error(await errorMessage(res))
  const { token } = await res.json()
  return token
}

export const getTasks = (token: string) =>
  send<Task[]>(token, 'GET', '/api/tasks')

export const createTask = (token: string, title: string, dueDate: string | null) =>
  send<Task>(token, 'POST', '/api/tasks', { title, dueDate })

export const updateTask = (
  token: string,
  id: string,
  title: string,
  isComplete: boolean,
  dueDate: string | null,
) => send<Task>(token, 'PUT', `/api/tasks/${id}`, { title, isComplete, dueDate })

export const deleteTask = (token: string, id: string) =>
  send<void>(token, 'DELETE', `/api/tasks/${id}`)

async function send<T>(token: string, method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(path, {
    method,
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (res.status === 401) throw new Error('Unauthorized')
  if (!res.ok) throw new Error(await errorMessage(res))
  return res.status === 204 ? (undefined as T) : res.json()
}

async function errorMessage(res: Response): Promise<string> {
  const body = await res.json().catch(() => null)
  const fieldErrors = body?.errors as Record<string, string[]> | undefined
  const all = fieldErrors ? Object.values(fieldErrors).flat() : []
  if (all.length) return all.join(' ')
  return body?.detail ?? body?.error ?? body?.title ?? `Request failed: ${res.status}`
}
