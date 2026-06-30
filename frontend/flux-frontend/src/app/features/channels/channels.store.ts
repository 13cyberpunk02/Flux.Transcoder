import { Injectable, inject, signal, computed } from '@angular/core';
import { ChannelsApi } from '../../core/api/channels.api';
import {ChannelView, ChannelHealthReport, CreateChannelRequest} from '../../core/api/models';

@Injectable({ providedIn: 'root' })
export class ChannelsStore {
  private readonly api = inject(ChannelsApi);

  private readonly _channels = signal<ChannelView[]>([]);
  private readonly _health = signal<Record<string, ChannelHealthReport>>({});
  private readonly _loading = signal(false);

  readonly channels = this._channels.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly health = this._health.asReadonly();

  readonly runningCount = computed(() =>
    this._channels().filter(c => c.status === 'Running').length);

  // загрузка одного канала (списка эндпоинта у тебя пока нет — добавишь GET /api/channels)
  loadChannel(id: string): void {
    this._loading.set(true);
    this.api.getById(id).subscribe({
      next: ch => {
        this._channels.update(list => {
          const rest = list.filter(c => c.channelId !== ch.channelId);
          return [...rest, ch];
        });
        this._loading.set(false);
      },
      error: () => this._loading.set(false)
    });
  }

  loadAll(): void {
    this._loading.set(true);
    this.api.getAll().subscribe({
      next: list => { this._channels.set(list); this._loading.set(false); },
      error: () => this._loading.set(false),
    });
  }


  create(body: CreateChannelRequest): void {
    this.api.create(body).subscribe(() => this.loadAll());
  }

  start(id: string): void {
    this.api.start(id).subscribe(() => this.patchStatus(id, 'Starting'));
  }

  stop(id: string): void {
    this.api.stop(id).subscribe(() => this.patchStatus(id, 'Stopped'));
  }

  restart(id: string): void {
    this.api.restart(id).subscribe(() => this.patchStatus(id, 'Starting'));
  }

  // вызывается из SignalR — обновляет статус и health вживую
  applyHealth(report: ChannelHealthReport): void {
    this._health.update(h => ({ ...h, [report.channelId]: report }));
  }

  patchStatus(id: string, status: ChannelView['status']): void {
    this._channels.update(list =>
      list.map(c => c.channelId === id ? { ...c, status } : c));
  }
}
