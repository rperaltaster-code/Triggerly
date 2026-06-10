import { useState } from 'react'
import { Plus, Trash2, GripVertical, ChevronDown, ChevronUp } from 'lucide-react'
import type { FormField, FormFieldType } from '../../types'

const FIELD_TYPES: { value: FormFieldType; label: string }[] = [
  { value: 'Text', label: 'Text' },
  { value: 'Number', label: 'Number' },
  { value: 'Date', label: 'Date' },
  { value: 'Dropdown', label: 'Dropdown' },
  { value: 'Checkbox', label: 'Checkbox' },
]

function newField(): FormField {
  return {
    id: crypto.randomUUID(),
    label: '',
    type: 'Text',
    required: false,
    placeholder: '',
    options: [],
  }
}

interface Props {
  fields: FormField[]
  onChange: (fields: FormField[]) => void
}

export function FormBuilder({ fields, onChange }: Props) {
  const [expanded, setExpanded] = useState<string | null>(null)

  const add = () => {
    const f = newField()
    onChange([...fields, f])
    setExpanded(f.id)
  }

  const remove = (id: string) => {
    onChange(fields.filter((f) => f.id !== id))
    if (expanded === id) setExpanded(null)
  }

  const update = (id: string, patch: Partial<FormField>) => {
    onChange(fields.map((f) => (f.id === id ? { ...f, ...patch } : f)))
  }

  const move = (index: number, dir: -1 | 1) => {
    const next = [...fields]
    const swap = index + dir
    if (swap < 0 || swap >= next.length) return
    ;[next[index], next[swap]] = [next[swap], next[index]]
    onChange(next)
  }

  return (
    <div className="space-y-3">
      {fields.length === 0 && (
        <div className="text-center py-10 border-2 border-dashed border-gray-200 rounded-xl text-gray-400 text-sm">
          No fields yet — click "Add Field" to start building your form
        </div>
      )}

      {fields.map((field, idx) => (
        <div key={field.id} className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
          {/* Header row */}
          <div className="flex items-center gap-2 px-4 py-3">
            <div className="flex flex-col gap-0.5">
              <button onClick={() => move(idx, -1)} disabled={idx === 0} className="text-gray-300 hover:text-gray-500 disabled:opacity-30">
                <ChevronUp size={13} />
              </button>
              <button onClick={() => move(idx, 1)} disabled={idx === fields.length - 1} className="text-gray-300 hover:text-gray-500 disabled:opacity-30">
                <ChevronDown size={13} />
              </button>
            </div>
            <GripVertical size={14} className="text-gray-300 flex-shrink-0" />
            <span className="flex-1 text-sm font-medium text-gray-800 truncate">
              {field.label || <span className="text-gray-400 italic">Untitled field</span>}
            </span>
            <span className="text-xs text-gray-400 bg-gray-100 px-2 py-0.5 rounded-full">{field.type}</span>
            {field.required && (
              <span className="text-xs text-red-500 font-medium">Required</span>
            )}
            <button
              onClick={() => setExpanded(expanded === field.id ? null : field.id)}
              className="p-1 text-gray-400 hover:text-gray-600 rounded"
            >
              <ChevronDown size={15} className={`transition-transform ${expanded === field.id ? 'rotate-180' : ''}`} />
            </button>
            <button onClick={() => remove(field.id)} className="p-1 text-gray-300 hover:text-red-500 rounded">
              <Trash2 size={14} />
            </button>
          </div>

          {/* Expanded config */}
          {expanded === field.id && (
            <div className="px-4 pb-4 pt-1 border-t border-gray-100 space-y-3 bg-gray-50">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Label *</label>
                  <input
                    type="text"
                    value={field.label}
                    onChange={(e) => update(field.id, { label: e.target.value })}
                    placeholder="e.g. Customer Name"
                    className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Type</label>
                  <select
                    value={field.type}
                    onChange={(e) => update(field.id, { type: e.target.value as FormFieldType, options: [] })}
                    className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    {FIELD_TYPES.map((t) => (
                      <option key={t.value} value={t.value}>{t.label}</option>
                    ))}
                  </select>
                </div>
              </div>

              {field.type !== 'Checkbox' && field.type !== 'Dropdown' && (
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Placeholder</label>
                  <input
                    type="text"
                    value={field.placeholder ?? ''}
                    onChange={(e) => update(field.id, { placeholder: e.target.value })}
                    className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              )}

              {field.type === 'Dropdown' && (
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    Options <span className="text-gray-400 font-normal">(one per line)</span>
                  </label>
                  <textarea
                    rows={4}
                    value={(field.options ?? []).join('\n')}
                    onChange={(e) =>
                      update(field.id, {
                        options: e.target.value.split('\n').map((o) => o.trim()).filter(Boolean),
                      })
                    }
                    placeholder="Option A&#10;Option B&#10;Option C"
                    className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none font-mono"
                  />
                </div>
              )}

              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={field.required}
                  onChange={(e) => update(field.id, { required: e.target.checked })}
                  className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700">Required</span>
              </label>
            </div>
          )}
        </div>
      ))}

      <button
        onClick={add}
        className="flex items-center gap-2 px-4 py-2.5 border-2 border-dashed border-gray-300 text-gray-500 rounded-xl hover:border-blue-400 hover:text-blue-600 transition-colors text-sm w-full justify-center"
      >
        <Plus size={15} /> Add Field
      </button>
    </div>
  )
}
