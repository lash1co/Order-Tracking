import 'leaflet/dist/leaflet.css';
import './styles.css';
import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './App';
import { DashboardProvider } from './state/DashboardContext';
import { ErrorBoundary } from './ui/ErrorBoundary';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ErrorBoundary>
      <DashboardProvider>
        <App />
      </DashboardProvider>
    </ErrorBoundary>
  </React.StrictMode>
);
