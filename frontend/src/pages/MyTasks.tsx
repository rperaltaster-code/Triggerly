import { useNavigate } from 'react-router-dom'
import { ClipboardList, CheckCircle, Clock, ExternalLink } from 'lucide-react'
import { useMyTasks, useCompleteActionStep } from '../hooks/useExecutions'
import { formatDistanceToNow, isToday, isTomorrow, isPast } from 'date-fns'
import type { MyTask } from '../types'

function taskGroup(task: MyTask): 'overdue' | 'today' | 'upcoming' | 'no-due' {
  if (!task.dueAt) return 'no-due'
  const due = new Date(task.dueAt)
  if (isPast(due)) return 'overdue'
  if (isToday(due)) return 'today'
  return 'upcoming'
}

function DueBadge({ dueAt }: { dueAt: string | null }) {
  if (!dueAt) return null
  const due = new Date(dueAt)
  const overdue = isPast(due)
  const today = isToday(due)

  return (
    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
      overdue ? 'bg-red-100 text-red-700' :
      today ? 'bg-orange-100 text-orange-700' :
      'bg-gray-100 text-gray-600'
    }`}>
      {overdue ? 'Overdue' : today ? 'Due today' : isTomorrow(due) ? 'Due tomorrow' : formatDistanceToNow(due, { addSuffix: true })}
    </span>
  )
}

function TaskRow({ task, onComplete }: { task: MyTask; onComplete: (t: MyTask) => void }) {
  const navigate = useNavigate()
  const isAction = task.stepType === 'Action'

  return (
    <div className="flex items-center gap-4 px-5 py-4 hover:bg-gray-50 transition-colors">
      <div className="w-8 h-8 rounded-full bg-blue-50 flex items-center justify-center flex-shrink-0">
        {task.stepType === 'Approval'
          ? <Clock size={15} className="text-orange-500" />
          : <CheckCircle size={15} className="text-blue-500" />}
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-sm font-medium text-gray-900">{task.stepName}</span>
          <span className="text-xs px-1.5 py-0.5 bg-gray-100 text-gray-500 rounded font-mono">{task.stepType}</span>
        </div>
        <p className="text-xs text-gray-500 mt-0.5 truncate">
          {task.workflowName}
          {task.clientName && <> · <span className="font-medium">{task.clientName}</span></>}
          {task.serviceTypeName && <> — {task.serviceTypeName}</>}
        </p>
      </div>

      <div className="flex items-center gap-3 flex-shrink-0">
        <DueBadge dueAt={task.dueAt} />
        {isAction && (
          <button
            onClick={() => onComplete(task)}
            className="flex items-center gap-1.5 px-3 py-1.5 bg-green-600 text-white text-xs font-medium rounded-lg hover:bg-green-700"
          >
            <CheckCircle size={13} /> Mark done
          </button>
        )}
        <button
          onClick={() => navigate(`/executions/${task.executionId}`)}
          className="flex items-center gap-1.5 px-3 py-1.5 border border-gray-200 text-gray-600 text-xs rounded-lg hover:bg-gray-50"
        >
          <ExternalLink size={13} /> Open
        </button>
      </div>
    </div>
  )
}

function Section({ title, count, children }: { title: string; count: number; children: React.ReactNode }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
      <div className="px-5 py-3.5 border-b border-gray-100 flex items-center gap-2">
        <h2 className="font-semibold text-gray-800 text-sm">{title}</h2>
        <span className="px-2 py-0.5 bg-gray-100 text-gray-600 text-xs font-medium rounded-full">{count}</span>
      </div>
      <div className="divide-y divide-gray-100">{children}</div>
    </div>
  )
}

export function MyTasks() {
  const { data: tasks = [], isLoading } = useMyTasks()
  const complete = useCompleteActionStep()

  const handleComplete = (task: MyTask) => {
    complete.mutate({ executionId: task.executionId, stepId: task.stepId })
  }

  const overdue = tasks.filter((t) => taskGroup(t) === 'overdue')
  const today = tasks.filter((t) => taskGroup(t) === 'today')
  const upcoming = tasks.filter((t) => taskGroup(t) === 'upcoming')
  const noDue = tasks.filter((t) => taskGroup(t) === 'no-due')

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    )
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center gap-3">
        <ClipboardList size={22} className="text-gray-500" />
        <div>
          <h1 className="text-2xl font-bold text-gray-900">My Tasks</h1>
          <p className="text-gray-500 text-sm mt-0.5">
            {tasks.length} active task{tasks.length !== 1 ? 's' : ''} assigned to you
          </p>
        </div>
      </div>

      {tasks.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm px-6 py-16 text-center">
          <CheckCircle size={40} className="mx-auto text-green-400 mb-3" />
          <p className="font-medium text-gray-700">All clear</p>
          <p className="text-sm text-gray-400 mt-1">No tasks assigned to you right now.</p>
        </div>
      ) : (
        <>
          {overdue.length > 0 && (
            <Section title="Overdue" count={overdue.length}>
              {overdue.map((t) => (
                <TaskRow key={`${t.executionId}-${t.stepId}`} task={t} onComplete={handleComplete} />
              ))}
            </Section>
          )}
          {today.length > 0 && (
            <Section title="Due Today" count={today.length}>
              {today.map((t) => (
                <TaskRow key={`${t.executionId}-${t.stepId}`} task={t} onComplete={handleComplete} />
              ))}
            </Section>
          )}
          {upcoming.length > 0 && (
            <Section title="Upcoming" count={upcoming.length}>
              {upcoming.map((t) => (
                <TaskRow key={`${t.executionId}-${t.stepId}`} task={t} onComplete={handleComplete} />
              ))}
            </Section>
          )}
          {noDue.length > 0 && (
            <Section title="No due date" count={noDue.length}>
              {noDue.map((t) => (
                <TaskRow key={`${t.executionId}-${t.stepId}`} task={t} onComplete={handleComplete} />
              ))}
            </Section>
          )}
        </>
      )}
    </div>
  )
}
