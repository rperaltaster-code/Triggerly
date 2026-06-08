import { useEffect, useState } from 'react'
import { X } from 'lucide-react'
import type { Node } from '@xyflow/react'
import type { StepNodeData } from './StepNode'

interface StepConfigPanelProps {
  node: Node | null
  onClose: () => void
  onUpdate: (nodeId: string, data: Partial<StepNodeData>) => void
}

export function StepConfigPanel({ node, onClose, onUpdate }: StepConfigPanelProps) {
  const nodeData = node?.data as StepNodeData | undefined
  const [name, setName] = useState('')
  const [config, setConfig] = useState<Record<string, unknown>>({})
  const [approverEmail, setApproverEmail] = useState('')

  useEffect(() => {
    if (nodeData) {
      setName(nodeData.name)
      setConfig(nodeData.config ?? {})
      setApproverEmail(nodeData.approverEmail ?? '')
    }
  }, [node?.id])

  if (!node || !nodeData) return null

  const handleSave = () => {
    onUpdate(node.id, {
      name,
      config,
      approverEmail: approverEmail || undefined,
    })
  }

  const setConfigField = (key: string, value: unknown) =>
    setConfig((prev) => ({ ...prev, [key]: value }))

  return (
    <div className="w-72 bg-white border-l border-gray-200 flex flex-col h-full shadow-lg">
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
        <p className="text-sm font-semibold text-gray-800">Configure Step</p>
        <button onClick={onClose} className="p-1 text-gray-400 hover:text-gray-600 rounded">
          <X size={16} />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">Step Name</label>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Type</label>
          <span className="text-sm text-gray-700">{nodeData.type}</span>
        </div>

        {/* Type-specific fields */}
        {nodeData.type === 'Approval' && (
          <>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Approver Email</label>
              <input
                type="email"
                value={approverEmail}
                onChange={(e) => setApproverEmail(e.target.value)}
                placeholder="approver@example.com"
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">SLA (hours)</label>
              <input
                type="number"
                value={String(config.slaHours ?? 72)}
                onChange={(e) => setConfigField('slaHours', Number(e.target.value))}
                min={1}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </>
        )}

        {nodeData.type === 'Notification' && (
          <>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Channel</label>
              <select
                value={String(config.channel ?? 'email')}
                onChange={(e) => setConfigField('channel', e.target.value)}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="email">Email</option>
                <option value="slack">Slack</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Recipient</label>
              <input
                value={String(config.recipient ?? '')}
                onChange={(e) => setConfigField('recipient', e.target.value)}
                placeholder="user@example.com"
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Message</label>
              <textarea
                value={String(config.message ?? '')}
                onChange={(e) => setConfigField('message', e.target.value)}
                rows={3}
                placeholder="Notification message..."
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              />
            </div>
          </>
        )}

        {nodeData.type === 'Delay' && (
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Delay (seconds)</label>
            <input
              type="number"
              value={String(config.delaySeconds ?? 60)}
              onChange={(e) => setConfigField('delaySeconds', Number(e.target.value))}
              min={1}
              className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        )}

        {nodeData.type === 'Webhook' && (
          <>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">URL</label>
              <input
                value={String(config.url ?? '')}
                onChange={(e) => setConfigField('url', e.target.value)}
                placeholder="https://example.com/webhook"
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Method</label>
              <select
                value={String(config.method ?? 'POST')}
                onChange={(e) => setConfigField('method', e.target.value)}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {['POST', 'GET', 'PUT', 'PATCH'].map((m) => (
                  <option key={m} value={m}>{m}</option>
                ))}
              </select>
            </div>
          </>
        )}

        {nodeData.type === 'Condition' && (
          <>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Field</label>
              <input
                value={String(config.field ?? '')}
                onChange={(e) => setConfigField('field', e.target.value)}
                placeholder="e.g. amount"
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Operator</label>
              <select
                value={String(config.operator ?? 'equals')}
                onChange={(e) => setConfigField('operator', e.target.value)}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {['equals', 'not-equals', 'contains', 'greater-than', 'less-than'].map((op) => (
                  <option key={op} value={op}>{op}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Value</label>
              <input
                value={String(config.value ?? '')}
                onChange={(e) => setConfigField('value', e.target.value)}
                placeholder="e.g. 1000"
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </>
        )}

        {nodeData.type === 'Action' && (
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Description</label>
            <textarea
              value={String(config.description ?? '')}
              onChange={(e) => setConfigField('description', e.target.value)}
              rows={3}
              placeholder="Describe what this step does..."
              className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </div>
        )}

        {nodeData.type === 'DataTransform' && (
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Mappings (JSON)</label>
            <textarea
              value={JSON.stringify(config.mappings ?? {}, null, 2)}
              onChange={(e) => {
                try { setConfigField('mappings', JSON.parse(e.target.value)) } catch { /* invalid JSON */ }
              }}
              rows={5}
              placeholder='{ "targetField": "sourceField" }'
              className="w-full text-xs font-mono border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </div>
        )}
      </div>

      <div className="p-4 border-t border-gray-200">
        <button
          onClick={handleSave}
          className="w-full py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
        >
          Apply
        </button>
      </div>
    </div>
  )
}
