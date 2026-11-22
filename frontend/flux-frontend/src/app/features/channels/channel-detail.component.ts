import { Component, computed, DestroyRef, inject, input, linkedSignal, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatDatepicker, MatDatepickerInput, MatDatepickerToggle } from '@angular/material/datepicker';
import { MatTimepicker, MatTimepickerInput, MatTimepickerToggle } from '@angular/material/timepicker';

import { ChannelsStore } from './channels.store';
import { ChannelActions } from './channel-actions';
import { ProfilesStore } from '../profiles/profiles.store';
import { PlayerComponent } from '../player/player.component';
import { BitrateChartComponent } from './bitrate-chart.component';
import { RetentionSettingsComponent } from './retention-settings.component';
import { StatusTagComponent } from '../../shared/status-tag.component';
import { BitratePipe } from '../../core/format/format.pipes';
import { combineDateAndTime, formatBitrate, formatDuration, toLocalIso } from '../../core/format/format';
import { archivePlaylistUrl, livePlaylistUrl } from '../../core/config/endpoints';
import { canRestart, canStop, isInactive } from '../../core/api/models';
import { codecShort } from '../../core/api/profiles.api';
import { Notifier } from '../../core/notify/notifier';

type Mode = 'live' | 'archive';

const ARCHIVE_PRESETS = [
  { minutes: 5, label: '−5 мин' },
  { minutes: 15, label: '−15 мин' },
  { minutes: 60, label: '−1 ч' },
  { minutes: 360, label: '−6 ч' },
  { minutes: 1440, label: '−24 ч' },
];

@Component({
  selector: 'app-channel-detail',
  imports: [
    RouterLink, MatButton, MatIconButton, MatIcon, MatTooltip, MatButtonToggle, MatButtonToggleGroup,
    MatFormField, MatLabel, MatSuffix, MatInput, MatDatepicker, MatDatepickerInput, MatDatepickerToggle,
    MatTimepicker, MatTimepickerInput, MatTimepickerToggle,
    PlayerComponent, BitrateChartComponent, RetentionSettingsComponent, StatusTagComponent, BitratePipe,
  ],
  template: `
    <a matButton routerLink="/channels" class="!-ml-3 !mb-2">
      <mat-icon>arrow_back</mat-icon> Каналы
    </a>

    @if (channel(); as ch) {
      <!-- ШАПКА -->
      <div class="flex items-start justify-between gap-4 flex-wrap mb-5">
        <div class="min-w-0">
          <div class="flex items-center gap-3 flex-wrap">
            <h1 class="text-xl font-semibold truncate">{{ ch.sourceUrl }}</h1>
            <app-status-tag [status]="ch.status" />
          </div>
          <button type="button" (click)="copyId(ch.channelId)" matTooltip="Скопировать ID"
                  class="mt-1 flex items-center gap-1.5 text-xs font-mono text-muted hover:text-on-surface">
            {{ ch.channelId }}
            <span class="material-symbols-outlined !text-sm" aria-hidden="true">content_copy</span>
          </button>
        </div>

        <div class="flex items-center gap-2 flex-wrap">
          @if (isInactive(ch.status)) {
            <button matButton="filled" (click)="actions.start(ch.channelId)">
              <mat-icon>play_arrow</mat-icon> Запустить
            </button>
          }
          @if (canRestart(ch.status)) {
            <button matButton="outlined" (click)="actions.restart(ch.channelId)">
              <mat-icon>restart_alt</mat-icon> Перезапустить
            </button>
          }
          @if (canStop(ch.status)) {
            <button matButton="outlined" class="danger-button" (click)="actions.stop(ch.channelId, ch.sourceUrl)">
              <mat-icon>stop</mat-icon> Остановить
            </button>
          }
          <span class="w-px h-6 bg-outline-variant mx-1" aria-hidden="true"></span>
          <span [matTooltip]="isInactive(ch.status) ? 'Удалить канал' : 'Сначала остановите канал'">
            <button matIconButton class="danger-button" aria-label="Удалить канал"
                    [disabled]="!isInactive(ch.status)" (click)="remove(ch.channelId, ch.sourceUrl)">
              <mat-icon>delete</mat-icon>
            </button>
          </span>
        </div>
      </div>

      <!-- МЕТРИКИ -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-5">
        @for (m of metrics(); track m.label) {
          <div class="panel px-4 py-3">
            <p class="text-xs text-muted flex items-center gap-1.5">
              <span class="material-symbols-outlined !text-base" aria-hidden="true">{{ m.icon }}</span>{{ m.label }}
            </p>
            <p class="text-lg font-semibold num mt-0.5 truncate" [class]="m.tone">{{ m.value }}</p>
          </div>
        }
      </div>

      <div class="grid grid-cols-1 xl:grid-cols-3 gap-5 mb-5">
        <!-- ПЛЕЕР -->
        <section class="xl:col-span-2 panel overflow-hidden">
          <div class="flex items-center justify-between gap-3 flex-wrap px-4 py-3 border-b border-outline-variant">
            <mat-button-toggle-group [value]="mode()" (change)="setMode($event.value)"
                                     hideSingleSelectionIndicator aria-label="Режим просмотра">
              <mat-button-toggle value="live"><mat-icon>sensors</mat-icon> Прямой эфир</mat-button-toggle>
              <mat-button-toggle value="archive"><mat-icon>history</mat-icon> Архив</mat-button-toggle>
            </mat-button-toggle-group>
            @if (mode() === 'archive' && appliedFrom()) {
              <span class="text-sm text-muted">с <b class="text-on-surface num">{{ appliedFromLabel() }}</b></span>
            }
          </div>

          @if (mode() === 'archive') {
            <div class="flex items-center gap-2 flex-wrap px-4 py-3 border-b border-outline-variant">
              @for (p of archivePresets; track p.minutes) {
                <button matButton="outlined" (click)="watchArchiveAgo(p.minutes)">{{ p.label }}</button>
              }
              <span class="text-muted text-sm mx-1">или</span>
              <mat-form-field class="w-40">
                <mat-label>Дата</mat-label>
                <input matInput [matDatepicker]="datePicker" [value]="archiveDate()"
                       (dateInput)="pickedDate.set($event.value)" (dateChange)="pickedDate.set($event.value)"
                       [min]="archiveMinDate()" [max]="now()" />
                <mat-datepicker-toggle matSuffix [for]="datePicker" />
                <mat-datepicker #datePicker />
              </mat-form-field>
              <mat-form-field class="w-32">
                <mat-label>Время</mat-label>
                <input matInput [matTimepicker]="timePicker" [value]="archiveTime()" (valueChange)="pickedTime.set($event)" />
                <mat-timepicker-toggle matSuffix [for]="timePicker" />
                <mat-timepicker #timePicker interval="5min" />
              </mat-form-field>
              <button matButton="filled" [disabled]="!!momentError()" (click)="watchArchive()">
                <mat-icon>play_arrow</mat-icon> Смотреть
              </button>
              @if (momentError(); as error) {
                <p role="alert" class="w-full text-xs text-error">{{ error }}</p>
              }
            </div>
          }

          @if (playerSrc(); as src) {
            <app-player [src]="src" [live]="mode() === 'live'" (failed)="onPlayerFailed()" />
          } @else {
            <div class="aspect-video flex flex-col items-center justify-center gap-3 text-muted px-6 text-center">
              <span class="material-symbols-outlined !text-5xl opacity-50" aria-hidden="true">videocam_off</span>
              @if (mode() === 'live') {
                <p>Канал не в эфире</p>
                <button matButton="tonal" (click)="watchArchiveAgo(60)">
                  <mat-icon>history</mat-icon> Смотреть архив
                </button>
              } @else {
                <p>Выберите момент, с которого смотреть архив</p>
              }
            </div>
          }
        </section>

        <!-- БОКОВАЯ ПАНЕЛЬ -->
        <aside class="flex flex-col gap-5">
          <section class="panel p-4">
            <h2 class="font-semibold mb-3 flex items-center gap-2">
              <span class="material-symbols-outlined text-muted" aria-hidden="true">database</span>Хранение архива
            </h2>
            <app-retention-settings [channelId]="ch.channelId" [currentMinutes]="ch.retentionMinutes" />
          </section>

          <section class="panel p-4">
            <h2 class="font-semibold mb-3 flex items-center gap-2">
              <span class="material-symbols-outlined text-muted" aria-hidden="true">tune</span>Профиль кодирования
            </h2>
            @if (profile(); as p) {
              <p class="font-medium mb-2">{{ p.name }}</p>
              <ul class="flex flex-col gap-1.5">
                @for (r of p.renditions; track $index) {
                  <li class="flex items-center justify-between text-sm px-3 py-1.5 rounded-md bg-surface-container">
                    <span class="num">{{ r.width }}×{{ r.height }}</span>
                    <span class="text-muted num">{{ r.bitrateKbps | bitrate }} · {{ codecShort(r.codec) }}</span>
                  </li>
                }
              </ul>
              @if (p.renditions.length > 1) {
                <p class="text-xs text-muted mt-2">В эфир идёт старший рендишен.</p>
              }
            } @else {
              <div class="h-16 rounded-md bg-surface-high animate-pulse"></div>
            }
          </section>
        </aside>
      </div>

      <!-- ГРАФИК -->
      <section class="panel p-4">
        <app-bitrate-chart [channelId]="ch.channelId" (pointSelected)="watchArchiveFrom($event)" />
      </section>
    } @else if (notFound()) {
      <div role="alert" class="flex items-center gap-2 rounded-lg px-4 py-3 bg-amber-500/15 text-amber-800 dark:text-amber-300">
        <span class="material-symbols-outlined" aria-hidden="true">info</span>
        Канал не найден — возможно, он был удалён.
      </div>
    } @else {
      <div class="flex flex-col gap-4" aria-busy="true">
        <div class="h-8 w-96 max-w-full rounded-md bg-surface-high animate-pulse"></div>
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-3">
          @for (i of [1, 2, 3, 4]; track i) { <div class="h-18 rounded-xl bg-surface-high animate-pulse"></div> }
        </div>
        <div class="h-96 rounded-xl bg-surface-high animate-pulse"></div>
      </div>
    }
  `,
})
export class ChannelDetailComponent implements OnInit {
  readonly id = input.required<string>();

  private readonly store = inject(ChannelsStore);
  private readonly profiles = inject(ProfilesStore);
  private readonly router = inject(Router);
  private readonly notify = inject(Notifier);
  protected readonly actions = inject(ChannelActions);

  protected readonly isInactive = isInactive;
  protected readonly canRestart = canRestart;
  protected readonly canStop = canStop;
  protected readonly codecShort = codecShort;
  protected readonly archivePresets = ARCHIVE_PRESETS;

  protected readonly channel = computed(() =>
    this.store.channels().find(c => c.channelId === this.id()));
  protected readonly notFound = computed(() => this.store.loaded() && !this.channel());
  private readonly report = computed(() => this.store.health()[this.id()]);
  protected readonly profile = computed(() => {
    const ch = this.channel();
    return ch ? this.profiles.byId().get(ch.profileId) : undefined;
  });

  protected readonly mode = signal<Mode>('live');

  /** Выбор момента архива: дата и время отдельно (mat-datepicker + mat-timepicker). */
  protected readonly archiveDate = signal(new Date(Date.now() - 60 * 60_000));
  protected readonly archiveTime = signal<Date | null>(new Date(Date.now() - 60 * 60_000));
  /**
   * Ввод держим отдельно от [value] полей: обратная запись в [value] переформатировала бы
   * текст прямо во время набора. null — введено что-то некорректное.
   */
  protected readonly pickedDate = linkedSignal<Date | null>(() => this.archiveDate());
  protected readonly pickedTime = linkedSignal<Date | null>(() => this.archiveTime());
  private readonly pickedMoment = computed(() => combineDateAndTime(this.pickedDate(), this.pickedTime()));
  protected readonly momentError = computed(() => {
    const moment = this.pickedMoment();
    if (!moment) return 'Введите дату как дд.мм.гггг и время как чч:мм';
    if (moment > this.now()) return 'Этот момент ещё не наступил';
    const min = this.archiveMinDate();
    if (min && moment < min) return 'Этот момент старше срока хранения архива';
    return undefined;
  });

  /** Момент, с которого реально играет архив (после «Смотреть» / пресета / клика по графику). */
  protected readonly appliedFrom = signal<Date | undefined>(undefined);

  protected readonly now = signal(new Date());
  protected readonly archiveMinDate = computed(() => {
    const ch = this.channel();
    return ch ? new Date(this.now().getTime() - ch.retentionMinutes * 60_000) : null;
  });

  protected readonly appliedFromLabel = computed(() =>
    this.appliedFrom()?.toLocaleString('ru-RU', {
      day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit',
    }) ?? '');

  protected readonly playerSrc = computed(() => {
    const ch = this.channel();
    if (!ch) return undefined;
    if (this.mode() === 'live') {
      return isInactive(ch.status) ? undefined : livePlaylistUrl(ch.channelId);
    }
    const from = this.appliedFrom();
    return from ? archivePlaylistUrl(ch.channelId, toLocalIso(from)) : undefined;
  });

  protected readonly metrics = computed(() => {
    const ch = this.channel();
    const live = ch && !isInactive(ch.status) ? this.report() : undefined;
    return [
      { label: 'Битрейт', icon: 'speed', value: formatBitrate(live?.outputBitrateKbps), tone: '' },
      { label: 'Аптайм', icon: 'schedule', value: formatDuration(live?.uptimeSeconds), tone: '' },
      {
        label: 'Потерянные кадры', icon: 'broken_image',
        value: live ? String(live.droppedFrames) : '—',
        tone: (live?.droppedFrames ?? 0) > 0 ? 'text-amber-600' : '',
      },
      {
        label: 'Сегменты', icon: 'monitor_heart',
        value: !live ? '—' : live.status === 'Healthy' ? 'Пишутся' : live.status === 'Stalled' ? 'Не пишутся' : 'Ожидание',
        tone: live?.status === 'Stalled' ? 'text-error' : live?.status === 'Healthy' ? 'text-green-600 dark:text-green-400' : '',
      },
    ];
  });

  constructor() {
    // «сейчас» — граница допустимого момента; на открытой странице она должна двигаться
    const clock = setInterval(() => this.now.set(new Date()), 15_000);
    inject(DestroyRef).onDestroy(() => clearInterval(clock));
  }

  ngOnInit(): void {
    this.store.loadChannel(this.id());
    if (!this.profiles.loaded()) this.profiles.loadAll();
  }

  protected setMode(mode: Mode): void {
    this.mode.set(mode);
    this.now.set(new Date());
  }

  protected watchArchive(): void {
    const moment = this.pickedMoment();
    if (moment && !this.momentError()) this.watchArchiveFrom(moment);
  }

  protected watchArchiveAgo(minutes: number): void {
    this.watchArchiveFrom(new Date(Date.now() - minutes * 60_000));
  }

  protected watchArchiveFrom(from: Date): void {
    this.now.set(new Date());
    this.mode.set('archive');
    this.archiveDate.set(from);
    this.archiveTime.set(from);
    this.appliedFrom.set(from);
  }

  protected onPlayerFailed(): void {
    this.notify.error(this.mode() === 'archive'
      ? 'Нет записи архива с выбранного момента'
      : 'Не удалось загрузить прямой эфир');
  }

  protected async copyId(id: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(id);
      this.notify.info('ID скопирован');
    } catch {
      this.notify.error('Не удалось скопировать ID');
    }
  }

  protected async remove(id: string, source: string): Promise<void> {
    if (await this.actions.delete(id, source)) {
      this.router.navigate(['/channels']);
    }
  }
}
