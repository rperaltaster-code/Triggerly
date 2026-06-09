import { useState } from 'react'
import { Link } from 'react-router-dom'
import { CheckCircle, Clock, ChevronDown, ChevronRight } from 'lucide-react'
import { useExecutions, useApproveExecution, useRejectExecution } from '../hooks/useExecutions'
import { RejectModal } from '../components/executions/RejectModal'
import { formatDistanceToNow } from 'date-fns'
import type { WorkflowExecution } from '../types'

function ApprovalCard({
  execution,
  onApprove,
  onReject,
  isApprovePending,
}: {
  execution: WorkflowExecution
  onApprove: (id: string) => void
  onReject: (id: string) => void
  isApprovePending: boolean
}) {
  const [expanded, setExpanded] = useState(false)
  const hasInput = Object.keys(execution.inputData ?? {}).length > 0

  return (
    <div className="bg-white rounded-xl border border-orange-200 shadow-sm overflow-hidden">
      <div className="p-5">
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-start gap-3 min-w-0">
            <div className="p-2 bg-orange-50 rounded-lg shrink-0">
              <Clock size={18} className="text-orange-500" />
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <Link
                  to={`/executions/${execution.id}`}
                  className="font-semibold text-gray-900 hover:text-blue-600 truncate"
                >
                  {execution.workflowName}
                </Link>
                <span className="text-xs px-2 py-0.5 bg-orange-100 text-orange-700 rounded-full font-medium shrink-0">
                  Awaiting Approval
                </span>
              </div>
              <div className="flex items-center gap-4 mt-1.5 text-xs text-gray-400 flex-wrap">
                <span>
                  Triggered by <span className="font-medium text-gray-600">{execution.triggeredBy ?? 'System'}</span>
                </span>
                <span>
                  Waiting{' '}
                  <span className="font-medium text-gray-600">
                    {formatDistanceToNow(new Date(execution.startedAt), { addSuffix: false })}
                  </span>
                </span>
                {execution.currentStepName && (
                  <span>
                    Step: <span className="font-medium text-gray-600">{execution.currentStepName}</span>
                  </span>
                )}
              </div>

              {hasInput && (
                <button
                  onClick={() => setExpanded((v) => !v)}
                  className="flex items-center gap-1 mt-2 text-xs text-gray-400 hover:text-gray-700 transition-colors"
                >
                  {expanded ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
                  Input data
                </button>
              )}
              {expanded && hasInput && (
                <pre className="mt-2 text-xs bg-gray-50 border border-gray-200 rounded-lg p-3 overflow-auto max-h-40">
                  {JSON.stringify(execution.inputData, null, 2)}
                </pre>
              )}
            </div>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            <button
              onClick={() => onApprove(execution.id)}
              disabled={isApprovePending}
              className="flex items-center gap-1.5 px-4 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700 disabled:opacity-50 transition-colors"
            >
              <CheckCircle size={14} />
              Approve
            </button>
            <button
              onClick={() => onReject(execution.id)}
              className="flex items-center gap-1.5 px-4 py-2 border border-red-300 text-red-600 text-sm font-medium rounded-lg hover:bg-red-50 transition-colors"
            >
              Reject
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

export function Approvals() {
  const { data, isLoading } = useExecutions({ status: 'WaitingApproval', pageSize: 50 })
  const approve = useApproveExecution()
  const reject = useRejectExecution()
  const [rejectTarget, setRejectTarget] = useState<WorkflowExecution | null>(null)

  const handleRejectConfirm = async (_id: string, reason: string) => {
    await reject.mutateAsync({ id: rejectTarget!.id, reason })
    setRejectTarget(null)
  }

  return (
    <>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Pending Approvals</h1>
          <p className="text-gray-500 mt-1">Review and action workflow executions waiting for approval</p>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
          </div>
        ) : data?.items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 bg-white rounded-xl border border-gray-200 text-center">
            <CheckCircle size={40} className="text-green-400 mb-3" />
            <p className="text-gray-700 font-medium">All clear</p>
            <p className="text-gray-400 text-sm mt-1">No executions are waiting for approval.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {data?.items.map((ex) => (
              <ApprovalCard
                key={ex.id}
                execution={ex}
                onApprove={(id) => approve.mutate(id)}
                onReject={(_id) => setRejectTarget(ex)}
                isApprovePending={approve.isPending}
              />
            ))}
          </div>
        )}
      </div>

      {rejectTarget && (
        <RejectModal
          executionId={rejectTarget.id}
          workflowName={rejectTarget.workflowName}
          onConfirm={handleRejectConfirm}
          onClose={() => setRejectTarget(null)}
          isPending={reject.isPending}
        />
      )}
    </>
  )
}
