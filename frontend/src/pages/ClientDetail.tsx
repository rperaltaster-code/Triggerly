import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Plus, Pencil, Trash2, CheckCircle, XCircle } from 'lucide-react'
import {
  useClient,
  useClientServices,
  useServiceTypes,
  useUpdateClient,
  useUpdateClientService,
  useAddClientService,
  useRemoveClientService,
} from '../hooks/useClients'
import { useWorkflows } from '../hooks/useWorkflows'
import { useRole } from '../hooks/useRole'
import { ClientModal, FILING_PERIOD_LABELS } from './Clients'
import type { ClientService, FilingPeriod } from '../types'
import type { SaveClientPayload, SaveClientServicePayload } from '../api/clients'

function ServiceModal({
  initial,
  onSave,
  onClose,
  saving,
}: {
  initial?: ClientService
  onSave: (data: SaveClientServicePayload) => void
  onClose: () => void
  saving: boolean
}) {
  const { data: serviceTypes } = useServiceTypes()
  const { data: workflowsResult } = useWorkflows({ status: 'Active' })
  const workflows = workflowsResult?.items ?? []

  const [form, setForm] = useState<SaveClientServicePayload>(
    initial
      ? {
          serviceTypeId: initial.serviceTypeId,
          workflowId: initial.workflowId,
          filingPeriod: initial.filingPeriod,
          isActive: initial.isActive,
          notes: initial.notes,
        }
      : {
          serviceTypeId: serviceTypes?.[0]?.id ?? '',
          workflowId: workflows?.[0]?.id ?? '',
          filingPeriod: 'Annual',
          isActive: true,
          notes: null,
        },
  )

  const filingPeriods: FilingPeriod[] = ['Monthly', 'TwoMonthly', 'SixMonthly', 'Annual', 'OneOff']

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-4">{initial ? 'Edit Service' : 'Add Service'}</h2>
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Service Type *</label>
            <select
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.serviceTypeId}
              onChange={(e) => setForm((f) => ({ ...f, serviceTypeId: e.target.value }))}
            >
              {serviceTypes?.map((st) => (
                <option key={st.id} value={st.id}>{st.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Workflow *</label>
            <select
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.workflowId}
              onChange={(e) => setForm((f) => ({ ...f, workflowId: e.target.value }))}
            >
              {workflows.map((w) => (
                <option key={w.id} value={w.id}>{w.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Filing Period *</label>
            <select
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={form.filingPeriod}
              onChange={(e) => setForm((f) => ({ ...f, filingPeriod: e.target.value as FilingPeriod }))}
            >
              {filingPeriods.map((p) => (
                <option key={p} value={p}>{FILING_PERIOD_LABELS[p]}</option>
              ))}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isActive"
              checked={form.isActive}
              onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
              className="rounded"
            />
            <label htmlFor="isActive" className="text-sm text-gray-700">Active</label>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
            <textarea
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              rows={2}
              value={form.notes ?? ''}
              onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value || null }))}
            />
          </div>
        </div>
        <div className="flex justify-end gap-2 mt-6">
          <button onClick={onClose} className="px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-lg">
            Cancel
          </button>
          <button
            onClick={() => onSave(form)}
            disabled={saving || !form.serviceTypeId || !form.workflowId}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default function ClientDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { canEdit, isManager } = useRole()

  const { data: client, isLoading } = useClient(id)
  const { data: services } = useClientServices(id)
  const updateClient = useUpdateClient(id!)
  const addService = useAddClientService(id!)
  const removeService = useRemoveClientService(id!)

  const [editClient, setEditClient] = useState(false)
  const [showAddService, setShowAddService] = useState(false)

  if (isLoading) return <div className="flex justify-center py-12"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" /></div>
  if (!client) return <div className="text-center py-12 text-gray-500">Client not found</div>

  const handleUpdateClient = async (data: SaveClientPayload) => {
    await updateClient.mutateAsync(data)
    setEditClient(false)
  }

  const handleAddService = async (data: SaveClientServicePayload) => {
    await addService.mutateAsync(data)
    setShowAddService(false)
  }

  const handleRemoveService = async (svc: ClientService) => {
    if (!confirm(`Remove service "${svc.serviceTypeName}"?`)) return
    await removeService.mutateAsync(svc.id)
  }

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <button
        onClick={() => navigate('/clients')}
        className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
      >
        <ArrowLeft size={16} />
        Back to Clients
      </button>

      {/* Client header */}
      <div className="bg-white rounded-xl border p-6 mb-6">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{client.name}</h1>
            <p className="text-gray-500 text-sm mt-0.5">{client.email}</p>
            {client.phone && <p className="text-gray-500 text-sm">{client.phone}</p>}
          </div>
          {canEdit && (
            <button
              onClick={() => setEditClient(true)}
              className="flex items-center gap-1 text-sm text-gray-600 hover:text-gray-900 border rounded-lg px-3 py-2 hover:bg-gray-50"
            >
              <Pencil size={14} />
              Edit
            </button>
          )}
        </div>

        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6">
          {client.irdNumber && (
            <div>
              <p className="text-xs text-gray-400 uppercase font-medium">IRD Number</p>
              <p className="text-sm font-medium mt-0.5">{client.irdNumber}</p>
            </div>
          )}
          {client.balanceDate && (
            <div>
              <p className="text-xs text-gray-400 uppercase font-medium">Balance Date</p>
              <p className="text-sm font-medium mt-0.5">{client.balanceDate}</p>
            </div>
          )}
          {client.notes && (
            <div className="col-span-2">
              <p className="text-xs text-gray-400 uppercase font-medium">Notes</p>
              <p className="text-sm text-gray-600 mt-0.5">{client.notes}</p>
            </div>
          )}
        </div>
      </div>

      {/* Services */}
      <div className="bg-white rounded-xl border p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Services</h2>
          {canEdit && (
            <button
              onClick={() => setShowAddService(true)}
              className="flex items-center gap-1 text-sm bg-blue-600 text-white rounded-lg px-3 py-2 hover:bg-blue-700"
            >
              <Plus size={14} />
              Add Service
            </button>
          )}
        </div>

        {!services?.length ? (
          <div className="text-center py-8 text-gray-400 text-sm">
            No services assigned yet.
          </div>
        ) : (
          <div className="divide-y divide-gray-100">
            {services.map((svc) => (
              <ServiceRow
                key={svc.id}
                svc={svc}
                clientId={id!}
                canEdit={canEdit}
                isManager={isManager}
                onRemove={() => handleRemoveService(svc)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Modals */}
      {editClient && (
        <ClientModal
          initial={{
            name: client.name,
            email: client.email,
            phone: client.phone,
            balanceDate: client.balanceDate,
            irdNumber: client.irdNumber,
            notes: client.notes,
          }}
          onSave={handleUpdateClient}
          onClose={() => setEditClient(false)}
          saving={updateClient.isPending}
        />
      )}

      {showAddService && (
        <ServiceModal
          onSave={handleAddService}
          onClose={() => setShowAddService(false)}
          saving={addService.isPending}
        />
      )}
    </div>
  )
}

function ServiceRow({
  svc,
  clientId,
  canEdit,
  isManager,
  onRemove,
}: {
  svc: ClientService
  clientId: string
  canEdit: boolean
  isManager: boolean
  onRemove: () => void
}) {
  const [editing, setEditing] = useState(false)
  const updateService = useUpdateClientService(clientId, svc.id)

  const handleSave = async (data: SaveClientServicePayload) => {
    await updateService.mutateAsync(data)
    setEditing(false)
  }

  return (
    <div className="py-4">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <span className="font-medium text-gray-900">{svc.serviceTypeName}</span>
            {svc.isActive ? (
              <CheckCircle size={14} className="text-green-500" />
            ) : (
              <XCircle size={14} className="text-gray-400" />
            )}
          </div>
          <div className="text-sm text-gray-500 mt-0.5 flex flex-wrap gap-3">
            <span>Workflow: <span className="text-gray-700">{svc.workflowName ?? 'Unknown'}</span></span>
            <span>Period: <span className="text-gray-700">{FILING_PERIOD_LABELS[svc.filingPeriod]}</span></span>
            {svc.lastFiledAt && (
              <span>Last filed: <span className="text-gray-700">{new Date(svc.lastFiledAt).toLocaleDateString()}</span></span>
            )}
            {svc.nextDueAt && (
              <span>Next due: <span className="text-gray-700">{new Date(svc.nextDueAt).toLocaleDateString()}</span></span>
            )}
          </div>
          {svc.notes && <p className="text-xs text-gray-400 mt-1">{svc.notes}</p>}
        </div>
        {canEdit && (
          <div className="flex items-center gap-2 ml-4">
            <button
              onClick={() => setEditing(true)}
              className="text-gray-400 hover:text-gray-600"
            >
              <Pencil size={14} />
            </button>
            {isManager && (
              <button onClick={onRemove} className="text-red-400 hover:text-red-600">
                <Trash2 size={14} />
              </button>
            )}
          </div>
        )}
      </div>

      {editing && (
        <ServiceModal
          initial={svc}
          onSave={handleSave}
          onClose={() => setEditing(false)}
          saving={updateService.isPending}
        />
      )}
    </div>
  )
}
