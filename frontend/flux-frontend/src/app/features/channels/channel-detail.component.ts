import {Component, inject, input, computed, OnInit, signal} from '@angular/core';
import { ChannelsStore } from './channels.store';
import { PlayerComponent } from '../player/player.component';
import {DecimalPipe} from '@angular/common';
import {ChannelView} from '../../core/api/models';
import {RetentionSettingsComponent} from './retention-settings.component';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {BitrateChartComponent} from './bitrate-chart.component';

@Component({
  selector: 'app-channel-detail',
  standalone: true,
  imports: [
    PlayerComponent,
    DecimalPipe,
    RetentionSettingsComponent,
    BitrateChartComponent,
    FormsModule,
    RouterLink],
  template: `
    @if (channel(); as ch) {
      <div class="mb-6">
        <a routerLink="/channels"
           class="text-sm text-slate-400 hover:text-slate-200 transition inline-flex items-center gap-1 mb-3">
          ← к списку
        </a>

        <div class="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <h1 class="text-2xl font-semibold mb-1">Канал</h1>
            <p class="text-sm font-mono text-slate-500">{{ ch.channelId }}</p>
            <p class="text-sm text-slate-400 mt-1 break-all">{{ ch.sourceUrl }}</p>
          </div>

          <div class="flex items-center gap-3">
          <span class="text-sm px-3 py-1.5 rounded-full font-medium" [class]="badgeClass(ch.status)">
            {{ ch.status }}
          </span>
            <div class="flex gap-2">
              <button (click)="store.start(ch.channelId)" [disabled]="ch.status !== 'Idle'"
                      class="px-3 py-1.5 rounded-lg text-sm bg-emerald-500/20 text-emerald-300 border border-emerald-500/30
                     hover:bg-emerald-500/30 disabled:opacity-40 transition">Старт</button>
              <button (click)="store.restart(ch.channelId)"
                      [disabled]="ch.status === 'Idle' || ch.status === 'Stopped'"
                      class="px-3 py-1.5 rounded-lg text-sm bg-amber-500/20 text-amber-300 border border-amber-500/30
                     hover:bg-amber-500/30 disabled:opacity-40 transition">Рестарт</button>
              <button (click)="store.stop(ch.channelId)"
                      [disabled]="ch.status === 'Idle' || ch.status === 'Stopped'"
                      class="px-3 py-1.5 rounded-lg text-sm bg-rose-500/20 text-rose-300 border border-rose-500/30
                     hover:bg-rose-500/30 disabled:opacity-40 transition">Стоп</button>
            </div>
          </div>
        </div>
      </div>

      @if (report(); as h) {
        <div class="grid grid-cols-3 gap-3 mb-6">
          <div class="glass rounded-xl px-4 py-3">
            <p class="text-xs text-slate-400 mb-0.5">Битрейт</p>
            <p class="text-lg font-semibold">{{ h.outputBitrateKbps | number:'1.0-0' }}
              <span class="text-xs text-slate-500">kbps</span></p>
          </div>
          <div class="glass rounded-xl px-4 py-3">
            <p class="text-xs text-slate-400 mb-0.5">Дропы</p>
            <p class="text-lg font-semibold">{{ h.droppedFrames }}</p>
          </div>
          <div class="glass rounded-xl px-4 py-3">
            <p class="text-xs text-slate-400 mb-0.5">Аптайм</p>
            <p class="text-lg font-semibold">{{ h.uptimeSeconds | number:'1.0-0' }}<span class="text-xs text-slate-500">s</span></p>
          </div>
        </div>
      }

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-4">
        <!-- Плеер (2/3 ширины) -->
        <div class="lg:col-span-2">
          <div class="glass rounded-2xl overflow-hidden">
            @if (ch.status === 'Running' || mode() === 'archive') {
              <app-player [src]="playerSrc()" />
            } @else {
              <div class="aspect-video flex items-center justify-center text-slate-500">
                Канал не в эфире
              </div>
            }
          </div>
        </div>

        <div class="glass rounded-2xl p-5 flex flex-col gap-4">
          <h3 class="font-medium">Просмотр</h3>

          <button (click)="switchToLive()"
                  [class]="mode() === 'live'
            ? 'w-full px-4 py-2.5 rounded-xl bg-indigo-500/30 text-indigo-200 border border-indigo-400/40 font-medium transition'
            : 'w-full px-4 py-2.5 rounded-xl bg-white/5 text-slate-300 border border-white/10 hover:bg-white/10 transition'">
            ● Прямой эфир
          </button>

          <div class="border-t border-white/10 pt-4">
            <p class="text-xs text-slate-400 mb-2">Смотреть архив с момента:</p>
            <input type="datetime-local" [(ngModel)]="archiveFrom"
                   class="w-full mb-2 px-3 py-2 rounded-lg bg-white/5 border border-white/10 outline-none
                   focus:border-indigo-400/40 transition text-sm" />
            <button (click)="switchToArchive()" [disabled]="!archiveFrom"
                    [class]="mode() === 'archive'
              ? 'w-full px-4 py-2 rounded-xl bg-indigo-500/30 text-indigo-200 border border-indigo-400/40 font-medium transition'
              : 'w-full px-4 py-2 rounded-xl bg-white/5 text-slate-300 border border-white/10 hover:bg-white/10 disabled:opacity-40 transition'">
              Смотреть архив
            </button>
          </div>
        </div>
      </div>

      <div class="mb-4">
        <app-bitrate-chart [channelId]="ch.channelId" />
      </div>

      <app-retention-settings
        [channelId]="ch.channelId"
        [currentMinutes]="ch.retentionMinutes" />

    } @else {
      <div class="glass rounded-2xl p-10 text-center text-slate-400">Загрузка…</div>
    }
  `,
})
export class ChannelDetailComponent implements OnInit {
  readonly id = input.required<string>();
  readonly store = inject(ChannelsStore);
  readonly mode = signal<'live' | 'archive'>('live');
  archiveFrom = '';
  private readonly hlsBase = 'http://localhost:3088';
  private readonly apiBase = 'http://localhost:5000';

  readonly playerSrc = computed(() => {
    const id = this.id();
    if (this.mode() === 'live') {
      return `${this.hlsBase}/${id}/live/live.m3u8`;
    } else {
      const from = this.toLocalIso(this.archiveFrom);
      return `${this.apiBase}/api/channels/${id}/archive?from=${encodeURIComponent(from)}`;
    }
  });

  readonly channel = computed(() =>
    this.store.channels().find(c => c.channelId === this.id()));
  readonly report = computed(() => this.store.health()[this.id()]);

  ngOnInit(): void { this.store.loadAll(); }

  badgeClass(status: ChannelView['status']): string {
    switch (status) {
      case 'Running':  return 'bg-emerald-500/15 text-emerald-400';
      case 'Starting': return 'bg-amber-500/15 text-amber-400';
      case 'Degraded': return 'bg-orange-500/15 text-orange-400';
      case 'Failed':   return 'bg-rose-500/15 text-rose-400';
      case 'Stopped':  return 'bg-slate-500/15 text-slate-400';
      default:         return 'bg-slate-700/40 text-slate-300'; // Idle
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

  switchToLive(): void {
    this.mode.set('live');
  }

  switchToArchive(): void {
    if (!this.archiveFrom) return;
    this.mode.set('archive');
  }
}
