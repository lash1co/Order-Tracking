import { useState } from 'react';

type Props = {
  token: string | null;
  onSave(token: string | null): void;
};

export function TokenForm({ token, onSave }: Props) {
  const [value, setValue] = useState(token ?? '');

  return (
    <section className="panel token-panel">
      <div>
        <span className="eyebrow">JWT</span>
        <h2>Conexión segura al API</h2>
        <p>Pega un token válido para consumir endpoints protegidos y abrir el hub SignalR.</p>
      </div>
      <label>
        Token bearer
        <textarea value={value} onChange={(event) => setValue(event.target.value)} placeholder="eyJhbGciOi..." rows={4} />
      </label>
      <div className="button-row">
        <button type="button" onClick={() => onSave(value.trim() || null)}>
          Guardar y reconectar
        </button>
        <button
          className="secondary-button"
          type="button"
          onClick={() => {
            setValue('');
            onSave(null);
          }}
        >
          Limpiar
        </button>
      </div>
    </section>
  );
}
