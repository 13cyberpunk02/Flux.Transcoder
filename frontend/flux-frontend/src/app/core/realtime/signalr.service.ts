import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { TokenStore } from '../auth/token.store';
import { ChannelsStore } from '../../features/channels/channels.store';
import { ChannelHealthReport, ChannelStatus } from '../api/models';

const RETRY_DELAYS_MS = [0, 2000, 5000];
const MAX_RETRY_DELAY_MS = 10_000;

const retryDelay = (attempt: number) => RETRY_DELAYS_MS[attempt] ?? MAX_RETRY_DELAY_MS;

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly tokens = inject(TokenStore);
  private readonly store = inject(ChannelsStore);
  private connection?: signalR.HubConnection;
  private retryTimer?: ReturnType<typeof setTimeout>;

  /**
   * Идемпотентно. Держит подключение до stop(): withAutomaticReconnect не повторяет первый
   * start(), а со стандартной политикой сдаётся через 42 секунды — поэтому повторы без лимита.
   */
  start(): void {
    if (this.connection) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/channels', {
        accessTokenFactory: () => this.tokens.token() ?? '',
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: context => retryDelay(context.previousRetryCount),
      })
      .build();

    connection.on('channelHealth', (report: ChannelHealthReport) => {
      this.store.applyHealth(report);
    });

    connection.on('channelStatusChanged', (msg: { channelId: string; status: ChannelStatus }) => {
      this.store.patchStatus(msg.channelId, msg.status);
    });

    // пока связи не было, статусы могли поменяться
    connection.onreconnected(() => this.store.loadAll());
    // сервер может закрыть соединение без права на автоматический reconnect
    connection.onclose(() => this.scheduleConnect(connection, 1));

    this.connection = connection;
    void this.connect(connection, 0);
  }

  stop(): void {
    clearTimeout(this.retryTimer);
    const connection = this.connection;
    this.connection = undefined;
    connection?.stop().catch(() => {});
  }

  private async connect(connection: signalR.HubConnection, attempt: number): Promise<void> {
    if (connection !== this.connection) return;
    try {
      await connection.start();
      if (attempt > 0 && connection === this.connection) this.store.loadAll();
    } catch (err) {
      if (connection !== this.connection) return;
      console.warn('SignalR: нет связи, повторяю', err);
      this.scheduleConnect(connection, attempt + 1);
    }
  }

  private scheduleConnect(connection: signalR.HubConnection, attempt: number): void {
    if (connection !== this.connection) return;
    clearTimeout(this.retryTimer);
    this.retryTimer = setTimeout(() => void this.connect(connection, attempt), retryDelay(attempt));
  }
}
