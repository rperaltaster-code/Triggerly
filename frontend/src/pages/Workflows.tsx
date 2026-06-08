import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Plus, Play, Power, Trash2, Search, PenSquare } from 'lucide-react'
import { useWorkflows, useActivateWorkflow, useDeleteWorkflow, useTriggerWorkflow } from '../hooks/useWorkflows'
import { Badge } from '../components/ui/Badge'
import { formatDistanceToNow } from 'date-fns'

export function Workflows() {
  const [search, setSearch] = useState('')
  const navigate = useNavigate()
  const { data, isLoading } = useWorkflows({ search: search || undefined })
  const activate = useActivateWorkflow()
  const deleteWf = useDeleteWorkflow()
  const trigger = useTriggerWorkflow()

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Workflows</h1>
          <p className="text-gray-500 mt-1">Define and manage your automation workflows</p>
        </div>
        <Link
          to="/workflows/new"
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium"
        >
          <Plus size={16} /> New Workflow
        </Link>
      </div>

      <div className="relative">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input
          type="text"
          placeholder="Search workflows..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full pl-10 pr-4 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {isLoading ? (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Steps</th>
                <th className="text-left px-6 py-3 text-xs font-medium text-gray-500 uppercase tracking-wider">Updated</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {data?.items.map((wf) => (
                <tr key={wf.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4">
                    <Link to={`/workflows/${wf.id}`} className="font-medium text-gray-900 hover:text-blue-600">
                      {wf.name}
                    </Link>
                    <p className="text-gray-400 text-xs mt-0.5">v{wf.version}</p>
                  </td>
                  <td className="px-6 py-4"><Badge status={wf.status} /></td>
                  <td className="px-6 py-4 text-gray-600">{wf.stepCount}</td>
                  <td className="px-6 py-4 text-gray-500">
                    {formatDistanceToNow(new Date(wf.updatedAt), { addSuffix: true })}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2 justify-end">
                      <button
                        onClick={() => navigate(`/workflows/${wf.id}/builder`)}
                        className="p-1.5 text-indigo-600 hover:bg-indigo-50 rounded-lg"
                        title="Open Builder"
                      >
                        <PenSquare size={15} />
                      </button>
                      {wf.status === 'Active' && (
                        <button
                          onClick={() => trigger.mutate({ id: wf.id })}
                          className="p-1.5 text-green-600 hover:bg-green-50 rounded-lg"
                          title="Trigger"
                        >
                          <Play size={15} />
                        </button>
                      )}
                      {wf.status === 'Draft' && (
                        <button
                          onClick={() => activate.mutate(wf.id)}
                          className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg"
                          title="Activate"
                        >
                          <Power size={15} />
                        </button>
                      )}
                      <button
                        onClick={() => { if (confirm('Delete this workflow?')) deleteWf.mutate(wf.id) }}
                        className="p-1.5 text-red-400 hover:bg-red-50 rounded-lg"
                        title="Delete"
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {data?.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-gray-400">
                    No workflows found. Create your first one!
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
