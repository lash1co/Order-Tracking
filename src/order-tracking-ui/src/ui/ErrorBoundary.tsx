import React from 'react';

type State = {
  hasError: boolean;
  message?: string;
};

export class ErrorBoundary extends React.Component<React.PropsWithChildren, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, message: error.message };
  }

  render() {
    if (this.state.hasError) {
      return (
        <main className="app-shell">
          <section className="hero-card">
            <div>
              <span className="eyebrow">Error boundary</span>
              <h1>Algo falló en el dashboard</h1>
              <p>{this.state.message ?? 'Error desconocido'}</p>
            </div>
            <button type="button" onClick={() => window.location.reload()}>
              Recargar
            </button>
          </section>
        </main>
      );
    }

    return this.props.children;
  }
}
