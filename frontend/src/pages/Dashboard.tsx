import { GitBranch, Activity, Clock, AlertTriangle, CheckCircle } from 'lucide-react'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import { useDashboardStats } from '../hooks/useDashboard'
import { StatCard } from '../components/ui/StatCard'

export function Dashboard() {
  const { data: stats, isLoading } = useDashboardStats()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600" />
      </div>
    )
  }

  if (!stats) return null

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-500 mt-1">Workflow automation overview</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          title="Total Workflows"
          value={stats.totalWorkflows}
          icon={<GitBranch size={20} />}
          color="blue"
          subtitle={`${stats.activeWorkflows} active`}
        />
        <StatCard
          title="Running Executions"
          value={stats.runningExecutions}
          icon={<Activity size={20} />}
          color="green"
        />
        <StatCard
          title="Pending Approvals"
          value={stats.pendingApprovals}
          icon={<Clock size={20} />}
          color="orange"
          subtitle="Awaiting action"
        />
        <StatCard
          title="Failed Today"
          value={stats.failedExecutions}
          icon={<AlertTriangle size={20} />}
          color="red"
        />
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="xl:col-span-2 bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
          <h2 className="text-base font-semibold text-gray-800 mb-4">Execution Trend (7 days)</h2>
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={stats.recentTrend}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="date" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} />
              <Tooltip />
              <Legend />
              <Bar dataKey="completed" fill="#22c55e" name="Completed" radius={[3, 3, 0, 0]} />
              <Bar dataKey="failed" fill="#ef4444" name="Failed" radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
          <h2 className="text-base font-semibold text-gray-800 mb-4">Summary</h2>
          <div className="space-y-4">
            <SummaryRow icon={<CheckCircle size={16} className="text-green-500" />} label="Completed Today" value={stats.completedToday} />
            <SummaryRow icon={<Activity size={16} className="text-blue-500" />} label="Total Executions" value={stats.totalExecutions} />
            <SummaryRow icon={<GitBranch size={16} className="text-blue-500" />} label="Active Workflows" value={stats.activeWorkflows} />
            <SummaryRow icon={<AlertTriangle size={16} className="text-red-500" />} label="Failed Executions" value={stats.failedExecutions} />
          </div>
        </div>
      </div>
    </div>
  )
}

function SummaryRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: number }) {
  return (
    <div className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0">
      <div className="flex items-center gap-2 text-sm text-gray-600">
        {icon}
        {label}
      </div>
      <span className="font-semibold text-gray-900">{value}</span>
    </div>
  )
}
