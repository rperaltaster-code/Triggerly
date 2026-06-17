import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { ArrowLeft, Loader2, GitBranch, Sparkles, ChevronDown, ChevronUp } from 'lucide-react'
import { useCreateWorkflow, useAiGenerateWorkflow } from '../hooks/useWorkflows'
import type { AiGeneratedStep, StepType } from '../types'
import { workflowsApi } from '../api/workflows'

const STEP_TYPE_COLORS: Record<StepType, string> = {
  Action: 'bg-gray-100 text-gray-700',
  Approval: 'bg-blue-100 text-blue-700',
  Condition: 'bg-yellow-100 text-yellow-700',
  Delay: 'bg-orange-100 text-orange-700',
  Notification: 'bg-purple-100 text-purple-700',
  DataTransform: 'bg-teal-100 text-teal-700',
  Webhook: 'bg-pink-100 text-pink-700',
}

export function WorkflowNew() {
  const navigate = useNavigate()
  const create = useCreateWorkflow()
  const aiGenerate = useAiGenerateWorkflow()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')

  const [showAiPanel, setShowAiPanel] = useState(false)
  const [aiPrompt, setAiPrompt] = useState('')
  const [aiSteps, setAiSteps] = useState<AiGeneratedStep[] | null>(null)

  const handleGenerate = async () => {
    if (!aiPrompt.trim()) return
    setError('')
    try {
      const result = await aiGenerate.mutateAsync({ prompt: aiPrompt })
      setAiSteps(result.steps)
      if (result.suggestedName && !name.trim()) {
        setName(result.suggestedName)
      }
    } catch {
      setError('AI generation failed. Please try again or build the workflow manually.')
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      const wf = await create.mutateAsync({ name, description, steps: [] })
      if (aiSteps && aiSteps.length > 0) {
        await workflowsApi.saveSteps(
          wf.id,
          aiSteps.map((s) => ({
            name: s.name,
            type: s.type,
            order: s.order,
            config: s.config as Record<string, unknown>,
            approverEmail: s.approverEmail ?? null,
          })),
        )
      }
      navigate(`/workflows/${wf.id}/builder`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create workflow')
    }
  }

  return (
    <div className="max-w-xl mx-auto space-y-6">
      <div className="flex items-center gap-3">
        <Link
          to="/workflows"
          className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
        >
          <ArrowLeft size={18} />
        </Link>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">New Workflow</h1>
          <p className="text-gray-500 text-sm mt-0.5">Give your workflow a name to get started</p>
        </div>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        {error && (
          <div className="mb-5 px-3 py-2.5 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              Workflow name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              autoFocus
              placeholder="e.g. Employee Onboarding, Invoice Approval"
              className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">
              Description
              <span className="text-gray-400 font-normal ml-1">(optional)</span>
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              placeholder="What does this workflow do?"
              className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
            />
          </div>

          {/* AI Panel */}
          <div className="border border-gray-200 rounded-lg overflow-hidden">
            <button
              type="button"
              onClick={() => setShowAiPanel(!showAiPanel)}
              className="w-full flex items-center justify-between px-4 py-3 bg-gradient-to-r from-violet-50 to-blue-50 hover:from-violet-100 hover:to-blue-100 transition-colors text-sm font-medium text-violet-700"
            >
              <span className="flex items-center gap-2">
                <Sparkles size={15} />
                Generate steps with AI
              </span>
              {showAiPanel ? <ChevronUp size={15} /> : <ChevronDown size={15} />}
            </button>

            {showAiPanel && (
              <div className="p-4 space-y-3 border-t border-gray-200">
                <p className="text-xs text-gray-500">
                  Describe the workflow in plain English and Claude will generate the steps for you.
                </p>
                <textarea
                  value={aiPrompt}
                  onChange={(e) => setAiPrompt(e.target.value)}
                  rows={3}
                  placeholder="e.g. Quarterly GST return review — gather data, send for partner approval, then notify the client when filed"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500 focus:border-transparent resize-none"
                />
                <button
                  type="button"
                  onClick={handleGenerate}
                  disabled={aiGenerate.isPending || !aiPrompt.trim()}
                  className="flex items-center gap-2 px-4 py-2 bg-violet-600 text-white text-sm font-medium rounded-lg hover:bg-violet-700 disabled:opacity-50 transition-colors"
                >
                  {aiGenerate.isPending ? (
                    <Loader2 size={14} className="animate-spin" />
                  ) : (
                    <Sparkles size={14} />
                  )}
                  {aiGenerate.isPending ? 'Generating…' : 'Generate'}
                </button>

                {aiSteps && aiSteps.length > 0 && (
                  <div className="mt-3 space-y-2">
                    <p className="text-xs font-medium text-gray-600">
                      {aiSteps.length} step{aiSteps.length !== 1 ? 's' : ''} generated — review and edit in the builder:
                    </p>
                    <ol className="space-y-1.5">
                      {aiSteps.map((step) => (
                        <li key={step.order} className="flex items-center gap-2.5 text-sm text-gray-700">
                          <span className="flex-none w-5 h-5 rounded-full bg-gray-200 text-gray-600 text-xs font-medium flex items-center justify-center">
                            {step.order}
                          </span>
                          <span className="flex-1 truncate">{step.name}</span>
                          <span
                            className={`flex-none px-2 py-0.5 rounded text-xs font-medium ${STEP_TYPE_COLORS[step.type] ?? 'bg-gray-100 text-gray-600'}`}
                          >
                            {step.type}
                          </span>
                        </li>
                      ))}
                    </ol>
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="flex items-center gap-3 pt-1">
            <button
              type="submit"
              disabled={create.isPending || !name.trim()}
              className="flex items-center gap-2 px-5 py-2.5 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {create.isPending ? (
                <Loader2 size={15} className="animate-spin" />
              ) : (
                <GitBranch size={15} />
              )}
              {create.isPending
                ? 'Creating…'
                : aiSteps && aiSteps.length > 0
                  ? 'Create with these steps'
                  : 'Create & open builder'}
            </button>
            <Link
              to="/workflows"
              className="px-4 py-2.5 text-sm text-gray-600 hover:text-gray-900 transition-colors"
            >
              Cancel
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
