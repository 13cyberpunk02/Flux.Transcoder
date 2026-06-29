import { Component, inject, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChannelsStore } from './channels.store';
import { ChannelView } from '../../core/api/models';

@Component({
  selector: 'app-channel-list',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-semibold">Каналы</h1>
    </div>

    @if (store.loading()) {
      <p class="text-slate-400">Загрузка…</p>
    } @else if (store.channels().length === 0) {
      <div class="rounded-xl border border-dashed border-slate-700 p-10 text-center text-slate-400">
        Пока нет каналов
      </div>
    } @else {
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        @for (ch of store.channels(); track ch.channelId) {
          <a [routerLink]="['/channels', ch.channelId]"
            class="group rounded-xl border border-slate-800 bg-slate-900/50 p-5
                   hover:border-indigo-500/60 hover:bg-slate-900 transition">
            <div class="flex items-center justify-between mb-3">
              <span class="text-xs font-mono text-slate-500 truncate max-w-[60%]">
                {{ ch.channelId.slice(0, 8) }}
              </span>
              <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                    [class]="badgeClass(ch.status)">
                {{ ch.status }}
              </span>
            </div>
            <p class="text-sm text-slate-300 truncate">{{ ch.sourceUrl }}</p>
          </a>
        }
      </div>
    }
  `,
})
export class ChannelListComponent {
  readonly store = inject(ChannelsStore);

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
