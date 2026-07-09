import * as signalR from '@microsoft/signalr';
import type { DriverLocation, Order } from '../domain/types';

type Handlers = {
  onOrderChanged(order: Order): void;
  onDriverLocationChanged(driver: DriverLocation): void;
  onConnecting(): void;
  onConnected(connectionId?: string | null): void;
  onReconnecting(error?: Error): void;
  onDisconnected(error?: Error): void;
};

export class TrackingHubClient {
  private connection?: signalR.HubConnection;

  constructor(
    private readonly getToken: () => string | null,
    private readonly handlers: Handlers
  ) {}

  async connect() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return;

    this.handlers.onConnecting();
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/tracking', {
        accessTokenFactory: () => this.getToken() ?? '',
        transport:
          signalR.HttpTransportType.WebSockets |
          signalR.HttpTransportType.ServerSentEvents |
          signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('order.changed', this.handlers.onOrderChanged);
    this.connection.on('driver.location.changed', this.handlers.onDriverLocationChanged);
    this.connection.onreconnecting((error) => this.handlers.onReconnecting(error));
    this.connection.onreconnected((connectionId) => this.handlers.onConnected(connectionId));
    this.connection.onclose((error) => this.handlers.onDisconnected(error));

    await this.connection.start();
    await this.connection.invoke('SubscribeDashboard');
    this.handlers.onConnected(this.connection.connectionId);
  }

  async disconnect() {
    if (!this.connection) return;
    await this.connection.stop();
  }
}
