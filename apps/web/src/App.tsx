import { RouterProvider } from 'react-router-dom';
import { AuthProvider as OidcLibProvider } from 'react-oidc-context';
import { ToastProvider } from './components/Toast';
import { AuthProvider } from './auth/AuthContext';
import { OIDC_ENABLED, oidcConfig } from './auth/oidcConfig';
import { router } from './router';

export function App() {
  const tree = (
    <AuthProvider>
      <ToastProvider>
        <RouterProvider router={router} />
      </ToastProvider>
    </AuthProvider>
  );

  // OIDC bật (VITE_OIDC_AUTHORITY) → bọc provider react-oidc-context để OidcAuthProvider tiêu thụ.
  return OIDC_ENABLED ? <OidcLibProvider {...oidcConfig}>{tree}</OidcLibProvider> : tree;
}
