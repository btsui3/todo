import { useState } from 'react'
import { login, register } from '../api'

// Seeded demo account password, pre-filled on the sign-in screen. Kept in sync
// with SeedData.DemoPassword in the backend.
const DEMO_PASSWORD = 'Todo-Demo-Acct-2026!'

// Sign-in and sign-up share one form; a link toggles between them.
export function AuthForm({ onLogin }: { onLogin: (token: string) => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('demo@todo.app')
  const [password, setPassword] = useState(DEMO_PASSWORD)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const isLogin = mode === 'login'

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    // Guard against a duplicate submit (e.g. a password manager auto-submitting),
    if (submitting) return
    setSubmitting(true)
    try {
      const token = isLogin ? await login(email, password) : await register(email, password)
      onLogin(token) // register auto-logs in: both paths return a JWT
    } catch (err) {
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
