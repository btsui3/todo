import { useEffect, useState } from 'react'
import { createTask, deleteTask, getTasks, updateTask, type Task } from './api'

export function useTasks(token: string, onUnauthorized: () => void) {
  const [tasks, setTasks] = useState<Task[]>([])
  const [error, setError] = useState<string | null>(null)

  function handleError(err: unknown) {
    if (err instanceof Error && err.message === 'Unauthorized') onUnauthorized()
    else setError(err instanceof Error ? err.message : 'Something went wrong')
  }

  useEffect(() => {
    getTasks(token).then(setTasks).catch(handleError)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token])

  async function addTask(title: string, dueDate: string | null): Promise<boolean> {
    setError(null)
    try {
      const created = await createTask(token, title, dueDate)
      setTasks((prev) => [created, ...prev])
      return true
    } catch (err) {
      handleError(err)
      return false
    }
  }

  async function toggleTask(task: Task) {
    setError(null)
    try {
      const updated = await updateTask(token, task.id, task.title, !task.isComplete, task.dueDate)
      setTasks((prev) => prev.map((t) => (t.id === updated.id ? updated : t)))
    } catch (err) {
      handleError(err)
    }
  }

  async function removeTask(task: Task) {
    setError(null)
    try {
      await deleteTask(token, task.id)
      setTasks((prev) => prev.filter((t) => t.id !== task.id))
    } catch (err) {
      handleError(err)
    }
  }

  return { tasks, error, addTask, toggleTask, removeTask }
}
