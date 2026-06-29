import { Component, inject, input, computed, OnInit } from '@angular/core';
import { ChannelsStore } from './channels.store';
import { PlayerComponent } from '../player/player.component';
import {DecimalPipe} from '@angular/common';
import {ChannelView} from '../../core/api/models';

@Component({
  selector: 'app-channel-detail',
  standalone: true,
  imports: [PlayerComponent, DecimalPipe],
  template: `
    @if (channel(); as ch) {
      <div class="mb-6">
        <a routerLink="/channels" class="text-sm text-slate-400 hover:text-slate-200">← к списку</a>
      </div>

      <div class="flex items-start justify-between mb-6">
        <div>
          <h1 class="text-2xl font-semibold mb-1">Канал</h1>
          <p class="text-sm font-mono text-slate-500">{{ ch.channelId }}</p>
          <p class="text-sm text-slate-400 mt-1">{{ ch.sourceUrl }}</p>
        </div>
        <span class="text-sm px-3 py-1 rounded-full font-medium" [class]="badgeClass(ch.status)">
        {{ ch.status }}
      </span>
      </div>

      <div class="flex gap-3 mb-6">
        <button (click)="store.start(ch.channelId)" [disabled]="ch.status !== 'Idle'"
                class="px-4 py-2 rounded-md bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 transition font-medium">
          Старт
        </button>
        <button (click)="store.stop(ch.channelId)"
                [disabled]="ch.status === 'Idle' || ch.status === 'Stopped'"
                class="px-4 py-2 rounded-md bg-rose-600 hover:bg-rose-500 disabled:opacity-40 transition font-medium">
          Стоп
        </button>
      </div>

      @if (report(); as h) {
        <div class="grid grid-cols-3 gap-4 mb-6">
          <div class="rounded-lg border border-slate-800 bg-slate-900/50 p-4">
            <p class="text-xs text-slate-500 mb-1">Битрейт</p>
            <p class="text-lg font-semibold">{{ h.outputBitrateKbps | number:'1.0-0' }} <span class="text-sm text-slate-400">kbps</span></p>
          </div>
          <div class="rounded-lg border border-slate-800 bg-slate-900/50 p-4">
            <p class="text-xs text-slate-500 mb-1">Дропы</p>
            <p class="text-lg font-semibold">{{ h.droppedFrames }}</p>
          </div>
          <div class="rounded-lg border border-slate-800 bg-slate-900/50 p-4">
            <p class="text-xs text-slate-500 mb-1">Аптайм</p>
            <p class="text-lg font-semibold">{{ h.uptimeSeconds | number:'1.0-0' }}<span class="text-sm text-slate-400">s</span></p>
          </div>
        </div>
      }

      @if (ch.status === 'Running') {
        <div class="rounded-xl overflow-hidden border border-slate-800 bg-black">
          <app-player [channelId]="ch.channelId" />
        </div>
      } @else {
        <div class="rounded-xl border border-dashed border-slate-700 p-10 text-center text-slate-500">
          Видео появится, когда канал перейдёт в Running
        </div>
      }
    } @else {
      <p class="text-slate-400">Загрузка…</p>
    }
  `,
})
export class ChannelDetailComponent implements OnInit {
  readonly id = input.required<string>();
  readonly store = inject(ChannelsStore);

  readonly channel = computed(() =>
    this.store.channels().find(c => c.channelId === this.id()));
  readonly report = computed(() => this.store.health()[this.id()]);

  ngOnInit(): void { this.store.loadChannel(this.id()); }

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
}
