import { useCallback, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ReactFlow, Background, Controls, MiniMap,
  useNodesState, useEdgesState, addEdge,
  type Node, type Edge, type Connection,
  ReactFlowProvider,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import { ArrowLeft, Save, Loader2, CheckCircle } from 'lucide-react'
import { useWorkflow, useSaveWorkflowForm } from '../hooks/useWorkflows'
import { workflowsApi } from '../api/workflows'
import { StepNode, type StepNodeData } from '../components/builder/StepNode'
import { StepPalette } from '../components/builder/StepPalette'
import { StepConfigPanel } from '../components/builder/StepConfigPanel'
import { FormBuilder } from '../components/builder/FormBuilder'
import type { FormField, StepType } from '../types'

type BuilderTab = 'steps' | 'form'

const nodeTypes = { stepNode: StepNode }

function BuilderCanvas() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: workflow, isLoading } = useWorkflow(id!)
  const saveForm = useSaveWorkflowForm()
  const [activeTab, setActiveTab] = useState<BuilderTab>('steps')
  const [formFields, setFormFields] = useState<FormField[]>([])
  const [formInitialized, setFormInitialized] = useState(false)
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([])
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([])
  const [selectedNode, setSelectedNode] = useState<Node | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [saveError, setSaveError] = useState('')
  const [initialized, setInitialized] = useState(false)
  const reactFlowWrapper = useRef<HTMLDivElement>(null)
  const [rfInstance, setRfInstance] = useState<ReturnType<typeof import('@xyflow/react').useReactFlow> | null>(null)

  // Seed form fields from loaded workflow (once)
  if (workflow && !formInitialized) {
    setFormInitialized(true)
    setFormFields(workflow.formSchema ?? [])
  }

  // Initialize nodes from loaded workflow (once)
  if (workflow && !initialized) {
    setInitialized(true)
    const initialNodes: Node[] = workflow.steps
      .sort((a, b) => a.order - b.order)
      .map((step, i) => ({
        id: step.id,
        type: 'stepNode',
        position: { x: 250, y: i * 200 + 50 },
        data: {
          name: step.name,
          type: step.type,
          config: step.config,
          approverEmail: undefined as string | undefined,
          onDelete: (nodeId: string) =>
            setNodes((nds) => nds.filter((n) => n.id !== nodeId)),
        } satisfies StepNodeData,
      }))

    const initialEdges: Edge[] = workflow.steps
      .filter((s) => s.nextStepId)
      .map((s) => ({
        id: `e-${s.id}-${s.nextStepId}`,
        source: s.id,
        target: s.nextStepId!,
        type: 'smoothstep',
        style: { stroke: '#94a3b8', strokeWidth: 2 },
      }))

    setNodes(initialNodes)
    setEdges(initialEdges)
  }

  const onConnect = useCallback(
    (connection: Connection) =>
      setEdges((eds) =>
        addEdge({ ...connection, type: 'smoothstep', style: { stroke: '#94a3b8', strokeWidth: 2 } }, eds)),
    [setEdges],
  )

  const onDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
  }, [])

  const onDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault()
      const type = e.dataTransfer.getData('application/reactflow') as StepType
      if (!type || !reactFlowWrapper.current) return

      const bounds = reactFlowWrapper.current.getBoundingClientRect()
      const position = rfInstance
        ? rfInstance.screenToFlowPosition({ x: e.clientX - bounds.left, y: e.clientY - bounds.top })
        : { x: e.clientX - bounds.left, y: e.clientY - bounds.top }

      const newId = crypto.randomUUID()
      const newNode: Node = {
        id: newId,
        type: 'stepNode',
        position,
        data: {
          name: `New ${type}`,
          type,
          config: {},
          onDelete: (nodeId: string) =>
            setNodes((nds) => nds.filter((n) => n.id !== nodeId)),
        } satisfies StepNodeData,
      }
      setNodes((nds) => [...nds, newNode])
    },
    [rfInstance, setNodes],
  )

  const onNodeClick = useCallback((_: React.MouseEvent, node: Node) => {
    setSelectedNode(node)
  }, [])

  const onPaneClick = useCallback(() => {
    setSelectedNode(null)
  }, [])

  const handleUpdateNode = useCallback(
    (nodeId: string, updates: Partial<StepNodeData>) => {
      setNodes((nds) =>
        nds.map((n) =>
          n.id === nodeId
            ? { ...n, data: { ...n.data, ...updates } }
            : n,
        ),
      )
      setSelectedNode((prev) =>
        prev?.id === nodeId ? { ...prev, data: { ...prev.data, ...updates } } : prev,
      )
    },
    [setNodes],
  )

  const handleSave = async () => {
    if (!id) return
    setSaving(true)
    setSaveError('')
    try {
      if (activeTab === 'form') {
        await saveForm.mutateAsync({ id, fields: formFields })
      } else {
        const sortedNodes = [...nodes].sort((a, b) => a.position.y - b.position.y)
        const steps = sortedNodes.map((node, i) => {
          const data = node.data as StepNodeData
          return {
            name: data.name,
            type: data.type,
            order: i + 1,
            config: data.config ?? {},
            approverEmail: data.approverEmail ?? null,
          }
        })
        await workflowsApi.saveSteps(id, steps)
      }
      setSaved(true)
      setTimeout(() => setSaved(false), 2000)
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setSaving(false)
    }
  }

  const onDragStart = (e: React.DragEvent, type: StepType) => {
    e.dataTransfer.setData('application/reactflow', type)
    e.dataTransfer.effectAllowed = 'move'
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 className="animate-spin text-blue-600" size={32} />
      </div>
    )
  }

  return (
    <div className="flex flex-col h-screen bg-gray-50">
      {/* Toolbar */}
      <div className="flex items-center gap-3 px-4 py-3 bg-white border-b border-gray-200 shadow-sm z-10">
        <button
          onClick={() => navigate(`/workflows/${id}`)}
          className="p-2 hover:bg-gray-100 rounded-lg text-gray-600"
        >
          <ArrowLeft size={18} />
        </button>
        <div className="flex-1 min-w-0">
          <h1 className="font-semibold text-gray-900 text-sm truncate">{workflow?.name}</h1>
          <p className="text-xs text-gray-400">
            {activeTab === 'steps'
              ? `${nodes.length} step${nodes.length !== 1 ? 's' : ''}`
              : `${formFields.length} form field${formFields.length !== 1 ? 's' : ''}`}
          </p>
        </div>

        {/* Tab switcher */}
        <div className="flex rounded-lg border border-gray-200 overflow-hidden">
          {(['steps', 'form'] as BuilderTab[]).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-3 py-1.5 text-xs font-medium transition-colors capitalize ${
                activeTab === tab
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-500 hover:bg-gray-50'
              }`}
            >
              {tab === 'steps' ? 'Steps' : 'Trigger Form'}
            </button>
          ))}
        </div>

        {saveError ? (
          <p className="text-xs text-red-500 hidden sm:block">{saveError}</p>
        ) : (
          <p className="text-xs text-gray-400 hidden sm:block">
            {activeTab === 'steps'
              ? 'Drag steps from the palette → drop on canvas → click to configure'
              : 'Define fields collected when this workflow is triggered'}
          </p>
        )}
        <button
          onClick={handleSave}
          disabled={saving}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-60 transition-colors"
        >
          {saving ? (
            <><Loader2 size={14} className="animate-spin" /> Saving…</>
          ) : saved ? (
            <><CheckCircle size={14} /> Saved</>
          ) : (
            <><Save size={14} /> Save</>
          )}
        </button>
      </div>

      {/* Form builder panel */}
      {activeTab === 'form' && (
        <div className="flex-1 overflow-y-auto p-6 bg-gray-50">
          <div className="max-w-2xl mx-auto">
            <div className="mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Trigger Form</h2>
              <p className="text-sm text-gray-500 mt-0.5">
                Fields shown to the user before they can trigger this workflow. Values are passed as input data.
              </p>
            </div>
            <FormBuilder fields={formFields} onChange={setFormFields} />
          </div>
        </div>
      )}

      {/* Main canvas area */}
      {activeTab === 'steps' && (
      <div className="flex flex-1 overflow-hidden">
        <StepPalette onDragStart={onDragStart} />

        <div className="flex-1 relative" ref={reactFlowWrapper}>
          <ReactFlow
            nodes={nodes}
            edges={edges}
            nodeTypes={nodeTypes}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onDrop={onDrop}
            onDragOver={onDragOver}
            onNodeClick={onNodeClick}
            onPaneClick={onPaneClick}
            onInit={(instance) => setRfInstance(instance as unknown as typeof rfInstance)}
            fitView
            fitViewOptions={{ padding: 0.3 }}
            deleteKeyCode="Delete"
          >
            <Background color="#e2e8f0" gap={20} />
            <Controls className="!shadow-md !rounded-lg" />
            <MiniMap
              nodeColor={(n) => {
                const type = (n.data as StepNodeData).type
                const colors: Record<string, string> = {
                  Approval: '#fb923c', Notification: '#a855f7', Delay: '#9ca3af',
                  DataTransform: '#14b8a6', Webhook: '#22c55e', Condition: '#eab308',
                }
                return colors[type] ?? '#3b82f6'
              }}
              className="!rounded-lg !shadow-md"
            />
            {nodes.length === 0 && (
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                <div className="text-center">
                  <p className="text-gray-400 text-lg font-medium">Canvas is empty</p>
                  <p className="text-gray-300 text-sm mt-1">Drag step types from the left panel</p>
                </div>
              </div>
            )}
          </ReactFlow>
        </div>

        {selectedNode && (
          <StepConfigPanel
            node={selectedNode}
            onClose={() => setSelectedNode(null)}
            onUpdate={handleUpdateNode}
            formFields={formFields}
          />
        )}
      </div>
      )}
    </div>
  )
}

export function WorkflowBuilder() {
  return (
    <ReactFlowProvider>
      <BuilderCanvas />
    </ReactFlowProvider>
  )
}
