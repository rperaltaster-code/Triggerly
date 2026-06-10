import { useEffect, useState } from 'react'
import { X, Tag } from 'lucide-react'
import type { Node } from '@xyflow/react'
import type { StepNodeData } from './StepNode'
import type { FormField } from '../../types'

interface StepConfigPanelProps {
  node: Node | null
  onClose: () => void
  onUpdate: (nodeId: string, data: Partial<StepNodeData>) => void
  formFields?: FormField[]
}

const TOKEN_MIME = 'application/x-field-token'

function resolvePreview(value: string, fields: FormField[]): string {
  return value.replace(/\{\{input\.([^}]+)\}\}/g, (_, fieldId) => {
    const f = fields.find((ff) => ff.id === fieldId)
    return f ? `[${f.label}]` : '[unknown]'
  })
}

export function StepConfigPanel({ node, onClose, onUpdate, formFields = [] }: StepConfigPanelProps) {
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
    onUpdate(node.id, { name, config, approverEmail: approverEmail || undefined })
  }

  const setConfigField = (key: string, value: unknown) =>
    setConfig((prev) => ({ ...prev, [key]: value }))

  const onChipDragStart = (e: React.DragEvent, fieldId: string) => {
    e.dataTransfer.setData(TOKEN_MIME, `{{input.${fieldId}}}`)
    e.dataTransfer.effectAllowed = 'copy'
  }

  const makeDropHandlers = (key: string, currentVal: string) => ({
    onDragOver: (e: React.DragEvent) => {
      if (e.dataTransfer.types.includes(TOKEN_MIME)) e.preventDefault()
    },
    onDrop: (e: React.DragEvent) => {
      const token = e.dataTransfer.getData(TOKEN_MIME)
      if (!token) return
      e.preventDefault()
      const el = e.currentTarget as HTMLInputElement | HTMLTextAreaElement
      const start = el.selectionStart ?? currentVal.length
      const end = el.selectionEnd ?? currentVal.length
      setConfigField(key, currentVal.slice(0, start) + token + currentVal.slice(end))
    },
  })

  const hasTokens = (val: unknown) =>
    typeof val === 'string' && val.includes('{{input.')

  const tokenPreview = (val: unknown) => {
    if (!hasTokens(val) || !formFields.length) return null
    return (
      <p className="text-xs text-blue-500 mt-0.5 font-mono">
        {resolvePreview(val as string, formFields)}
      </p>
    )
  }

  const strVal = (key: string, fallback = '') => String(config[key] ?? fallback)

  return (
    <div className="w-72 bg-white border-l border-gray-200 flex flex-col h-full shadow-lg">
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
        <p className="text-sm font-semibold text-gray-800">Configure Step</p>
        <button onClick={onClose} className="p-1 text-gray-400 hover:text-gray-600 rounded">
          <X size={16} />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* Form field tokens */}
        {formFields.length > 0 && (
          <div className="rounded-lg border border-blue-100 bg-blue-50 p-3">
            <div className="flex items-center gap-1.5 mb-2">
              <Tag size={12} className="text-blue-500" />
              <span className="text-xs font-semibold text-blue-600">Form Fields</span>
              <span className="text-xs text-blue-400">— drag into fields below</span>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {formFields.map((f) => (
                <span
                  key={f.id}
                  draggable
                  onDragStart={(e) => onChipDragStart(e, f.id)}
                  className="inline-flex items-center px-2 py-0.5 bg-white border border-blue-200 text-blue-700 text-xs rounded-full cursor-grab active:cursor-grabbing select-none hover:bg-blue-100 transition-colors"
                  title={`{{input.${f.id}}}`}
                >
                  {f.label}
                </span>
              ))}
            </div>
          </div>
        )}

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

        {nodeData.type === 'Approval' && (
          <>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Approver Email</label>
              <input
                type="email"
                value={approverEmail}
                onChange={(e) => setApproverEmail(e.target.value)}
                placeholder="approver@example.com"
                {...makeDropHandlers('__approverEmail', approverEmail)}
                onDrop={(e) => {
                  const token = e.dataTransfer.getData(TOKEN_MIME)
                  if (!token) return
                  e.preventDefault()
                  const el = e.currentTarget
                  const start = el.selectionStart ?? approverEmail.length
                  const end = el.selectionEnd ?? approverEmail.length
                  setApproverEmail(approverEmail.slice(0, start) + token + approverEmail.slice(end))
                }}
                onDragOver={(e) => { if (e.dataTransfer.types.includes(TOKEN_MIME)) e.preventDefault() }}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {hasTokens(approverEmail) && (
                <p className="text-xs text-blue-500 mt-0.5 font-mono">{resolvePreview(approverEmail, formFields)}</p>
              )}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">SLA (hours)</label>
              <input
                type="number"
                value={strVal('slaHours', '72')}
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
                value={strVal('channel', 'email')}
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
                value={strVal('recipient')}
                onChange={(e) => setConfigField('recipient', e.target.value)}
                placeholder="user@example.com"
                {...makeDropHandlers('recipient', strVal('recipient'))}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {tokenPreview(config.recipient)}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Message</label>
              <textarea
                value={strVal('message')}
                onChange={(e) => setConfigField('message', e.target.value)}
                rows={3}
                placeholder="Notification message..."
                {...makeDropHandlers('message', strVal('message'))}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              />
              {tokenPreview(config.message)}
            </div>
          </>
        )}

        {nodeData.type === 'Delay' && (
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Delay (seconds)</label>
            <input
              type="number"
              value={strVal('delaySeconds', '60')}
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
                value={strVal('url')}
                onChange={(e) => setConfigField('url', e.target.value)}
                placeholder="https://example.com/webhook"
                {...makeDropHandlers('url', strVal('url'))}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {tokenPreview(config.url)}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Method</label>
              <select
                value={strVal('method', 'POST')}
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
                value={strVal('field')}
                onChange={(e) => setConfigField('field', e.target.value)}
                placeholder="e.g. amount"
                {...makeDropHandlers('field', strVal('field'))}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {tokenPreview(config.field)}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Operator</label>
              <select
                value={strVal('operator', 'equals')}
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
                value={strVal('value')}
                onChange={(e) => setConfigField('value', e.target.value)}
                placeholder="e.g. 1000"
                {...makeDropHandlers('value', strVal('value'))}
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {tokenPreview(config.value)}
            </div>
          </>
        )}

        {nodeData.type === 'Action' && (
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Description</label>
            <textarea
              value={strVal('description')}
              onChange={(e) => setConfigField('description', e.target.value)}
              rows={3}
              placeholder="Describe what this step does..."
              {...makeDropHandlers('description', strVal('description'))}
              className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
            {tokenPreview(config.description)}
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
