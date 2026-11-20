import { Injectable, inject, signal, computed } from '@angular/core';
import { ChannelsApi } from '../../core/api/channels.api';
import {ChannelView, ChannelHealthReport, CreateChannelRequest} from '../../core/api/models';
import {ToastService} from '../../core/toast/toast.service';

@Injectable({ providedIn: 'root' })
export class ChannelsStore {
  private readonly api = inject(ChannelsApi);
  private readonly toast = inject(ToastService);

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
    this.api.create(body).subscribe({
      next: () => {
        this.loadAll();
        this.toast.success('Канал создан', 'Готово');
      },
      error: () => this.toast.error('Не удалось создать канал'),
    });
  }

  start(id: string): void {
    this.api.start(id).subscribe({
      next: () => { this.patchStatus(id, 'Starting'); this.toast.info('Канал запускается'); },
      error: err => this.toast.error(err?.error?.detail ?? 'Не удалось запустить канал'),
    });
  }

  stop(id: string): void {
    this.api.stop(id).subscribe({
      next: () => { this.patchStatus(id, 'Stopped'); this.toast.info('Канал остановлен'); },
      error: err => this.toast.error(err?.error?.detail ?? 'Не удалось остановить канал'),
    });
  }

  restart(id: string): void {
    this.api.restart(id).subscribe({
      next: () => {
        this.patchStatus(id, 'Starting');
        this.toast.info('Канал перезапущен');
      },
      error: err => this.toast.error(err?.error?.detail ?? 'Не удалось перезапустить канал'),
    });
  }

  delete(id: string): void {
    this.api.delete(id).subscribe({
      next: () => {
        this._channels.update(list => list.filter(c => c.channelId !== id));
        this.toast.success('Канал удалён');
      },
      error: err => this.toast.error(err?.error?.detail ?? 'Не удалось удалить канал'),
    });
  }

  updateRetention(id: string, minutes: number): void {
    this.api.updateRetention(id, minutes).subscribe({
      next: () => {
        this.loadChannel(id);
        this.toast.info('Время хранения архива изменено успешно');
      },
      error: () => this.toast.error('Не удалось изменить время хранения для канала')
    });
  }

  applyHealth(report: ChannelHealthReport): void {
    this._health.update(h => ({ ...h, [report.channelId]: report }));
  }

  patchStatus(id: string, status: ChannelView['status']): void {
    this._channels.update(list =>
      list.map(c => c.channelId === id ? { ...c, status } : c));
  }
}
