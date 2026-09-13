import { createBrowserRouter, Outlet } from 'react-router-dom';
import { AppShell } from './components/AppShell';
import { HomePage } from './pages/HomePage';
import { StartHerePage } from './pages/StartHerePage';
import { OnboardingPage } from './pages/OnboardingPage';
import { ReportsPage } from './pages/ReportsPage';
import { DocumentPage } from './pages/DocumentPage';
import { DocumentsPage } from './pages/DocumentsPage';
import { KbPage } from './pages/KbPage';
import { LoginPage } from './pages/LoginPage';
import { ForbiddenPage } from './pages/ForbiddenPage';
import { AdminPage } from './pages/AdminPage';
import { AuditPage } from './pages/AuditPage';
import { FeedbackTriagePage } from './pages/FeedbackTriagePage';
import { ReviewQueuePage } from './pages/ReviewQueuePage';
import { PublishPage } from './pages/PublishPage';
import { EditorPage } from './pages/EditorPage';
import { HistoryPage } from './pages/HistoryPage';
import { RequireAuth, RequireRole } from './auth/guards';
import { ADMIN_ROLES, APPROVE_ROLES, AUTHOR_ROLES } from './auth/roles';

function ProtectedLayout() {
  return (
    <RequireAuth>
      <AppShell>
        <Outlet />
      </AppShell>
    </RequireAuth>
  );
}

/**
 * Route Sprint 1. /login + /403 public; phần còn lại yêu cầu đăng nhập (RequireAuth).
 * /admin thêm RequireRole (§10.1). Các route S3/S6/S9… bổ sung theo BUILD_PLAN §4.
 */
export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/403', element: <ForbiddenPage /> },
  {
    element: <ProtectedLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/start', element: <StartHerePage /> },
      { path: '/onboarding', element: <OnboardingPage /> },
      { path: '/documents', element: <DocumentsPage /> },
      { path: '/kb', element: <KbPage /> },
      { path: '/documents/:docId', element: <DocumentPage /> },
      { path: '/documents/:docId/history', element: <HistoryPage /> },
      {
        element: <RequireRole anyOf={AUTHOR_ROLES} />,
        children: [
          { path: '/editor/new', element: <EditorPage /> },
          { path: '/editor/:versionId', element: <EditorPage /> },
        ],
      },
      {
        element: <RequireRole anyOf={APPROVE_ROLES} />,
        children: [
          { path: '/review', element: <ReviewQueuePage /> },
          { path: '/publish', element: <PublishPage /> },
        ],
      },
      {
        element: <RequireRole anyOf={ADMIN_ROLES} />,
        children: [
          { path: '/admin', element: <AdminPage /> },
          { path: '/audit', element: <AuditPage /> },
          { path: '/feedback', element: <FeedbackTriagePage /> },
          { path: '/reports', element: <ReportsPage /> },
        ],
      },
    ],
  },
]);
