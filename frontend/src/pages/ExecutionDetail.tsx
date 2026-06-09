import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, CheckCircle, XCircle, Clock, AlertTriangle, MessageSquare, Send } from 'lucide-react'
import { useExecution, useApproveExecution, useRejectExecution, useAddComment } from '../hooks/useExecutions'
import { RejectModal } from '../components/executions/RejectModal'
import { Badge } from '../components/ui/Badge'
import { format, formatDistanceToNow } from 'date-fns'
import type { ExecutionStatus } from '../types'

const stepStatusIcon: Record<ExecutionStatus, React.ReactNode> = {
  Completed: <CheckCircle size={16} className="text-green-500" />,
  Failed: <AlertTriangle size={16} className="text-red-500" />,
  Running: <div className="w-4 h-4 rounded-full border-2 border-blue-500 border-t-transparent animate-spin" />,
  WaitingApproval: <Clock size={16} className="text-orange-500" />,
  Pending: <div className="w-4 h-4 rounded-full border-2 border-gray-300" />,
  Approved: <CheckCircle size={16} className="text-teal-500" />,
  Rejected: <XCircle size={16} className="text-red-500" />,
  Cancelled: <XCircle size={16} className="text-gray-400" />,
  TimedOut: <Clock size={16} className="text-purple-500" />,
}

export function ExecutionDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: execution, isLoading } = useExecution(id!)
  const approve = useApproveExecution()
  const reject = useRejectExecution()
  const addComment = useAddComment(id!)
  const [commentText, setCommentText] = useState('')
  const [showRejectModal, setShowRejectModal] = useState(false)

  if (isLoading) return <div className="flex justify-center py-12"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" /></div>
  if (!execution) return <div className="text-center py-12 text-gray-500">Execution not found</div>

  const handleRejectConfirm = async (execId: string, reason: string) => {
    await reject.mutateAsync({ id: execId, reason })
    setShowRejectModal(false)
  }

  const handleAddComment = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!commentText.trim()) return
    await addComment.mutateAsync(commentText.trim())
    setCommentText('')
  }

  return (
    <div className="space-y-6 max-w-4xl">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate(-1)} className="p-2 hover:bg-gray-100 rounded-lg">
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3 flex-wrap">
            <h1 className="text-2xl font-bold text-gray-900">{execution.workflowName}</h1>
            <Badge status={execution.status} />
            {execution.slaBreachedAt && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-red-100 text-red-700 text-xs font-medium rounded-full">
                <AlertTriangle size={11} />
                SLA Breached
              </span>
            )}
          </div>
          <p className="text-gray-400 text-sm font-mono mt-0.5">{execution.temporalWorkflowId}</p>
        </div>
        {execution.status === 'WaitingApproval' && (
          <div className="flex gap-2">
            <button
              onClick={() => approve.mutate(execution.id)}
              disabled={approve.isPending}
              className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm disabled:opacity-50"
            >
              <CheckCircle size={15} /> Approve
            </button>
            <button
              onClick={() => setShowRejectModal(true)}
              className="flex items-center gap-2 px-4 py-2 border border-red-300 text-red-600 rounded-lg hover:bg-red-50 text-sm"
            >
              <XCircle size={15} /> Reject
            </button>
          </div>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        {[
          { label: 'Started', value: format(new Date(execution.startedAt), 'MMM d, yyyy HH:mm:ss') },
          { label: 'Completed', value: execution.completedAt ? format(new Date(execution.completedAt), 'MMM d, yyyy HH:mm:ss') : '—' },
          { label: 'Triggered By', value: execution.triggeredBy ?? 'System' },
          { label: 'Current Step', value: execution.currentStepName ?? '—' },
        ].map(({ label, value }) => (
          <div key={label} className="bg-white border border-gray-200 rounded-lg p-4">
            <p className="text-xs text-gray-500">{label}</p>
            <p className="font-medium text-gray-900 mt-0.5">{value}</p>
          </div>
        ))}
      </div>

      {execution.slaBreachedAt && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex gap-3">
          <AlertTriangle size={18} className="text-red-500 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-medium text-red-700">SLA Breached</p>
            <p className="text-sm text-red-600 mt-0.5">
              Approval SLA was breached at {format(new Date(execution.slaBreachedAt), 'MMM d, yyyy HH:mm:ss')}.
              The approver has been notified by email.
            </p>
          </div>
        </div>
      )}

      {execution.errorMessage && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-sm font-medium text-red-700">Error</p>
          <p className="text-sm text-red-600 mt-1">{execution.errorMessage}</p>
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <h2 className="font-semibold text-gray-800 mb-4">Execution Steps</h2>
        {execution.steps.length === 0 ? (
          <p className="text-gray-400 text-sm">No step details available.</p>
        ) : (
          <div className="space-y-2">
            {execution.steps.map((step) => (
              <div key={step.id} className="flex items-center gap-4 p-3 bg-gray-50 rounded-lg">
                <div className="flex-shrink-0">{stepStatusIcon[step.status] ?? null}</div>
                <div className="flex-1">
                  <span className="font-medium text-sm text-gray-900">{step.stepName}</span>
                  {step.errorMessage && <p className="text-xs text-red-500 mt-0.5">{step.errorMessage}</p>}
                </div>
                <Badge status={step.status} />
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Comments */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <div className="flex items-center gap-2 mb-4">
          <MessageSquare size={18} className="text-gray-500" />
          <h2 className="font-semibold text-gray-800">
            Comments
            {execution.comments.length > 0 && (
              <span className="ml-2 text-xs font-normal text-gray-400">({execution.comments.length})</span>
            )}
          </h2>
        </div>

        {execution.comments.length === 0 ? (
          <p className="text-gray-400 text-sm mb-4">No comments yet.</p>
        ) : (
          <div className="space-y-4 mb-4">
            {execution.comments.map((comment) => (
              <div key={comment.id} className="flex gap-3">
                <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center flex-shrink-0 text-blue-700 text-xs font-semibold">
                  {comment.authorName.charAt(0).toUpperCase()}
                </div>
                <div className="flex-1">
                  <div className="flex items-baseline gap-2">
                    <span className="text-sm font-medium text-gray-900">{comment.authorName}</span>
                    <span className="text-xs text-gray-400">
                      {formatDistanceToNow(new Date(comment.createdAt), { addSuffix: true })}
                    </span>
                  </div>
                  <p className="text-sm text-gray-700 mt-0.5 whitespace-pre-wrap">{comment.content}</p>
                </div>
              </div>
            ))}
          </div>
        )}

        <form onSubmit={handleAddComment} className="flex gap-2 pt-4 border-t border-gray-100">
          <input
            type="text"
            value={commentText}
            onChange={(e) => setCommentText(e.target.value)}
            placeholder="Add a comment..."
            className="flex-1 text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            type="submit"
            disabled={!commentText.trim() || addComment.isPending}
            className="flex items-center gap-1.5 px-4 py-2 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <Send size={14} />
            {addComment.isPending ? 'Posting...' : 'Post'}
          </button>
        </form>
      </div>

      {Object.keys(execution.inputData).length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
          <h2 className="font-semibold text-gray-800 mb-3">Input Data</h2>
          <pre className="text-xs bg-gray-50 p-4 rounded-lg overflow-auto">
            {JSON.stringify(execution.inputData, null, 2)}
          </pre>
        </div>
      )}

      {showRejectModal && (
        <RejectModal
          executionId={execution.id}
          workflowName={execution.workflowName}
          onConfirm={handleRejectConfirm}
          onClose={() => setShowRejectModal(false)}
          isPending={reject.isPending}
        />
      )}
    </div>
  )
}
