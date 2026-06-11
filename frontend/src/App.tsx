import { useState } from 'react'
import { getToken, setToken } from './api'
import { AuthForm } from './components/AuthForm'
import { TaskList } from './components/TaskList'

// Root: holds the auth token and shows the task list when signed in, the auth
// form otherwise.
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

export default App
