import L from 'leaflet';
import { useEffect, useMemo, useRef } from 'react';
import type { DriverLocation } from '../domain/types';

type Props = {
  drivers: DriverLocation[];
};

export function DashboardMap({ drivers }: Props) {
  const mapElementRef = useRef<HTMLDivElement | null>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markerLayerRef = useRef<L.LayerGroup | null>(null);
  const center = useMemo(() => calculateCenter(drivers), [drivers]);

  useEffect(() => {
    if (!mapElementRef.current || mapRef.current) return;

    mapRef.current = L.map(mapElementRef.current, {
      zoomControl: false,
      attributionControl: false
    }).setView(center, 13);

    L.control.zoom({ position: 'bottomright' }).addTo(mapRef.current);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19
    }).addTo(mapRef.current);
    markerLayerRef.current = L.layerGroup().addTo(mapRef.current);
  }, [center]);

  useEffect(() => {
    if (!mapRef.current || !markerLayerRef.current) return;

    markerLayerRef.current.clearLayers();
    drivers.forEach((driver) => {
      L.circleMarker([driver.latitude, driver.longitude], {
        radius: 9,
        weight: 2,
        color: '#f8fafc',
        fillColor: colorFor(driver.status),
        fillOpacity: 0.95
      })
        .bindPopup(`<strong>${escapeHtml(driver.name)}</strong><br/>${escapeHtml(driver.status)}`)
        .addTo(markerLayerRef.current!);
    });

    if (drivers.length > 0) {
      mapRef.current.setView(center, 13, { animate: true });
    }
  }, [center, drivers]);

  return (
    <section className="panel map-panel">
      <div className="panel-header">
        <div>
          <span className="eyebrow">Live map</span>
          <h2>Conductores</h2>
        </div>
        <span className="pill">{drivers.length} activos</span>
      </div>
      <div ref={mapElementRef} className="map-canvas" aria-label="Mapa de conductores" />
    </section>
  );
}

function calculateCenter(drivers: DriverLocation[]): L.LatLngExpression {
  if (drivers.length === 0) return [4.711, -74.0721];
  const sum = drivers.reduce(
    (acc, driver) => ({
      latitude: acc.latitude + driver.latitude,
      longitude: acc.longitude + driver.longitude
    }),
    { latitude: 0, longitude: 0 }
  );
  return [sum.latitude / drivers.length, sum.longitude / drivers.length];
}

function colorFor(status: string) {
  if (status === 'Available') return '#22c55e';
  if (status === 'Delivering' || status === 'Assigned') return '#f97316';
  return '#64748b';
}

function escapeHtml(value: string) {
  return value.replace(/[&<>"']/g, (character) => {
    const replacements: Record<string, string> = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#039;'
    };
    return replacements[character];
  });
}
