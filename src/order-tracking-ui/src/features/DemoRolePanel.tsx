import { useState } from 'react';
import { createDemoToken } from '../services/apiClient';

type DemoRole = 'Admin' | 'Dispatcher' | 'Driver';

type Props = {
  onToken(token: string): void;
};

const roleCards: Array<{
  role: DemoRole;
  title: string;
  description: string;
  permissions: string[];
}> = [
  {
    role: 'Admin',
    title: 'Admin',
    description: 'Prepara y controla todo el sistema demo.',
    permissions: ['Crear órdenes', 'Crear drivers', 'Asignar drivers', 'Cambiar estados']
  },
  {
    role: 'Dispatcher',
    title: 'Dispatcher',
    description: 'Opera pedidos activos y coordina repartidores.',
    permissions: ['Crear órdenes', 'Crear drivers', 'Asignar drivers', 'Cambiar estados']
  },
  {
    role: 'Driver',
    title: 'Driver',
    description: 'Ejecuta la entrega desde la perspectiva del repartidor.',
    permissions: ['Cambiar estados', 'Actualizar ubicación por API', 'Ver dashboard']
  }
];

export function DemoRolePanel({ onToken }: Props) {
  const [loadingRole, setLoadingRole] = useState<DemoRole | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function useRole(role: DemoRole) {
    setLoadingRole(role);
    setError(null);

    try {
      const response = await createDemoToken([role], `${role.toLowerCase()}-demo`);
      onToken(response.token);
    } catch (unknownError) {
      const message = unknownError instanceof Error ? unknownError.message : 'No se pudo generar el token demo.';
      setError(message);
    } finally {
      setLoadingRole(null);
    }
  }

  return (
    <section className="panel demo-role-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Modo tutorial</span>
          <h2>Probar como rol demo</h2>
          <p>Genera un JWT local y reconecta el dashboard sin copiar/pegar tokens manualmente.</p>
        </div>
      </div>

      <div className="role-card-grid">
        {roleCards.map((card) => (
          <article className="role-card" key={card.role}>
            <div>
              <h3>{card.title}</h3>
              <p>{card.description}</p>
            </div>
            <ul>
              {card.permissions.map((permission) => (
                <li key={permission}>{permission}</li>
              ))}
            </ul>
            <button type="button" disabled={loadingRole !== null} onClick={() => void useRole(card.role)}>
              {loadingRole === card.role ? 'Generando...' : `Usar ${card.title}`}
            </button>
          </article>
        ))}
      </div>

      {error && (
        <p className="inline-error">
          {error} Si estás fuera de Docker Compose, revisa que `DemoTokens:Enabled` esté activo o usa el script manual.
        </p>
      )}
    </section>
  );
}
