import { useState } from 'react'
import { X, Loader2 } from 'lucide-react'
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query'
import { automationRulesApi } from '../../api/automationRules'
import { workflowsApi } from '../../api/workflows'
import type { TriggerType } from '../../types'

const TRIGGER_TYPES: { value: TriggerType; label: string; hint: string }[] = [
  { value: 'Manual', label: 'Manual', hint: 'Triggered on demand' },
  { value: 'Schedule', label: 'Schedule', hint: 'Run on a cron schedule' },
  { value: 'Webhook', label: 'Webhook', hint: 'Triggered by an incoming HTTP request' },
  { value: 'Event', label: 'Event', hint: 'Triggered by an internal event' },
  { value: 'Condition', label: 'Condition', hint: 'Triggered when a condition is met' },
]

interface Props {
  onClose: () => void
}

export function NewRuleModal({ onClose }: Props) {
  const qc = useQueryClient()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [triggerType, setTriggerType] = useState<TriggerType>('Manual')
  const [triggerConfig, setTriggerConfig] = useState('')
  const [workflowId, setWorkflowId] = useState('')
  const [error, setError] = useState('')

  const { data: workflows } = useQuery({
    queryKey: ['workflows-list-all'],
    queryFn: () => workflowsApi.list({ pageSize: 100 }),
  })

  const create = useMutation({
    mutationFn: automationRulesApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['automation-rules'] })
      onClose()
    },
    onError: (err) => setError(err instanceof Error ? err.message : 'Failed to create rule'),
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    const config = triggerType === 'Schedule' && triggerConfig.trim()
      ? JSON.stringify({ cron: triggerConfig.trim() })
      : triggerConfig || '{}'
    create.mutate({ name, description, triggerType, triggerConfig: config, workflowId })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="text-lg font-semibold text-gray-900">New Automation Rule</h2>
          <button onClick={onClose} className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors">
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-5 space-y-4">
          {error && (
            <div className="px-3 py-2.5 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">{error}</div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              Rule name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              autoFocus
              placeholder="e.g. Daily invoice sync"
              className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              Description <span className="text-gray-400 font-normal">(optional)</span>
            </label>
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="What does this rule do?"
              className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              Trigger type <span className="text-red-500">*</span>
            </label>
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
              {TRIGGER_TYPES.map(({ value, label, hint }) => (
                <button
                  key={value}
                  type="button"
                  onClick={() => setTriggerType(value)}
                  className={`text-left px-3 py-2.5 rounded-lg border text-sm transition-colors ${
                    triggerType === value
                      ? 'border-blue-500 bg-blue-50 text-blue-700'
                      : 'border-gray-200 hover:border-gray-300 text-gray-700'
                  }`}
                >
                  <div className="font-medium">{label}</div>
                  <div className="text-xs text-gray-400 mt-0.5">{hint}</div>
                </button>
              ))}
            </div>
          </div>

          {triggerType === 'Schedule' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Cron expression</label>
              <input
                type="text"
                value={triggerConfig}
                onChange={(e) => setTriggerConfig(e.target.value)}
                placeholder="0 9 * * 1-5  (weekdays at 9am)"
                className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
          )}

          {triggerType === 'Webhook' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Webhook secret <span className="text-gray-400 font-normal">(optional)</span></label>
              <input
                type="text"
                value={triggerConfig}
                onChange={(e) => setTriggerConfig(e.target.value)}
                placeholder="Secret key for HMAC verification"
                className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              Workflow <span className="text-red-500">*</span>
            </label>
            <select
              value={workflowId}
              onChange={(e) => setWorkflowId(e.target.value)}
              required
              className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white"
            >
              <option value="">Select a workflow…</option>
              {workflows?.items.map((wf) => (
                <option key={wf.id} value={wf.id}>{wf.name}</option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-3 pt-1">
            <button
              type="submit"
              disabled={create.isPending || !name.trim() || !workflowId}
              className="flex items-center gap-2 px-5 py-2.5 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {create.isPending && <Loader2 size={15} className="animate-spin" />}
              {create.isPending ? 'Creating…' : 'Create rule'}
            </button>
            <button type="button" onClick={onClose} className="px-4 py-2.5 text-sm text-gray-600 hover:text-gray-900 transition-colors">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
