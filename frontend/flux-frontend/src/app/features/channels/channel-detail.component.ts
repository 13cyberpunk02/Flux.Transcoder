import {
  Component, inject, input, signal, computed, OnInit,
} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {RouterLink, Router} from '@angular/router';
import {DecimalPipe} from '@angular/common';
import {FontAwesomeModule} from '@fortawesome/angular-fontawesome';
import {
  faArrowLeft, faPlay, faStop, faRotate, faTrash,
  faTowerBroadcast, faClockRotateLeft, faFloppyDisk,
  faGaugeHigh, faFilm, faClock, faVideo,
} from '@fortawesome/free-solid-svg-icons';

import {ChannelsStore} from './channels.store';
import {PlayerComponent} from '../player/player.component';
import {BitrateChartComponent} from './bitrate-chart.component';
import {StatusBadgeComponent} from '../../shared/ui/status-badge.component';
import {ButtonComponent} from '../../shared/ui/button.component';
import {ToastService} from '../../core/toast/toast.service';
import {ConfirmService} from '../../core/confirm/confirm.service';
import {ChannelsApi} from '../../core/api/channels.api';
import {SelectComponent} from '../../shared/ui/select.component';
import {InputComponent} from '../../shared/ui/input.component';

@Component({
  selector: 'app-channel-detail',
  standalone: true,
  imports: [
    FormsModule, RouterLink, DecimalPipe, FontAwesomeModule,
    PlayerComponent, BitrateChartComponent, StatusBadgeComponent, ButtonComponent, SelectComponent, InputComponent,
  ],
  template: `
    @if (channel(); as ch) {
      <!-- НАЗАД -->
      <a routerLink="/channels"
         class="inline-flex items-center gap-2 text-sm text-[rgb(var(--text-muted))]
                hover:text-[rgb(var(--text))] transition mb-4">
        <fa-icon [icon]="faArrowLeft" class="text-xs"/>
        к списку каналов
      </a>

      <!-- ШАПКА: имя + статус + управление + retention -->
      <div class="glass rounded-2xl p-5 mb-4 relative z-20">
        <div class="flex items-start justify-between gap-4 flex-wrap mb-4">
          <div class="min-w-0">
            <div class="flex items-center gap-3 mb-1.5">
              <h1 class="text-xl font-semibold text-[rgb(var(--text))]">Канал</h1>
              <ui-status-badge [status]="ch.status"/>
            </div>
            <p class="text-xs font-mono text-[rgb(var(--text-muted))] truncate">{{ ch.channelId }}</p>
            <p class="text-sm text-[rgb(var(--text-muted))] mt-1 flex items-center gap-1.5 truncate">
              <fa-icon [icon]="faVideo" class="text-xs opacity-60"/>
              {{ ch.sourceUrl }}
            </p>
          </div>

          <!-- КНОПКИ УПРАВЛЕНИЯ -->
          <div class="flex flex-wrap gap-2">
            <ui-button label="Старт" [icon]="faPlay" variant="primary" size="sm"
                       [disabled]="ch.status !== 'Idle' && ch.status !== 'Stopped' && ch.status !== 'Failed'"
                       (clicked)="onStart(ch.channelId)"/>
            <ui-button label="Рестарт" [icon]="faRotate" variant="secondary" size="sm"
                       [disabled]="ch.status === 'Idle' || ch.status === 'Stopped'"
                       (clicked)="onRestart(ch.channelId)"/>
            <ui-button label="Стоп" [icon]="faStop" variant="secondary" size="sm"
                       [disabled]="ch.status === 'Idle' || ch.status === 'Stopped'"
                       (clicked)="onStop(ch.channelId)"/>
            <ui-button label="Удалить" [icon]="faTrash" variant="danger" size="sm"
                       (clicked)="onDelete(ch.channelId)"/>
          </div>
        </div>

        <!-- RETENTION — вынесен наверх, рядом с управлением -->
        <div class="pt-4 border-t border-[rgb(var(--border)/var(--border-opacity))]">
          <label class="flex items-center gap-1.5 text-xs text-[rgb(var(--text-muted))] mb-2">
            <fa-icon [icon]="faClockRotateLeft" class="text-xs"/>
            Хранение архива
          </label>
          <div class="flex items-center gap-2 flex-wrap">
            <ui-input type="number" size="md" [(value)]="retentionValue" class="w-20"/>
            <ui-select size="md" [options]="retentionUnitOptions" [(value)]="retentionUnit" class="w-32"/>
            <ui-button label="Сохранить" [icon]="faFloppyDisk" variant="secondary" size="md"
                       [loading]="savingRetention()" (clicked)="saveRetention(ch.channelId)"/>
            <span class="text-xs text-[rgb(var(--text-muted))] ml-1">
                сейчас: {{ currentRetentionLabel() }}
            </span>
          </div>
        </div>
      </div>

      <!-- МЕТРИКИ -->
      @if (report(); as h) {
        <div class="grid grid-cols-3 gap-3 mb-4">
          <div class="glass rounded-xl px-4 py-3 flex items-center gap-3">
            <div class="flex items-center justify-center w-9 h-9 rounded-lg
                        bg-[rgb(var(--accent)/0.15)] text-[rgb(var(--accent))]">
              <fa-icon [icon]="faGaugeHigh"/>
            </div>
            <div>
              <p class="text-xs text-[rgb(var(--text-muted))]">Битрейт</p>
              <p class="text-lg font-semibold text-[rgb(var(--text))]">
                {{ h.outputBitrateKbps | number:'1.0-0' }}
                <span class="text-xs text-[rgb(var(--text-muted))]">kbps</span>
              </p>
            </div>
          </div>

          <div class="glass rounded-xl px-4 py-3 flex items-center gap-3">
            <div class="flex items-center justify-center w-9 h-9 rounded-lg
                        bg-amber-500/15 text-amber-400">
              <fa-icon [icon]="faFilm"/>
            </div>
            <div>
              <p class="text-xs text-[rgb(var(--text-muted))]">Дропы</p>
              <p class="text-lg font-semibold text-[rgb(var(--text))]">{{ h.droppedFrames }}</p>
            </div>
          </div>

          <div class="glass rounded-xl px-4 py-3 flex items-center gap-3">
            <div class="flex items-center justify-center w-9 h-9 rounded-lg
                        bg-emerald-500/15 text-emerald-400">
              <fa-icon [icon]="faClock"/>
            </div>
            <div>
              <p class="text-xs text-[rgb(var(--text-muted))]">Аптайм</p>
              <p class="text-lg font-semibold text-[rgb(var(--text))]">
                {{ h.uptimeSeconds | number:'1.0-0' }}<span class="text-xs text-[rgb(var(--text-muted))]">s</span>
              </p>
            </div>
          </div>
        </div>
      }

      <!-- ГЛАВНАЯ ЗОНА: плеер + панель просмотра -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-4">
        <div class="lg:col-span-2">
          <div class="glass rounded-2xl overflow-hidden">
            @if (ch.status === 'Running' || mode() === 'archive') {
              <app-player [src]="playerSrc()"/>
            } @else {
              <div class="aspect-video flex flex-col items-center justify-center gap-2
                          text-[rgb(var(--text-muted))]">
                <fa-icon [icon]="faTowerBroadcast" class="text-3xl opacity-40"/>
                <span class="text-sm">Канал не в эфире</span>
              </div>
            }
          </div>
        </div>

        <!-- ПАНЕЛЬ ПРОСМОТРА -->
        <div class="glass rounded-2xl p-5 flex flex-col gap-4">
          <h3 class="font-medium text-[rgb(var(--text))] flex items-center gap-2">
            <fa-icon [icon]="faTowerBroadcast" class="text-[rgb(var(--accent))] text-sm"/>
            Просмотр
          </h3>

          <ui-button label="Прямой эфир" [icon]="faTowerBroadcast"
                     [variant]="mode() === 'live' ? 'primary' : 'secondary'"
                     (clicked)="switchToLive()"/>

          <div class="border-t border-white/10 pt-4 flex flex-col gap-2">
            <label class="flex items-center gap-1.5 text-xs text-[rgb(var(--text-muted))]">
              <fa-icon [icon]="faClockRotateLeft" class="text-xs"/>
              Смотреть архив с момента
            </label>
            <ui-input type="datetime-local" [(value)]="archiveFrom"/>
            <ui-button label="Смотреть архив" [icon]="faClockRotateLeft"
                       [variant]="mode() === 'archive' ? 'primary' : 'secondary'"
                       [disabled]="!archiveFrom" (clicked)="switchToArchive()"/>
          </div>
        </div>
      </div>

      <!-- ГРАФИК -->
      <app-bitrate-chart [channelId]="ch.channelId"/>

    } @else {
      <div class="glass rounded-2xl p-10 text-center text-[rgb(var(--text-muted))]">
        Загрузка…
      </div>
    }
  `,
})
export class ChannelDetailComponent implements OnInit {
  readonly id = input.required<string>();

  readonly store = inject(ChannelsStore);
  private readonly api = inject(ChannelsApi);
  private readonly toast = inject(ToastService);
  private readonly confirmService = inject(ConfirmService);
  private readonly router = inject(Router);

  // иконки
  readonly faArrowLeft = faArrowLeft;
  readonly faPlay = faPlay;
  readonly faStop = faStop;
  readonly faRotate = faRotate;
  readonly faTrash = faTrash;
  readonly faTowerBroadcast = faTowerBroadcast;
  readonly faClockRotateLeft = faClockRotateLeft;
  readonly faFloppyDisk = faFloppyDisk;
  readonly faGaugeHigh = faGaugeHigh;
  readonly faFilm = faFilm;
  readonly faClock = faClock;
  readonly faVideo = faVideo;

  retentionUnit = signal<'minutes' | 'hours' | 'days'>('days');
  readonly retentionUnitOptions = [
    {value: 'minutes', label: 'минут'},
    {value: 'hours', label: 'часов'},
    {value: 'days', label: 'дней'},
  ];

  // просмотр
  readonly mode = signal<'live' | 'archive'>('live');
  archiveFrom = signal('');

  // retention-форма
  retentionValue = signal('7');
  readonly savingRetention = signal(false);

  private readonly hlsBase = 'http://localhost:3088';
  private readonly apiBase = 'http://localhost:5000';

  readonly channel = computed(() =>
    this.store.channels().find(c => c.channelId === this.id()));
  readonly report = computed(() => this.store.health()[this.id()]);

  readonly playerSrc = computed(() => {
    const id = this.id();
    if (this.mode() === 'live') {
      return `${this.hlsBase}/${id}/live/live.m3u8`;
    }
    const from = this.toLocalIso(this.archiveFrom());
    return `${this.apiBase}/api/channels/${id}/archive?from=${encodeURIComponent(from)}`;
  });

  readonly currentRetentionLabel = computed(() => {
    const ch = this.channel();
    if (!ch) return '';
    const m = ch.retentionMinutes;
    if (m % (24 * 60) === 0) return `${m / (24 * 60)} дн.`;
    if (m % 60 === 0) return `${m / 60} ч.`;
    return `${m} мин.`;
  });

  ngOnInit(): void {
    this.store.loadChannel(this.id());
  }

  onStart(id: string): void {
    this.store.start(id);
  }

  onStop(id: string): void {
    this.store.stop(id);
  }

  onRestart(id: string): void {
    this.store.restart(id);
  }

  async onDelete(id: string): Promise<void> {
    const ok = await this.confirmService.ask({
      title: 'Удалить канал?',
      message: 'Действие необратимо. Канал и его настройки будут удалены. ' +
        'Записанный архив останется на диске.',
      confirmLabel: 'Удалить',
      danger: true,
      icon: faTrash,
    });
    if (!ok) return;

    this.api.delete(id).subscribe({
      next: () => {
        this.toast.success('Канал удалён');
        this.router.navigate(['/channels']);
      },
      error: () => this.toast.error('Не удалось удалить канал'),
    });
  }

  switchToLive(): void {
    this.mode.set('live');
    this.toast.info('Прямой эфир');
  }

  switchToArchive(): void {
    if (!this.archiveFrom) return;
    this.mode.set('archive');
    this.toast.info('Переключено на архив');
  }

  saveRetention(id: string): void {
    const minutes = this.retentionToMinutes();
    if (minutes < 1) {
      this.toast.error('Минимум 1 минута');
      return;
    }
    if (minutes > 14 * 24 * 60) {
      this.toast.error('Максимум 14 дней');
      return;
    }

    this.savingRetention.set(true);
    this.api.updateRetention(id, minutes).subscribe({
      next: () => {
        this.store.loadChannel(id);
        this.toast.success('Настройки хранения сохранены');
        this.savingRetention.set(false);
      },
      error: () => {
        this.toast.error('Не удалось сохранить настройки');
        this.savingRetention.set(false);
      },
    });
  }

  private retentionToMinutes(): number {
    const val = parseInt(this.retentionValue(), 10) || 0;
    switch (this.retentionUnit()) {
      case 'days':
        return val * 24 * 60;
      case 'hours':
        return val * 60;
      default:
        return val;
    }
  }

  private toLocalIso(dateTimeLocal: string): string {
    const d = new Date(dateTimeLocal);
    const pad = (n: number) => String(n).padStart(2, '0');
    const offsetMin = -d.getTimezoneOffset();
    const sign = offsetMin >= 0 ? '+' : '-';
    const oh = pad(Math.floor(Math.abs(offsetMin) / 60));
    const om = pad(Math.abs(offsetMin) % 60);
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}` +
      `T${pad(d.getHours())}:${pad(d.getMinutes())}:00${sign}${oh}:${om}`;
  }
}
