import { useState } from 'react'
import { Shield, Search } from 'lucide-react'
import { formatDistanceToNow, format } from 'date-fns'
import { useAuditLogs } from '../hooks/useAuditLogs'
import type { AuditLog } from '../api/auditLogs'

const ENTITY_TYPES = ['Workflow', 'Execution', 'AutomationRule']

const ACTION_COLORS: Record<string, string> = {
  WorkflowCreated: 'bg-blue-100 text-blue-700',
  WorkflowActivated: 'bg-green-100 text-green-700',
  WorkflowDeactivated: 'bg-yellow-100 text-yellow-700',
  WorkflowDeleted: 'bg-red-100 text-red-700',
  ExecutionTriggered: 'bg-purple-100 text-purple-700',
  ExecutionApproved: 'bg-green-100 text-green-700',
  ExecutionRejected: 'bg-red-100 text-red-700',
  ExecutionCancelled: 'bg-gray-100 text-gray-700',
  AutomationRuleCreated: 'bg-blue-100 text-blue-700',
  AutomationRuleEnabled: 'bg-green-100 text-green-700',
  AutomationRuleDisabled: 'bg-yellow-100 text-yellow-700',
  AutomationRuleDeleted: 'bg-red-100 text-red-700',
}

function actionLabel(action: string) {
  return action.replace(/([A-Z])/g, ' $1').trim()
}

function ActionBadge({ action }: { action: string }) {
  const cls = ACTION_COLORS[action] ?? 'bg-gray-100 text-gray-600'
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${cls}`}>
      {actionLabel(action)}
    </span>
  )
}

function EntityTypeBadge({ type }: { type: string }) {
  const colors: Record<string, string> = {
    Workflow: 'bg-indigo-50 text-indigo-600',
    Execution: 'bg-purple-50 text-purple-600',
    AutomationRule: 'bg-teal-50 text-teal-600',
  }
  return (
    <span className={`inline-flex px-2 py-0.5 rounded text-xs font-medium ${colors[type] ?? 'bg-gray-50 text-gray-600'}`}>
      {type}
    </span>
  )
}

export function AuditLogPage() {
  const [search, setSearch] = useState('')
  const [entityType, setEntityType] = useState<string>('')
  const [page, setPage] = useState(1)

  const { data, isLoading } = useAuditLogs({
    page,
    pageSize: 50,
    entityType: entityType || undefined,
    search: search || undefined,
  })

  const handleSearch = (value: string) => {
    setSearch(value)
    setPage(1)
  }

  const handleEntityType = (value: string) => {
    setEntityType(value)
    setPage(1)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Audit Log</h1>
          <p className="text-gray-500 mt-1">Track all changes and actions across your workspace</p>
        </div>
        <div className="flex items-center gap-1.5 px-3 py-1.5 bg-green-50 border border-green-200 rounded-lg">
          <Shield size={14} className="text-green-600" />
          <span className="text-xs font-medium text-green-700">Tamper-proof log</span>
        </div>
      </div>

      <div className="flex gap-3">
        <div className="relative flex-1">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            placeholder="Search by action, entity name, or user…"
            value={search}
            onChange={(e) => handleSearch(e.target.value)}
            className="w-full pl-9 pr-4 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <select
          value={entityType}
          onChange={(e) => handleEntityType(e.target.value)}
          className="border border-gray-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
        >
          <option value="">All types</option>
          {ENTITY_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
        </select>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex justify-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Entity</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">User</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">When</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {data?.items.map((log: AuditLog) => (
                <tr key={log.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-3.5">
                    <ActionBadge action={log.action} />
                  </td>
                  <td className="px-6 py-3.5">
                    <div className="flex items-center gap-2">
                      <EntityTypeBadge type={log.entityType} />
                      <span className="text-gray-800 font-medium truncate max-w-[160px]" title={log.entityName}>
                        {log.entityName}
                      </span>
                    </div>
                  </td>
                  <td className="px-6 py-3.5">
                    <div className="flex items-center gap-2">
                      <div className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 text-xs font-semibold flex-shrink-0">
                        {log.userName.charAt(0).toUpperCase()}
                      </div>
                      <span className="text-gray-700">{log.userName}</span>
                    </div>
                  </td>
                  <td className="px-6 py-3.5 text-gray-500 whitespace-nowrap">
                    <span title={format(new Date(log.timestamp), 'MMM d, yyyy HH:mm:ss')}>
                      {formatDistanceToNow(new Date(log.timestamp), { addSuffix: true })}
                    </span>
                  </td>
                  <td className="px-6 py-3.5 text-gray-400 text-xs max-w-[200px] truncate" title={log.details ?? ''}>
                    {log.details ?? '—'}
                  </td>
                </tr>
              ))}
              {data?.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-gray-400">
                    No audit log entries yet. Actions you take will appear here.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}

        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between px-6 py-3 border-t border-gray-100 bg-gray-50">
            <span className="text-sm text-gray-500">
              {data.totalCount} entries
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={!data.hasPreviousPage}
                className="px-3 py-1.5 text-sm border border-gray-200 rounded-lg disabled:opacity-40 hover:bg-white transition-colors"
              >
                Previous
              </button>
              <span className="px-3 py-1.5 text-sm text-gray-600">
                {page} / {data.totalPages}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={!data.hasNextPage}
                className="px-3 py-1.5 text-sm border border-gray-200 rounded-lg disabled:opacity-40 hover:bg-white transition-colors"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
