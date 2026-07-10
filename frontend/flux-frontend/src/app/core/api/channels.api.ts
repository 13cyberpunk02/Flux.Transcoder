import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {ChannelView, CreateChannelRequest, MetricPoint} from './models';



@Injectable({ providedIn: 'root' })
export class ChannelsApi {
  private readonly http = inject(HttpClient);

  getById(id: string) { return this.http.get<ChannelView>(`/api/channels/${id}`); }
  getAll() { return this.http.get<ChannelView[]>('/api/channels'); }
  create(body: CreateChannelRequest) { return this.http.post<{ channelId: string }>('/api/channels', body); }
  delete(id: string) { return this.http.delete(`/api/channels/${id}`); }
  start(id: string) { return this.http.post(`/api/channels/${id}/start`, {}); }
  stop(id: string) { return this.http.post(`/api/channels/${id}/stop`, {}); }
  restart(id: string) { return this.http.post(`/api/channels/${id}/restart`, {}); }
  updateRetention(id: string, retentionMinutes: number) {
    return this.http.put(`/api/channels/${id}/retention`, { retentionMinutes });
  }
  getMetrics(id: string, minutes: number = 60) {
    return this.http.get<MetricPoint[]>(`/api/channels/${id}/metrics?minutes=${minutes}`);
  }
}
