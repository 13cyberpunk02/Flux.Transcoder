import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ChannelView, CreateChannelRequest } from './models';

@Injectable({ providedIn: 'root' })
export class ChannelsApi {
  private readonly http = inject(HttpClient);

  getById(id: string) { return this.http.get<ChannelView>(`/api/channels/${id}`); }
  create(body: CreateChannelRequest) { return this.http.post<{ channelId: string }>('/api/channels', body); }
  start(id: string) { return this.http.post(`/api/channels/${id}/start`, {}); }
  stop(id: string) { return this.http.post(`/api/channels/${id}/stop`, {}); }
}
