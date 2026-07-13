import type { AuthInfo, UserRole } from '../domain/types';
import { hasAnyRole } from '../services/authToken';

type Props = {
  auth: AuthInfo;
};

type Permission = {
  label: string;
  roles: UserRole[];
};

const permissions: Permission[] = [
  { label: 'Crear órdenes', roles: ['Admin', 'Dispatcher'] },
  { label: 'Crear drivers', roles: ['Admin', 'Dispatcher'] },
  { label: 'Asignar drivers', roles: ['Admin', 'Dispatcher'] },
  { label: 'Actualizar ubicación', roles: ['Admin', 'Driver'] },
  { label: 'Cambiar estados', roles: ['Admin', 'Dispatcher', 'Driver'] },
  { label: 'Consultar métricas', roles: ['Admin', 'Dispatcher', 'Driver'] }
];

export function AuthStatusPanel({ auth }: Props) {
  const status = statusLabel(auth);

  return (
    <section className="panel auth-status-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Sesión actual</span>
          <h2>Roles y permisos visibles</h2>
          <p>La UI decodifica el JWT para explicar permisos; el backend sigue siendo quien autoriza de verdad.</p>
        </div>
        <span className={`pill ${status.tone}`}>{status.label}</span>
      </div>

      <div className="auth-summary-grid">
        <article className="auth-summary-card">
          <span>Usuario</span>
          <strong>{auth.subject ?? 'Sin usuario'}</strong>
          <small>{auth.expiresAt ? `Expira: ${formatDate(auth.expiresAt)}` : 'Sin expiración legible'}</small>
        </article>
        <article className="auth-summary-card">
          <span>Roles activos</span>
          <strong>{auth.roles.length > 0 ? auth.roles.join(', ') : 'Ninguno'}</strong>
          <small>{auth.isValidToken ? 'Token decodificado localmente' : 'Elige un rol demo o pega un JWT válido'}</small>
        </article>
      </div>

      <div className="permission-grid">
        {permissions.map((permission) => {
          const allowed = hasAnyRole(auth, permission.roles);
          return (
            <article className={`permission-card ${allowed ? 'allowed' : 'denied'}`} key={permission.label}>
              <strong>{permission.label}</strong>
              <span>{allowed ? 'Permitido para este rol' : `Requiere ${permission.roles.join(' o ')}`}</span>
            </article>
          );
        })}
      </div>
    </section>
  );
}

function statusLabel(auth: AuthInfo) {
  if (!auth.isValidToken) return { label: 'Sin token', tone: 'warn-pill' };
  if (auth.isExpired) return { label: 'Token expirado', tone: 'warn-pill' };
  return { label: 'Token válido', tone: 'ok-pill' };
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('es-CO', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  }).format(new Date(value));
}
