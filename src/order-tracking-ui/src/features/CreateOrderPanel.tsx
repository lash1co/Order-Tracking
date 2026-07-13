import { type FormEvent, useMemo, useState } from 'react';
import type { CreateOrderRequest } from '../services/apiClient';

type Props = {
  onCreate(request: CreateOrderRequest): Promise<void>;
};

type DraftItem = {
  id: string;
  menuItemId: string;
  quantity: string;
  price: string;
};

const defaultEta = () => {
  const date = new Date(Date.now() + 45 * 60 * 1000);
  date.setSeconds(0, 0);
  return date.toISOString().slice(0, 16);
};

const newItem = (): DraftItem => ({
  id: crypto.randomUUID(),
  menuItemId: crypto.randomUUID(),
  quantity: '1',
  price: '12.50'
});

export function CreateOrderPanel({ onCreate }: Props) {
  const [customerId, setCustomerId] = useState<string>(() => crypto.randomUUID());
  const [restaurantId, setRestaurantId] = useState<string>(() => crypto.randomUUID());
  const [estimatedDelivery, setEstimatedDelivery] = useState(defaultEta);
  const [items, setItems] = useState<DraftItem[]>(() => [newItem()]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const total = useMemo(
    () =>
      items.reduce((sum, item) => {
        const quantity = Number(item.quantity);
        const price = Number(item.price);
        if (!Number.isFinite(quantity) || !Number.isFinite(price)) return sum;
        return sum + quantity * price;
      }, 0),
    [items]
  );

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    const validationError = validate(customerId, restaurantId, estimatedDelivery, items);
    if (validationError) {
      setError(validationError);
      return;
    }

    const request: CreateOrderRequest = {
      customerId,
      restaurantId,
      estimatedDelivery: new Date(estimatedDelivery).toISOString(),
      items: items.map((item) => ({
        menuItemId: item.menuItemId,
        quantity: Number(item.quantity),
        price: Number(item.price)
      }))
    };

    setIsSubmitting(true);
    try {
      await onCreate(request);
      setCustomerId(crypto.randomUUID());
      setRestaurantId(crypto.randomUUID());
      setEstimatedDelivery(defaultEta());
      setItems([newItem()]);
    } catch {
      setError('La API rechazó la creación. Revisa el toast para ver si fue token, permisos o validación.');
    } finally {
      setIsSubmitting(false);
    }
  }

  function updateItem(id: string, patch: Partial<DraftItem>) {
    setItems((current) => current.map((item) => (item.id === id ? { ...item, ...patch } : item)));
  }

  return (
    <section className="panel create-order-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Order command</span>
          <h2>Crear orden</h2>
          <p>Prueba el caso Admin/Dispatcher desde la UI. Driver debería recibir permiso denegado.</p>
        </div>
        <span className="pill">Total demo: ${total.toFixed(2)}</span>
      </div>

      <form className="entity-form" onSubmit={(event) => void submit(event)}>
        <div className="form-grid">
          <label>
            CustomerId
            <input value={customerId} onChange={(event) => setCustomerId(event.target.value)} />
          </label>
          <label>
            RestaurantId
            <input value={restaurantId} onChange={(event) => setRestaurantId(event.target.value)} />
          </label>
          <label>
            Estimated delivery
            <input
              min={new Date().toISOString().slice(0, 16)}
              type="datetime-local"
              value={estimatedDelivery}
              onChange={(event) => setEstimatedDelivery(event.target.value)}
            />
          </label>
        </div>

        <div className="items-editor">
          <div className="items-editor-header">
            <strong>Items</strong>
            <button className="secondary-button" type="button" onClick={() => setItems((current) => [...current, newItem()])}>
              Agregar item
            </button>
          </div>

          {items.map((item, index) => (
            <div className="item-row" key={item.id}>
              <label>
                MenuItemId #{index + 1}
                <input value={item.menuItemId} onChange={(event) => updateItem(item.id, { menuItemId: event.target.value })} />
              </label>
              <label>
                Cantidad
                <input
                  min="1"
                  step="1"
                  type="number"
                  value={item.quantity}
                  onChange={(event) => updateItem(item.id, { quantity: event.target.value })}
                />
              </label>
              <label>
                Precio
                <input
                  min="0.01"
                  step="0.01"
                  type="number"
                  value={item.price}
                  onChange={(event) => updateItem(item.id, { price: event.target.value })}
                />
              </label>
              <button
                className="secondary-button danger-button"
                disabled={items.length === 1}
                type="button"
                onClick={() => setItems((current) => current.filter((candidate) => candidate.id !== item.id))}
              >
                Quitar
              </button>
            </div>
          ))}
        </div>

        {error && <p className="inline-error form-error">{error}</p>}

        <div className="button-row">
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Creando...' : 'Crear orden'}
          </button>
          <button
            className="secondary-button"
            type="button"
            onClick={() => {
              setCustomerId(crypto.randomUUID());
              setRestaurantId(crypto.randomUUID());
              setEstimatedDelivery(defaultEta());
              setItems([newItem()]);
              setError(null);
            }}
          >
            Reiniciar demo
          </button>
        </div>
      </form>
    </section>
  );
}

function validate(customerId: string, restaurantId: string, estimatedDelivery: string, items: DraftItem[]) {
  if (!isGuid(customerId)) return 'CustomerId debe ser un GUID válido.';
  if (!isGuid(restaurantId)) return 'RestaurantId debe ser un GUID válido.';
  if (!estimatedDelivery || new Date(estimatedDelivery).getTime() <= Date.now()) return 'Estimated delivery debe ser una fecha futura.';
  if (items.length === 0) return 'La orden necesita al menos un item.';

  for (const [index, item] of items.entries()) {
    if (!isGuid(item.menuItemId)) return `MenuItemId del item ${index + 1} debe ser un GUID válido.`;
    if (!Number.isInteger(Number(item.quantity)) || Number(item.quantity) <= 0) return `Cantidad del item ${index + 1} debe ser mayor a 0.`;
    if (!Number.isFinite(Number(item.price)) || Number(item.price) <= 0) return `Precio del item ${index + 1} debe ser mayor a 0.`;
  }

  return null;
}

function isGuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}
