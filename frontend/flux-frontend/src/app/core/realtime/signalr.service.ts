import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { TokenStore } from '../auth/token.store';
import { ChannelsStore } from '../../features/channels/channels.store';
import { ChannelHealthReport } from '../api/models';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly tokens = inject(TokenStore);
  private readonly store = inject(ChannelsStore);
  private connection?: signalR.HubConnection;

  start(): void {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/channels', {
        accessTokenFactory: () => this.tokens.token() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('channelHealth', (report: ChannelHealthReport) => {
      this.store.applyHealth(report);
    });

    this.connection.on('channelStatusChanged', (msg: { channelId: string; status: string }) => {
      this.store.patchStatus(msg.channelId, msg.status as any);
    });

    this.connection.start().catch(err => console.error('SignalR:', err));
  }
}
