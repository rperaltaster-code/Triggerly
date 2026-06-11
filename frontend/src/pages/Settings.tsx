import { useState } from 'react'
import { ChevronDown, ChevronUp, RotateCcw, Save, Plus, Pencil, Trash2 } from 'lucide-react'
import { useTeam, useUpdateRole } from '../hooks/useTeam'
import { useRole } from '../hooks/useRole'
import { useAuth } from '../contexts/AuthContext'
import { useEmailTemplates, useUpsertEmailTemplate, useResetEmailTemplate } from '../hooks/useEmailTemplates'
import { useServiceTypes, useCreateServiceType, useUpdateServiceType, useDeleteServiceType } from '../hooks/useClients'
import { useWorkflows } from '../hooks/useWorkflows'
import { formatDistanceToNow } from 'date-fns'
import type { UserRole, EmailTemplate, ServiceType, FilingPeriod } from '../types'
import type { SaveServiceTypePayload } from '../api/clients'

const ROLES: UserRole[] = ['Preparer', 'Reviewer', 'Manager']

const roleDescriptions: Record<UserRole, string> = {
  Preparer: 'Can view and complete assigned tasks only',
  Reviewer: 'Can approve / reject steps and view all team activity',
  Manager: 'Full access — workflows, clients, team, and settings',
}

const roleBadge: Record<UserRole, string> = {
  Preparer: 'bg-gray-100 text-gray-600',
  Reviewer: 'bg-teal-100 text-teal-700',
  Manager: 'bg-blue-100 text-blue-700',
}

const templateMeta: Record<string, { label: string; tokens: string[] }> = {
  approval_request: {
    label: 'Approval Request',
    tokens: ['{{workflowName}}', '{{stepName}}', '{{executionId}}', '{{approvalsUrl}}'],
  },
  approval_reminder: {
    label: 'Approval Reminder',
    tokens: ['{{workflowName}}', '{{stepName}}', '{{executionId}}', '{{approvalsUrl}}', '{{percentElapsed}}', '{{remainingHours}}', '{{slaHours}}'],
  },
  escalation: {
    label: 'Escalation',
    tokens: ['{{workflowName}}', '{{stepName}}', '{{executionId}}', '{{approvalsUrl}}', '{{primaryEmail}}', '{{slaHours}}'],
  },
  sla_breach: {
    label: 'SLA Breach',
    tokens: ['{{workflowName}}', '{{stepName}}', '{{executionId}}', '{{approvalsUrl}}', '{{slaHours}}'],
  },
  notification: {
    label: 'Workflow Notification',
    tokens: ['{{message}}'],
  },
}

function TemplateEditor({ template, onClose }: { template: EmailTemplate; onClose: () => void }) {
  const [subject, setSubject] = useState(template.subject)
  const [body, setBody] = useState(template.body)
  const upsert = useUpsertEmailTemplate()
  const reset = useResetEmailTemplate()
  const meta = templateMeta[template.key]

  const handleSave = async () => {
    await upsert.mutateAsync({ key: template.key, subject, body })
    onClose()
  }

  const handleReset = async () => {
    if (!confirm('Reset to default? Your custom template will be deleted.')) return
    await reset.mutateAsync(template.key)
    onClose()
  }

  return (
    <div className="border-t border-gray-100 bg-gray-50 p-5 space-y-4">
      <div className="grid grid-cols-3 gap-4">
        <div className="col-span-2 space-y-3">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Subject</label>
            <input
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              className="w-full text-sm border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Body (HTML)</label>
            <textarea
              value={body}
              onChange={(e) => setBody(e.target.value)}
              rows={10}
              className="w-full text-sm font-mono border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-y"
            />
          </div>
        </div>
        <div>
          <p className="text-xs font-medium text-gray-600 mb-2">Available tokens</p>
          <div className="space-y-1">
            {meta?.tokens.map((token) => (
              <button
                key={token}
                type="button"
                onClick={() => setBody((b) => b + token)}
                className="block w-full text-left text-xs font-mono px-2 py-1.5 bg-white border border-gray-200 rounded hover:bg-blue-50 hover:border-blue-300 text-gray-700 transition-colors"
                title="Click to append to body"
              >
                {token}
              </button>
            ))}
          </div>
          <p className="text-xs text-gray-400 mt-2">Click a token to append it to the body.</p>
        </div>
      </div>

      <div className="flex items-center gap-2 pt-1">
        <button
          onClick={handleSave}
          disabled={upsert.isPending}
          className="flex items-center gap-1.5 px-4 py-2 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 disabled:opacity-50"
        >
          <Save size={14} /> {upsert.isPending ? 'Saving…' : 'Save'}
        </button>
        {template.isCustom && (
          <button
            onClick={handleReset}
            disabled={reset.isPending}
            className="flex items-center gap-1.5 px-3 py-2 border border-gray-300 text-gray-600 text-sm rounded-lg hover:bg-gray-50 disabled:opacity-50"
          >
            <RotateCcw size={13} /> Reset to default
          </button>
        )}
        <button
          onClick={onClose}
          className="px-3 py-2 text-sm text-gray-500 hover:text-gray-700"
        >
          Cancel
        </button>
      </div>
    </div>
  )
}

function TemplateRow({ template }: { template: EmailTemplate }) {
  const [open, setOpen] = useState(false)
  const meta = templateMeta[template.key]

  return (
    <div className="border-b border-gray-100 last:border-0">
      <button
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center gap-4 px-6 py-4 hover:bg-gray-50 transition-colors text-left"
      >
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-gray-900">{meta?.label ?? template.key}</span>
            {template.isCustom ? (
              <span className="text-xs px-2 py-0.5 bg-blue-100 text-blue-700 rounded-full font-medium">Custom</span>
            ) : (
              <span className="text-xs px-2 py-0.5 bg-gray-100 text-gray-500 rounded-full">Default</span>
            )}
          </div>
          <p className="text-xs text-gray-500 truncate mt-0.5">{template.subject}</p>
          {template.isCustom && template.updatedAt && (
            <p className="text-xs text-gray-400 mt-0.5">
              Last edited {formatDistanceToNow(new Date(template.updatedAt), { addSuffix: true })}
            </p>
          )}
        </div>
        {open ? <ChevronUp size={16} className="text-gray-400 flex-shrink-0" /> : <ChevronDown size={16} className="text-gray-400 flex-shrink-0" />}
      </button>
      {open && <TemplateEditor template={template} onClose={() => setOpen(false)} />}
    </div>
  )
}

const FILING_PERIODS: FilingPeriod[] = ['Monthly', 'TwoMonthly', 'SixMonthly', 'Annual', 'OneOff']
const FILING_LABELS: Record<FilingPeriod, string> = {
  Monthly: 'Monthly', TwoMonthly: 'Two-Monthly', SixMonthly: 'Six-Monthly', Annual: 'Annual', OneOff: 'One-Off',
}
const COLOR_PRESETS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16']

function ServiceTypeModal({
  initial,
  onSave,
  onClose,
  saving,
}: {
  initial?: ServiceType
  onSave: (d: SaveServiceTypePayload) => void
  onClose: () => void
  saving: boolean
}) {
  const { data: wfResult } = useWorkflows({ status: 'Active' })
  const workflows = wfResult?.items ?? []
  const [form, setForm] = useState<SaveServiceTypePayload>(
    initial
      ? { name: initial.name, description: initial.description, defaultWorkflowId: initial.defaultWorkflowId, defaultFilingPeriod: initial.defaultFilingPeriod, color: initial.color }
      : { name: '', description: null, defaultWorkflowId: null, defaultFilingPeriod: 'Annual', color: COLOR_PRESETS[0] },
  )

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-4">{initial ? 'Edit Service Type' : 'New Service Type'}</h2>
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
            <input className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <input className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.description ?? ''} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value || null }))} />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Default Workflow</label>
            <select className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.defaultWorkflowId ?? ''} onChange={(e) => setForm((f) => ({ ...f, defaultWorkflowId: e.target.value || null }))}>
              <option value="">— None —</option>
              {workflows.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Default Filing Period</label>
            <select className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.defaultFilingPeriod ?? ''} onChange={(e) => setForm((f) => ({ ...f, defaultFilingPeriod: (e.target.value || null) as FilingPeriod | null }))}>
              <option value="">— None —</option>
              {FILING_PERIODS.map((p) => <option key={p} value={p}>{FILING_LABELS[p]}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Colour</label>
            <div className="flex gap-2 flex-wrap">
              {COLOR_PRESETS.map((c) => (
                <button key={c} type="button"
                  onClick={() => setForm((f) => ({ ...f, color: c }))}
                  className={`w-7 h-7 rounded-full border-2 transition-transform ${form.color === c ? 'border-gray-900 scale-110' : 'border-transparent'}`}
                  style={{ backgroundColor: c }} />
              ))}
            </div>
          </div>
        </div>
        <div className="flex justify-end gap-2 mt-6">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-lg">Cancel</button>
          <button onClick={() => onSave(form)} disabled={saving || !form.name}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50">
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>
    </div>
  )
}

export function Settings() {
  const { user } = useAuth()
  const { isManager } = useRole()
  const { data: members, isLoading } = useTeam()
  const updateRole = useUpdateRole()
  const { data: emailTemplates, isLoading: templatesLoading } = useEmailTemplates()
  const { data: serviceTypes } = useServiceTypes()
  const createServiceType = useCreateServiceType()
  const deleteServiceType = useDeleteServiceType()
  const [editingServiceType, setEditingServiceType] = useState<ServiceType | null>(null)
  const [showNewServiceType, setShowNewServiceType] = useState(false)

  const handleCreateST = async (data: SaveServiceTypePayload) => {
    await createServiceType.mutateAsync(data)
    setShowNewServiceType(false)
  }

  const handleDeleteST = async (st: ServiceType) => {
    if (!confirm(`Delete service type "${st.name}"?`)) return
    await deleteServiceType.mutateAsync(st.id)
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
        <p className="text-gray-500 mt-1">Manage your team and organisation settings</p>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="font-semibold text-gray-800">Team Members</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            {isManager ? 'Assign roles to control what each member can do.' : 'Your team members and their roles.'}
          </p>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-10">
            <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-blue-600" />
          </div>
        ) : (
          <ul className="divide-y divide-gray-100">
            {members?.map((member) => (
              <li key={member.userId} className="flex items-center gap-4 px-6 py-4">
                <div className="w-9 h-9 rounded-full bg-blue-500 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
                  {member.name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {member.name}
                    {member.userId === user?.id && (
                      <span className="ml-2 text-xs text-gray-400">(you)</span>
                    )}
                  </p>
                  <p className="text-xs text-gray-500 truncate">{member.email}</p>
                </div>

                {isManager && member.userId !== user?.id ? (
                  <select
                    value={member.role}
                    onChange={(e) =>
                      updateRole.mutate({ userId: member.userId, role: e.target.value as UserRole })
                    }
                    className="text-sm border border-gray-200 rounded-lg px-2 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    {ROLES.map((r) => (
                      <option key={r} value={r}>{r}</option>
                    ))}
                  </select>
                ) : (
                  <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${roleBadge[member.role as UserRole] ?? roleBadge.Preparer}`}>
                    {member.role}
                  </span>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <h2 className="font-semibold text-gray-800 mb-4">Role Permissions</h2>
        <div className="space-y-3">
          {ROLES.map((r) => (
            <div key={r} className="flex items-start gap-3">
              <span className={`text-xs px-2.5 py-1 rounded-full font-medium w-24 text-center flex-shrink-0 ${roleBadge[r]}`}>
                {r}
              </span>
              <span className="text-sm text-gray-600 pt-0.5">{roleDescriptions[r]}</span>
            </div>
          ))}
        </div>
      </div>

      {isManager && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <div>
              <h2 className="font-semibold text-gray-800">Service Types</h2>
              <p className="text-sm text-gray-500 mt-0.5">Define the types of services you offer to clients.</p>
            </div>
            <button onClick={() => setShowNewServiceType(true)}
              className="flex items-center gap-1 text-sm bg-blue-600 text-white rounded-lg px-3 py-2 hover:bg-blue-700">
              <Plus size={14} /> New
            </button>
          </div>
          {!serviceTypes?.length ? (
            <div className="px-6 py-8 text-center text-sm text-gray-400">No service types yet.</div>
          ) : (
            <ul className="divide-y divide-gray-100">
              {serviceTypes.map((st) => (
                <li key={st.id} className="flex items-center gap-3 px-6 py-4">
                  <div className="w-3 h-3 rounded-full flex-shrink-0" style={{ backgroundColor: st.color ?? '#94a3b8' }} />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-900">{st.name}</p>
                    {st.description && <p className="text-xs text-gray-500 truncate">{st.description}</p>}
                  </div>
                  <div className="flex items-center gap-2">
                    <button onClick={() => setEditingServiceType(st)} className="text-gray-400 hover:text-gray-600"><Pencil size={14} /></button>
                    <button onClick={() => handleDeleteST(st)} className="text-red-400 hover:text-red-600"><Trash2 size={14} /></button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {isManager && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="font-semibold text-gray-800">Email Templates</h2>
            <p className="text-sm text-gray-500 mt-0.5">
              Customise the emails sent for workflow events. Tokens in <code className="text-xs bg-gray-100 px-1 rounded">{'{{brackets}}'}</code> are replaced at send time.
            </p>
          </div>
          {templatesLoading ? (
            <div className="flex justify-center py-8">
              <div className="animate-spin rounded-full h-7 w-7 border-b-2 border-blue-600" />
            </div>
          ) : (
            <div>
              {emailTemplates?.map((t) => (
                <TemplateRow key={t.key} template={t} />
              ))}
            </div>
          )}
        </div>
      )}

      {showNewServiceType && (
        <ServiceTypeModal
          onSave={handleCreateST}
          onClose={() => setShowNewServiceType(false)}
          saving={createServiceType.isPending}
        />
      )}
      {editingServiceType && (
        <EditServiceTypeWrapper
          st={editingServiceType}
          onClose={() => setEditingServiceType(null)}
        />
      )}
    </div>
  )
}

function EditServiceTypeWrapper({ st, onClose }: { st: ServiceType; onClose: () => void }) {
  const update = useUpdateServiceType(st.id)
  const handleSave = async (data: SaveServiceTypePayload) => {
    await update.mutateAsync(data)
    onClose()
  }
  return <ServiceTypeModal initial={st} onSave={handleSave} onClose={onClose} saving={update.isPending} />
}
