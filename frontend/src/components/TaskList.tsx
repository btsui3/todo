import { useState } from 'react'
import { useTasks } from '../useTasks'
import { TaskItem } from './TaskItem'

export function TaskList({ token, onLogout }: { token: string; onLogout: () => void }) {
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
          <TaskItem key={task.id} task={task} onToggle={toggleTask} onRemove={removeTask} />
        ))}
        {tasks.length === 0 && <li className="muted">No tasks yet — add one above.</li>}
      </ul>
    </main>
  )
}
