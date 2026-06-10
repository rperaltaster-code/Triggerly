import { useParams, useNavigate } from 'react-router-dom'
import { Play, Power, ArrowLeft, GitBranch, PenSquare } from 'lucide-react'
import { useWorkflow, useActivateWorkflow, useTriggerWorkflow } from '../hooks/useWorkflows'
import { Badge } from '../components/ui/Badge'
import { useRole } from '../hooks/useRole'
import { format } from 'date-fns'

const stepTypeColors: Record<string, string> = {
  Action: 'bg-blue-100 text-blue-700',
  Approval: 'bg-orange-100 text-orange-700',
  Condition: 'bg-purple-100 text-purple-700',
  Delay: 'bg-gray-100 text-gray-600',
  Notification: 'bg-teal-100 text-teal-700',
  DataTransform: 'bg-indigo-100 text-indigo-700',
  Webhook: 'bg-pink-100 text-pink-700',
}

export function WorkflowDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: workflow, isLoading } = useWorkflow(id!)
  const activate = useActivateWorkflow()
  const trigger = useTriggerWorkflow()
  const { canEdit } = useRole()

  if (isLoading) return <div className="flex justify-center py-12"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" /></div>
  if (!workflow) return <div className="text-center py-12 text-gray-500">Workflow not found</div>

  return (
    <div className="space-y-6 max-w-4xl">
      <div className="flex items-center gap-4">
        <button onClick={() => navigate(-1)} className="p-2 hover:bg-gray-100 rounded-lg">
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">{workflow.name}</h1>
            <Badge status={workflow.status} />
          </div>
          {workflow.description && <p className="text-gray-500 mt-0.5">{workflow.description}</p>}
        </div>
        <div className="flex gap-2">
          {canEdit && (
            <button
              onClick={() => navigate(`/workflows/${workflow.id}/builder`)}
              className="flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm"
            >
              <PenSquare size={15} /> Open Builder
            </button>
          )}
          {canEdit && workflow.status === 'Draft' && (
            <button
              onClick={() => activate.mutate(workflow.id)}
              className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm"
            >
              <Power size={15} /> Activate
            </button>
          )}
          {canEdit && workflow.status === 'Active' && (
            <button
              onClick={() => trigger.mutate({ id: workflow.id })}
              className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm"
            >
              <Play size={15} /> Trigger
            </button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Version', value: `v${workflow.version}` },
          { label: 'Created', value: format(new Date(workflow.createdAt), 'MMM d, yyyy') },
          { label: 'Updated', value: format(new Date(workflow.updatedAt), 'MMM d, yyyy HH:mm') },
        ].map(({ label, value }) => (
          <div key={label} className="bg-white border border-gray-200 rounded-lg p-4">
            <p className="text-xs text-gray-500">{label}</p>
            <p className="font-medium text-gray-900 mt-0.5">{value}</p>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <div className="flex items-center gap-2 mb-4">
          <GitBranch size={18} className="text-gray-500" />
          <h2 className="font-semibold text-gray-800">Steps ({workflow.steps.length})</h2>
        </div>

        {workflow.steps.length === 0 ? (
          <p className="text-gray-400 text-sm py-4 text-center">No steps defined yet.</p>
        ) : (
          <div className="space-y-3">
            {workflow.steps.map((step, idx) => (
              <div key={step.id} className="flex items-start gap-4 p-4 bg-gray-50 rounded-lg">
                <div className="flex-shrink-0 w-8 h-8 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center text-sm font-bold">
                  {idx + 1}
                </div>
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-gray-900">{step.name}</span>
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${stepTypeColors[step.type] ?? 'bg-gray-100 text-gray-600'}`}>
                      {step.type}
                    </span>
                  </div>
                  {Object.keys(step.config).length > 0 && (
                    <pre className="mt-2 text-xs text-gray-500 bg-white rounded p-2 border overflow-auto">
                      {JSON.stringify(step.config, null, 2)}
                    </pre>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
