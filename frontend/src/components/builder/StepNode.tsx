import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { X, CheckSquare, Bell, Clock, ArrowLeftRight, Globe, GitBranch, Zap } from 'lucide-react'
import type { StepType } from '../../types'

export interface StepNodeData extends Record<string, unknown> {
  name: string
  type: StepType
  config: Record<string, unknown>
  approverEmail?: string
  onDelete: (id: string) => void
}

const typeStyles: Record<StepType, { bg: string; border: string; text: string; icon: React.ReactNode }> = {
  Action:        { bg: 'bg-blue-50',   border: 'border-blue-300',   text: 'text-blue-700',   icon: <Zap size={14} /> },
  Approval:      { bg: 'bg-orange-50', border: 'border-orange-300', text: 'text-orange-700', icon: <CheckSquare size={14} /> },
  Notification:  { bg: 'bg-purple-50', border: 'border-purple-300', text: 'text-purple-700', icon: <Bell size={14} /> },
  Delay:         { bg: 'bg-gray-50',   border: 'border-gray-300',   text: 'text-gray-600',   icon: <Clock size={14} /> },
  DataTransform: { bg: 'bg-teal-50',   border: 'border-teal-300',   text: 'text-teal-700',   icon: <ArrowLeftRight size={14} /> },
  Webhook:       { bg: 'bg-green-50',  border: 'border-green-300',  text: 'text-green-700',  icon: <Globe size={14} /> },
  Condition:     { bg: 'bg-yellow-50', border: 'border-yellow-300', text: 'text-yellow-700', icon: <GitBranch size={14} /> },
}

function StepNodeComponent({ id, data, selected }: NodeProps) {
  const nodeData = data as StepNodeData
  const style = typeStyles[nodeData.type] ?? typeStyles.Action

  return (
    <div
      className={`relative w-56 rounded-xl border-2 shadow-sm transition-shadow ${style.bg} ${style.border} ${
        selected ? 'shadow-lg ring-2 ring-blue-400 ring-offset-1' : ''
      }`}
    >
      <Handle type="target" position={Position.Top} className="!w-3 !h-3 !bg-gray-400 !border-2 !border-white" />

      <div className="p-3">
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-center gap-1.5 min-w-0">
            <span className={style.text}>{style.icon}</span>
            <span className="text-xs font-semibold text-gray-500 uppercase tracking-wide">{nodeData.type}</span>
          </div>
          <button
            onClick={(e) => { e.stopPropagation(); nodeData.onDelete(id) }}
            className="flex-shrink-0 p-0.5 text-gray-300 hover:text-red-400 rounded transition-colors"
          >
            <X size={12} />
          </button>
        </div>
        <p className="mt-1.5 font-medium text-sm text-gray-900 truncate">{nodeData.name}</p>
        {nodeData.approverEmail && (
          <p className="mt-0.5 text-xs text-gray-400 truncate">{nodeData.approverEmail}</p>
        )}
        {nodeData.type === 'Delay' && nodeData.config.delaySeconds && (
          <p className="mt-0.5 text-xs text-gray-400">{String(nodeData.config.delaySeconds)}s delay</p>
        )}
        {nodeData.type === 'Webhook' && nodeData.config.url && (
          <p className="mt-0.5 text-xs text-gray-400 truncate">{String(nodeData.config.url)}</p>
        )}
      </div>

      <Handle type="source" position={Position.Bottom} className="!w-3 !h-3 !bg-gray-400 !border-2 !border-white" />
    </div>
  )
}

export const StepNode = memo(StepNodeComponent)
