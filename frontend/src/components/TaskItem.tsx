import type { Task } from '../api'
import { formatDueDate, isOverdue } from '../lib/dates'

// A single row in the task list: complete toggle, title, optional due date, delete.
export function TaskItem({
  task,
  onToggle,
  onRemove,
}: {
  task: Task
  onToggle: (task: Task) => void
  onRemove: (task: Task) => void
}) {
  return (
    <li className="task-item">
      <input type="checkbox" checked={task.isComplete} onChange={() => onToggle(task)} />
      <span className={`task-title${task.isComplete ? ' done' : ''}`}>{task.title}</span>
      {task.dueDate && (
        <span className={`due${isOverdue(task) ? ' overdue' : ''}`}>
          Due {formatDueDate(task.dueDate)}{isOverdue(task) ? ' (overdue)' : ''}
        </span>
      )}
      <button className="btn-quiet" onClick={() => onRemove(task)} aria-label={`Delete ${task.title}`}>✕</button>
    </li>
  )
}
