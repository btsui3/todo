import type { Task } from '../api'

// A task is overdue if its due date is before *today in the user's own
// timezone* and it isn't done yet. We compare against the local calendar date
// (not UTC) so a deadline doesn't flip "overdue" a day early/late near midnight.
export function isOverdue(task: Task): boolean {
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
export function formatDueDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}
