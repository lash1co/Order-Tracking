import type { ConnectionStatus } from '../domain/types';

type Props = {
  connection: {
    status: ConnectionStatus;
    lastSyncAt?: string;
    connectionId?: string | null;
    error?: string;
  };
  onReconnect(): Promise<void>;
};

export function ConnectionBanner({ connection, onReconnect }: Props) {
  return (
    <section className={`connection-banner ${connection.status}`}>
      <div>
        <strong>{labelFor(connection.status)}</strong>
        <span>
          {connection.lastSyncAt
            ? `Última sincronización: ${new Intl.DateTimeFormat('es-CO', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
              }).format(new Date(connection.lastSyncAt))}`
            : 'Sin sincronización completa todavía'}
        </span>
        {connection.error && <small>{connection.error}</small>}
      </div>
      <button type="button" onClick={() => void onReconnect()}>
        Reconciliar ahora
      </button>
    </section>
  );
}

function labelFor(status: ConnectionStatus) {
  const labels: Record<ConnectionStatus, string> = {
    connected: 'Conectado',
    connecting: 'Conectando',
    reconnecting: 'Reconectando',
    disconnected: 'Desconectado'
  };
  return labels[status];
}
