import { Routes, Route } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { Dashboard } from './pages/Dashboard'
import { Workflows } from './pages/Workflows'
import { WorkflowDetail } from './pages/WorkflowDetail'
import { Executions } from './pages/Executions'
import { ExecutionDetail } from './pages/ExecutionDetail'
import { AutomationRules } from './pages/AutomationRules'

export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<Dashboard />} />
        <Route path="/workflows" element={<Workflows />} />
        <Route path="/workflows/:id" element={<WorkflowDetail />} />
        <Route path="/executions" element={<Executions />} />
        <Route path="/executions/:id" element={<ExecutionDetail />} />
        <Route path="/automation" element={<AutomationRules />} />
      </Route>
    </Routes>
  )
}
