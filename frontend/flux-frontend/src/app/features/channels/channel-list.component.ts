import {Component, inject, signal, OnInit} from '@angular/core';
import {RouterLink} from '@angular/router';
import {ChannelsStore} from './channels.store';
import {ChannelView} from '../../core/api/models';
import {CreateChannelComponent} from './create-channel.component';

@Component({
  selector: 'app-channel-list',
  standalone: true,
  imports: [RouterLink, CreateChannelComponent],
  template: `
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-semibold">Каналы</h1>
      <button (click)="showForm.set(!showForm())"
              class="px-4 py-2 rounded-md bg-indigo-600 hover:bg-indigo-500 transition font-medium">
        {{ showForm() ? 'Отмена' : '+ Новый канал' }}
      </button>
    </div>

    @if (showForm()) {
      <div class="mb-8">
        <app-create-channel (created)="onCreated()"/>
      </div>
    }

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
             class="glass rounded-2xl p-5 transition hover:bg-white/8 hover:border-white/20
           hover:-translate-y-0.5 duration-200">
            <div class="flex items-center justify-between mb-3">
            <span class="text-xs font-mono text-slate-400 truncate max-w-[60%]">
                {{ ch.channelId.slice(0, 8) }}
            </span>
              <span class="text-xs px-2.5 py-1 rounded-full font-medium backdrop-blur-sm"
                    [class]="badgeClass(ch.status)">
        {{ ch.status }}
      </span>
            </div>
            <p class="text-sm text-slate-200 truncate">{{ ch.sourceUrl }}</p>
          </a>
        }
      </div>
    }
  `,
})
export class ChannelListComponent implements OnInit {
  readonly store = inject(ChannelsStore);
  readonly showForm = signal(false);

  ngOnInit(): void {
    this.store.loadAll();
  }

  onCreated(): void {
    this.showForm.set(false);   // закрыть форму после создания
  }

  badgeClass(status: ChannelView['status']): string {
    switch (status) {
      case 'Running':  return 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30';
      case 'Starting': return 'bg-amber-500/20 text-amber-300 border border-amber-500/30';
      case 'Degraded': return 'bg-orange-500/20 text-orange-300 border border-orange-500/30';
      case 'Failed':   return 'bg-rose-500/20 text-rose-300 border border-rose-500/30';
      case 'Stopped':  return 'bg-slate-500/20 text-slate-300 border border-slate-500/30';
      default:         return 'bg-white/10 text-slate-300 border border-white/10';
    }
  }
}
