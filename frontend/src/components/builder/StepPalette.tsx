import { Zap, CheckSquare, Bell, Clock, ArrowLeftRight, Globe, GitBranch } from 'lucide-react'
import type { StepType } from '../../types'

const STEP_TYPES: { type: StepType; label: string; icon: React.ReactNode; description: string }[] = [
  { type: 'Action',        label: 'Action',        icon: <Zap size={16} />,           description: 'Run a custom action' },
  { type: 'Approval',      label: 'Approval',      icon: <CheckSquare size={16} />,   description: 'Wait for human approval' },
  { type: 'Notification',  label: 'Notification',  icon: <Bell size={16} />,          description: 'Send email or Slack' },
  { type: 'Delay',         label: 'Delay',         icon: <Clock size={16} />,         description: 'Wait for a duration' },
  { type: 'DataTransform', label: 'Transform',     icon: <ArrowLeftRight size={16} />,description: 'Map or transform data' },
  { type: 'Webhook',       label: 'Webhook',       icon: <Globe size={16} />,         description: 'Call external HTTP endpoint' },
  { type: 'Condition',     label: 'Condition',     icon: <GitBranch size={16} />,     description: 'Branch on a condition' },
]

const typeColor: Record<StepType, string> = {
  Action:        'bg-blue-100 text-blue-700 border-blue-200',
  Approval:      'bg-orange-100 text-orange-700 border-orange-200',
  Notification:  'bg-purple-100 text-purple-700 border-purple-200',
  Delay:         'bg-gray-100 text-gray-600 border-gray-200',
  DataTransform: 'bg-teal-100 text-teal-700 border-teal-200',
  Webhook:       'bg-green-100 text-green-700 border-green-200',
  Condition:     'bg-yellow-100 text-yellow-700 border-yellow-200',
}

interface StepPaletteProps {
  onDragStart: (e: React.DragEvent, type: StepType) => void
}

export function StepPalette({ onDragStart }: StepPaletteProps) {
  return (
    <div className="w-52 bg-white border-r border-gray-200 flex flex-col h-full">
      <div className="px-4 py-3 border-b border-gray-200">
        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Step Types</p>
        <p className="text-xs text-gray-400 mt-0.5">Drag onto canvas</p>
      </div>
      <div className="flex-1 overflow-y-auto p-3 space-y-2">
        {STEP_TYPES.map(({ type, label, icon, description }) => (
          <div
            key={type}
            draggable
            onDragStart={(e) => onDragStart(e, type)}
            className={`flex items-start gap-2.5 p-2.5 rounded-lg border cursor-grab active:cursor-grabbing select-none hover:shadow-sm transition-shadow ${typeColor[type]}`}
          >
            <span className="flex-shrink-0 mt-0.5">{icon}</span>
            <div>
              <p className="text-xs font-semibold leading-tight">{label}</p>
              <p className="text-xs opacity-70 leading-tight mt-0.5">{description}</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
