import { useState } from 'react'
import { X, XCircle, Loader2 } from 'lucide-react'

interface Props {
  executionId: string
  workflowName: string
  onConfirm: (id: string, reason: string) => void | Promise<void>
  onClose: () => void
  isPending?: boolean
}

export function RejectModal({ executionId, workflowName, onConfirm, onClose, isPending }: Props) {
  const [reason, setReason] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!reason.trim()) return
    onConfirm(executionId, reason.trim())
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <div className="flex items-center gap-2">
            <XCircle size={18} className="text-red-500" />
            <h2 className="text-base font-semibold text-gray-900">Reject execution</h2>
          </div>
          <button onClick={onClose} className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg">
            <X size={16} />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-5 space-y-4">
          <p className="text-sm text-gray-600">
            You are rejecting <span className="font-medium text-gray-900">{workflowName}</span>.
            This cannot be undone.
          </p>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              Reason <span className="text-red-500">*</span>
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              required
              autoFocus
              rows={3}
              placeholder="Explain why this execution is being rejected…"
              className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-transparent resize-none"
            />
          </div>

          <div className="flex items-center gap-3 pt-1">
            <button
              type="submit"
              disabled={isPending || !reason.trim()}
              className="flex items-center gap-2 px-5 py-2.5 bg-red-600 text-white text-sm font-medium rounded-lg hover:bg-red-700 disabled:opacity-50 transition-colors"
            >
              {isPending && <Loader2 size={14} className="animate-spin" />}
              {isPending ? 'Rejecting…' : 'Reject'}
            </button>
            <button type="button" onClick={onClose} className="px-4 py-2.5 text-sm text-gray-600 hover:text-gray-900">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
