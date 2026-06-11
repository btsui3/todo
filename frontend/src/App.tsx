import { useState } from 'react'
import { getToken, login, register, setToken, type Task } from './api'
import { useTasks } from './useTasks'

// Seeded demo account password, pre-filled on the sign-in screen. Kept in sync
// with SeedData.DemoPassword in the backend.
const DEMO_PASSWORD = 'Todo-Demo-Acct-2026!'

function App() {
  const [token, setTok] = useState<string | null>(getToken())

  function handleLogin(newToken: string) {
    setToken(newToken)
    setTok(newToken)
  }

  function handleLogout() {
    setToken(null)
    setTok(null)
  }

  return token
    ? <TaskList token={token} onLogout={handleLogout} />
    : <AuthForm onLogin={handleLogin} />
}

// Sign-in and sign-up share one form; a link toggles between them.
function AuthForm({ onLogin }: { onLogin: (token: string) => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('demo@todo.app')
  const [password, setPassword] = useState(DEMO_PASSWORD)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const isLogin = mode === 'login'

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    // Guard against a duplicate submit (e.g. a password manager auto-submitting),
    // which would otherwise blank the error message mid-request.
    if (submitting) return
    setSubmitting(true)
    try {
      const token = isLogin ? await login(email, password) : await register(email, password)
      onLogin(token) // register auto-logs in: both paths return a JWT
    } catch (err) {
      // Set the error only on failure (never cleared optimistically), so the
      // message persists until the next real attempt.
      setError(err instanceof Error ? err.message : 'Something went wrong')
    } finally {
      setSubmitting(false)
    }
  }

  function switchMode() {
    setError(null)
    if (isLogin) {
      setMode('register')
      setEmail('')
      setPassword('')
    } else {
      setMode('login')
      setEmail('demo@todo.app')
      setPassword(DEMO_PASSWORD)
    }
  }

  return (
    <main>
      <h1 className="brand-title">To-Do — Task Management</h1>
      <h2 className="subtitle">{isLogin ? 'Sign in' : 'Create account'}</h2>
      <form onSubmit={submit} className="stack">
        <input
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Email"
          type="email"
          name="email"
          autoComplete="email"
        />
        <input
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Password"
          type="password"
          name="password"
          autoComplete={isLogin ? 'current-password' : 'new-password'}
        />
        <button type="submit" disabled={submitting}>
          {isLogin ? 'Log in' : 'Sign up'}
        </button>
      </form>
      {error && <p className="error">{error}</p>}
      <p className="muted">
        {isLogin ? 'Need an account? ' : 'Already have an account? '}
        <button type="button" onClick={switchMode} className="link-button">
          {isLogin ? 'Sign up' : 'Log in'}
        </button>
      </p>
      {isLogin && <p className="muted">Demo account is pre-filled.</p>}
    </main>
  )
}

function TaskList({ token, onLogout }: { token: string; onLogout: () => void }) {
  const { tasks, error, addTask, toggleTask, removeTask } = useTasks(token, onLogout)
  const [title, setTitle] = useState('')
  const [dueDate, setDueDate] = useState('')

  async function add(e: React.FormEvent) {
    e.preventDefault()
    if (await addTask(title, dueDate || null)) {
      setTitle('')
      setDueDate('')
    }
  }

  return (
    <main>
      <div className="row-between">
        <h1>Tasks</h1>
        <button className="btn-quiet" onClick={onLogout}>Log out</button>
      </div>

      <form onSubmit={add} className="add-form">
        <input
          className="grow"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Add a task…"
        />
        <input
          type="date"
          value={dueDate}
          onChange={(e) => setDueDate(e.target.value)}
          aria-label="Due date (optional)"
        />
        <button type="submit">Add</button>
      </form>

      {error && <p className="error">{error}</p>}

      <ul className="task-list">
        {tasks.map((task) => (
          <li key={task.id} className="task-item">
            <input type="checkbox" checked={task.isComplete} onChange={() => toggleTask(task)} />
            <span className={`task-title${task.isComplete ? ' done' : ''}`}>{task.title}</span>
            {task.dueDate && (
              <span className={`due${isOverdue(task) ? ' overdue' : ''}`}>
                Due {formatDueDate(task.dueDate)}{isOverdue(task) ? ' (overdue)' : ''}
              </span>
            )}
            <button className="btn-quiet" onClick={() => removeTask(task)} aria-label={`Delete ${task.title}`}>✕</button>
          </li>
        ))}
        {tasks.length === 0 && <li className="muted">No tasks yet — add one above.</li>}
      </ul>
    </main>
  )
}

// A task is overdue if its due date is before *today in the user's own
// timezone* and it isn't done yet. We compare against the local calendar date
// (not UTC) so a deadline doesn't flip "overdue" a day early/late near midnight.
function isOverdue(task: Task): boolean {
  if (!task.dueDate || task.isComplete) return false
  return task.dueDate < todayLocal()
}

// Today's date as YYYY-MM-DD in the user's local timezone. Built from local
// parts on purpose — `new Date().toISOString()` would give the UTC date.
function todayLocal(): string {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

// Show the stored ISO date in the user's locale, e.g. "Jul 1, 2026".
// Build the Date from parts so it stays on the intended calendar day (avoids
// the UTC-midnight off-by-one you get from `new Date("2026-07-01")`).
function formatDueDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export default App
