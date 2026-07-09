import type { Toast } from '../domain/types';

type Props = {
  toasts: Toast[];
  onDismiss(id: string): void;
};

export function ToastRegion({ toasts, onDismiss }: Props) {
  return (
    <aside className="toast-region" aria-live="polite" aria-label="Notificaciones">
      {toasts.map((toast) => (
        <button key={toast.id} type="button" className={`toast ${toast.tone}`} onClick={() => onDismiss(toast.id)}>
          {toast.message}
        </button>
      ))}
    </aside>
  );
}
