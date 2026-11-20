import { Injectable, inject, signal, computed } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ChannelsApi } from '../../core/api/channels.api';
import {
  ChannelView, ChannelHealthReport, ChannelStatus, CreateChannelRequest,
} from '../../core/api/models';
import { Notifier } from '../../core/notify/notifier';

@Injectable({ providedIn: 'root' })
export class ChannelsStore {
  private readonly api = inject(ChannelsApi);
  private readonly notify = inject(Notifier);

  private readonly _channels = signal<ChannelView[]>([]);
  private readonly _health = signal<Record<string, ChannelHealthReport>>({});
  private readonly _loading = signal(false);
  private readonly _loaded = signal(false);

  readonly channels = this._channels.asReadonly();
  readonly health = this._health.asReadonly();
  readonly loading = this._loading.asReadonly();
  /** Хотя бы одна загрузка завершилась — отличаем «пусто» от «ещё грузится». */
  readonly loaded = this._loaded.asReadonly();

  readonly runningCount = computed(() =>
    this._channels().filter(c => c.status === 'Running').length);

  readonly problemCount = computed(() =>
    this._channels().filter(c => c.status === 'Failed' || c.status === 'Degraded').length);

  loadAll(): void {
    this._loading.set(true);
    this.api.getAll().subscribe({
      next: list => {
        this._channels.set(list);
        this._loading.set(false);
        this._loaded.set(true);
      },
      error: err => {
        this._loading.set(false);
        this.notify.error('Не удалось загрузить каналы', err);
      },
    });
  }

  loadChannel(id: string): void {
    this.api.getById(id).subscribe({
      next: ch => this._channels.update(list =>
        list.some(c => c.channelId === ch.channelId)
          ? list.map(c => (c.channelId === ch.channelId ? ch : c))
          : [...list, ch]),
      error: err => this.notify.error('Не удалось загрузить канал', err),
    });
  }

  /** Возвращает Promise — диалог создания закрывается только после успеха. */
  async create(body: CreateChannelRequest): Promise<boolean> {
    try {
      await firstValueFrom(this.api.create(body));
      this.loadAll();
      this.notify.success('Канал создан');
      return true;
    } catch (err) {
      this.notify.error('Не удалось создать канал', err);
      return false;
    }
  }

  start(id: string): void {
    this.api.start(id).subscribe({
      next: () => { this.patchStatus(id, 'Starting'); this.notify.info('Канал запускается'); },
      error: err => this.notify.error('Не удалось запустить канал', err),
    });
  }

  stop(id: string): void {
    this.api.stop(id).subscribe({
      next: () => { this.patchStatus(id, 'Stopped'); this.notify.info('Канал остановлен'); },
      error: err => this.notify.error('Не удалось остановить канал', err),
    });
  }

  restart(id: string): void {
    this.api.restart(id).subscribe({
      next: () => { this.patchStatus(id, 'Starting'); this.notify.info('Канал перезапускается'); },
      error: err => this.notify.error('Не удалось перезапустить канал', err),
    });
  }

  async delete(id: string): Promise<boolean> {
    try {
      await firstValueFrom(this.api.delete(id));
      this._channels.update(list => list.filter(c => c.channelId !== id));
      this.notify.success('Канал удалён');
      return true;
    } catch (err) {
      this.notify.error('Не удалось удалить канал', err);
      return false;
    }
  }

  async updateRetention(id: string, minutes: number): Promise<boolean> {
    try {
      await firstValueFrom(this.api.updateRetention(id, minutes));
      this._channels.update(list =>
        list.map(c => (c.channelId === id ? { ...c, retentionMinutes: minutes } : c)));
      this.notify.success('Время хранения архива изменено');
      return true;
    } catch (err) {
      this.notify.error('Не удалось изменить время хранения', err);
      return false;
    }
  }

  applyHealth(report: ChannelHealthReport): void {
    this._health.update(h => ({ ...h, [report.channelId]: report }));
  }

  patchStatus(id: string, status: ChannelStatus): void {
    this._channels.update(list =>
      list.map(c => (c.channelId === id ? { ...c, status } : c)));
  }
}
