import { Navigate, Routes, Route } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { Dashboard } from './pages/Dashboard'
import { Login } from './pages/Login'
import { Register } from './pages/Register'
import { Workflows } from './pages/Workflows'
import { WorkflowDetail } from './pages/WorkflowDetail'
import { WorkflowNew } from './pages/WorkflowNew'
import { WorkflowBuilder } from './pages/WorkflowBuilder'
import { Executions } from './pages/Executions'
import { ExecutionDetail } from './pages/ExecutionDetail'
import { AutomationRules } from './pages/AutomationRules'
import { Approvals } from './pages/Approvals'
import { AuditLogPage } from './pages/AuditLogPage'
import { Settings } from './pages/Settings'
import { Team } from './pages/Team'
import { AcceptInvite } from './pages/AcceptInvite'

function RequireAuth({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth()
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/accept-invite" element={<AcceptInvite />} />

      {/* Builder is full-screen — outside AppLayout */}
      <Route path="/workflows/:id/builder" element={
        <RequireAuth><WorkflowBuilder /></RequireAuth>
      } />

      <Route element={<RequireAuth><AppLayout /></RequireAuth>}>
        <Route path="/" element={<Dashboard />} />
        <Route path="/workflows" element={<Workflows />} />
        <Route path="/workflows/new" element={<WorkflowNew />} />
        <Route path="/workflows/:id" element={<WorkflowDetail />} />
        <Route path="/executions" element={<Executions />} />
        <Route path="/executions/:id" element={<ExecutionDetail />} />
        <Route path="/automation" element={<AutomationRules />} />
        <Route path="/approvals" element={<Approvals />} />
        <Route path="/team" element={<Team />} />
        <Route path="/audit" element={<AuditLogPage />} />
        <Route path="/settings" element={<Settings />} />
      </Route>
    </Routes>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  )
}
