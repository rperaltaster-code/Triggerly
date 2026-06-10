import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { X, Play, Loader2 } from 'lucide-react'
import { useTriggerWorkflow } from '../../hooks/useWorkflows'
import type { FormField, WorkflowSummary } from '../../types'

interface Props {
  workflow: WorkflowSummary
  formSchema: FormField[]
  onClose: () => void
}

export function TriggerFormModal({ workflow, formSchema, onClose }: Props) {
  const navigate = useNavigate()
  const trigger = useTriggerWorkflow()
  const [values, setValues] = useState<Record<string, unknown>>({})
  const [errors, setErrors] = useState<Record<string, string>>({})

  const setValue = (id: string, value: unknown) => {
    setValues((prev) => ({ ...prev, [id]: value }))
    setErrors((prev) => { const next = { ...prev }; delete next[id]; return next })
  }

  const validate = () => {
    const errs: Record<string, string> = {}
    for (const field of formSchema) {
      if (field.required) {
        const v = values[field.id]
        if (v === undefined || v === null || v === '' || (field.type === 'Checkbox' && v === false)) {
          errs[field.id] = 'This field is required'
        }
      }
    }
    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validate()) return
    const execution = await trigger.mutateAsync({ id: workflow.id, inputData: values })
    onClose()
    navigate(`/executions/${execution.id}`)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <div>
            <h2 className="font-semibold text-gray-900">Trigger: {workflow.name}</h2>
            <p className="text-xs text-gray-400 mt-0.5">Fill in the form to start this workflow</p>
          </div>
          <button onClick={onClose} className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg">
            <X size={18} />
          </button>
        </div>

        {/* Form fields */}
        <form onSubmit={handleSubmit} className="flex flex-col flex-1 overflow-hidden">
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
            {formSchema.map((field) => (
              <div key={field.id}>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  {field.label}
                  {field.required && <span className="ml-1 text-red-500">*</span>}
                </label>

                {field.type === 'Text' && (
                  <input
                    type="text"
                    value={(values[field.id] as string) ?? ''}
                    onChange={(e) => setValue(field.id, e.target.value)}
                    placeholder={field.placeholder}
                    className={`w-full text-sm border rounded-lg px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-blue-500 ${errors[field.id] ? 'border-red-400' : 'border-gray-200'}`}
                  />
                )}

                {field.type === 'Number' && (
                  <input
                    type="number"
                    value={(values[field.id] as string) ?? ''}
                    onChange={(e) => setValue(field.id, e.target.value === '' ? '' : Number(e.target.value))}
                    placeholder={field.placeholder}
                    className={`w-full text-sm border rounded-lg px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-blue-500 ${errors[field.id] ? 'border-red-400' : 'border-gray-200'}`}
                  />
                )}

                {field.type === 'Date' && (
                  <input
                    type="date"
                    value={(values[field.id] as string) ?? ''}
                    onChange={(e) => setValue(field.id, e.target.value)}
                    className={`w-full text-sm border rounded-lg px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-blue-500 ${errors[field.id] ? 'border-red-400' : 'border-gray-200'}`}
                  />
                )}

                {field.type === 'Dropdown' && (
                  <select
                    value={(values[field.id] as string) ?? ''}
                    onChange={(e) => setValue(field.id, e.target.value)}
                    className={`w-full text-sm border rounded-lg px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-blue-500 ${errors[field.id] ? 'border-red-400' : 'border-gray-200'}`}
                  >
                    <option value="">Select an option…</option>
                    {(field.options ?? []).map((opt) => (
                      <option key={opt} value={opt}>{opt}</option>
                    ))}
                  </select>
                )}

                {field.type === 'Checkbox' && (
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={(values[field.id] as boolean) ?? false}
                      onChange={(e) => setValue(field.id, e.target.checked)}
                      className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700">{field.placeholder || field.label}</span>
                  </label>
                )}

                {errors[field.id] && (
                  <p className="mt-1 text-xs text-red-500">{errors[field.id]}</p>
                )}
              </div>
            ))}
          </div>

          {/* Footer */}
          <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-100">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={trigger.isPending}
              className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white text-sm rounded-lg hover:bg-green-700 disabled:opacity-50"
            >
              {trigger.isPending ? (
                <><Loader2 size={14} className="animate-spin" /> Starting…</>
              ) : (
                <><Play size={14} /> Start Workflow</>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
