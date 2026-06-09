import { useState } from 'react'
import { Plus, Trash2, Zap, Copy, Check } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { automationRulesApi } from '../api/automationRules'
import { Badge } from '../components/ui/Badge'
import { formatDistanceToNow } from 'date-fns'
import { NewRuleModal } from '../components/automationRules/NewRuleModal'

function WebhookUrl({ token }: { token: string }) {
  const [copied, setCopied] = useState(false)
  const url = `${window.location.origin}/api/webhooks/${token}`
  const copy = () => {
    navigator.clipboard.writeText(url)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }
  return (
    <div className="flex items-center gap-2 mt-2 px-3 py-2 bg-gray-50 border border-gray-200 rounded-lg max-w-xl">
      <span className="text-xs font-mono text-gray-600 truncate flex-1">{url}</span>
      <button onClick={copy} className="shrink-0 text-gray-400 hover:text-gray-700 transition-colors">
        {copied ? <Check size={14} className="text-green-600" /> : <Copy size={14} />}
      </button>
    </div>
  )
}

export function AutomationRules() {
  const qc = useQueryClient()
  const [showNewModal, setShowNewModal] = useState(false)
  const { data, isLoading } = useQuery({
    queryKey: ['automation-rules'],
    queryFn: () => automationRulesApi.list(),
  })

  const enable = useMutation({
    mutationFn: automationRulesApi.enable,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-rules'] }),
  })
  const disable = useMutation({
    mutationFn: automationRulesApi.disable,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-rules'] }),
  })
  const del = useMutation({
    mutationFn: automationRulesApi.delete,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-rules'] }),
  })

  return (
    <>
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Automation Rules</h1>
          <p className="text-gray-500 mt-1">Configure triggers that automatically start workflows</p>
        </div>
        <button
          onClick={() => setShowNewModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium"
        >
          <Plus size={16} /> New Rule
        </button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-12"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" /></div>
      ) : (
        <div className="grid gap-4">
          {data?.items.map((rule) => (
            <div key={rule.id} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
              <div className="flex items-start justify-between">
                <div className="flex items-start gap-3">
                  <div className={`p-2 rounded-lg ${rule.isEnabled ? 'bg-blue-50 text-blue-600' : 'bg-gray-100 text-gray-400'}`}>
                    <Zap size={18} />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="font-semibold text-gray-900">{rule.name}</h3>
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${rule.isEnabled ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                        {rule.isEnabled ? 'Enabled' : 'Disabled'}
                      </span>
                    </div>
                    {rule.description && <p className="text-sm text-gray-500 mt-0.5">{rule.description}</p>}
                    <div className="flex items-center gap-4 mt-2 text-xs text-gray-400">
                      <span>Trigger: <span className="font-medium text-gray-600">{rule.triggerType}</span></span>
                      <span>Workflow: <span className="font-medium text-gray-600">{rule.workflowName}</span></span>
                      <span>Runs: <span className="font-medium text-gray-600">{rule.executionCount}</span></span>
                      {rule.lastTriggeredAt && (
                        <span>Last: <span className="font-medium text-gray-600">{formatDistanceToNow(new Date(rule.lastTriggeredAt), { addSuffix: true })}</span></span>
                      )}
                    </div>
                    {rule.triggerType === 'Webhook' && rule.webhookToken && (
                      <WebhookUrl token={rule.webhookToken} />
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => rule.isEnabled ? disable.mutate(rule.id) : enable.mutate(rule.id)}
                    className={`px-3 py-1.5 text-xs rounded-lg border font-medium ${
                      rule.isEnabled
                        ? 'border-yellow-300 text-yellow-700 hover:bg-yellow-50'
                        : 'border-green-300 text-green-700 hover:bg-green-50'
                    }`}
                  >
                    {rule.isEnabled ? 'Disable' : 'Enable'}
                  </button>
                  <button
                    onClick={() => { if (confirm('Delete this rule?')) del.mutate(rule.id) }}
                    className="p-1.5 text-red-400 hover:bg-red-50 rounded-lg"
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              </div>
            </div>
          ))}
          {data?.items.length === 0 && (
            <div className="text-center py-12 text-gray-400 bg-white rounded-xl border border-gray-200">
              No automation rules yet. Create one to auto-trigger your workflows!
            </div>
          )}
        </div>
      )}
    </div>

      {showNewModal && <NewRuleModal onClose={() => setShowNewModal(false)} />}
    </>
  )
}
