import { useState } from 'react'
import { Link } from 'react-router-dom'
import { CheckCircle, XCircle, X, RefreshCcw } from 'lucide-react'
import { useExecutions, useApproveExecution, useRejectExecution, useCancelExecution } from '../hooks/useExecutions'
import { Badge } from '../components/ui/Badge'
import { formatDistanceToNow } from 'date-fns'
import type { ExecutionStatus } from '../types'

export function Executions() {
  const [statusFilter, setStatusFilter] = useState<ExecutionStatus | undefined>()
  const { data, isLoading, refetch } = useExecutions({ status: statusFilter })
  const approve = useApproveExecution()
  const reject = useRejectExecution()
  const cancel = useCancelExecution()

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Executions</h1>
          <p className="text-gray-500 mt-1">Monitor running and completed workflow executions</p>
        </div>
        <button onClick={() => refetch()} className="flex items-center gap-2 px-3 py-2 border border-gray-200 rounded-lg text-sm text-gray-600 hover:bg-gray-50">
          <RefreshCcw size={14} /> Refresh
        </button>
      </div>

      <div className="flex gap-2 flex-wrap">
        {([undefined, 'Running', 'WaitingApproval', 'Completed', 'Failed'] as (ExecutionStatus | undefined)[]).map((s) => (
          <button
            key={s ?? 'all'}
            onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${
              statusFilter === s
                ? 'bg-blue-600 text-white border-blue-600'
                : 'bg-white text-gray-600 border-gray-200 hover:border-blue-300'
            }`}
          >
            {s ?? 'All'}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="flex justify-center py-12"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" /></div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Workflow</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Step</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Started</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {data?.items.map((ex) => (
                <tr key={ex.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4">
                    <Link to={`/executions/${ex.id}`} className="font-medium text-gray-900 hover:text-blue-600">
                      {ex.workflowName}
                    </Link>
                    <p className="text-gray-400 text-xs mt-0.5 font-mono">{ex.id.slice(0, 8)}…</p>
                  </td>
                  <td className="px-6 py-4"><Badge status={ex.status} /></td>
                  <td className="px-6 py-4 text-gray-500">{ex.currentStepName ?? '—'}</td>
                  <td className="px-6 py-4 text-gray-500">
                    {formatDistanceToNow(new Date(ex.startedAt), { addSuffix: true })}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2 justify-end">
                      {ex.status === 'WaitingApproval' && (
                        <>
                          <button
                            onClick={() => approve.mutate(ex.id)}
                            className="p-1.5 text-green-600 hover:bg-green-50 rounded-lg"
                            title="Approve"
                          >
                            <CheckCircle size={15} />
                          </button>
                          <button
                            onClick={() => {
                              const reason = prompt('Reason for rejection:')
                              if (reason) reject.mutate({ id: ex.id, reason })
                            }}
                            className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg"
                            title="Reject"
                          >
                            <XCircle size={15} />
                          </button>
                        </>
                      )}
                      {['Running', 'WaitingApproval'].includes(ex.status) && (
                        <button
                          onClick={() => { if (confirm('Cancel this execution?')) cancel.mutate(ex.id) }}
                          className="p-1.5 text-gray-400 hover:bg-gray-100 rounded-lg"
                          title="Cancel"
                        >
                          <X size={15} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {data?.items.length === 0 && (
                <tr><td colSpan={5} className="px-6 py-12 text-center text-gray-400">No executions found.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
