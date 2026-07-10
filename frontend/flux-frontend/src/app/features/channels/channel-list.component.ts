import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faPlus, faVideo, faLayerGroup, faTowerBroadcast } from '@fortawesome/free-solid-svg-icons';

import { ChannelsStore } from './channels.store';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { CreateChannelComponent } from './create-channel.component';

@Component({
  selector: 'app-channel-list',
  standalone: true,
  imports: [RouterLink, FontAwesomeModule, StatusBadgeComponent, ButtonComponent, CreateChannelComponent],
  template: `
    <!-- ЗАГОЛОВОК -->
    <div class="flex items-center justify-between mb-6">
      <div class="flex items-center gap-3">
        <div class="flex items-center justify-center w-10 h-10 rounded-xl
                    bg-[rgb(var(--accent)/0.15)] text-[rgb(var(--accent))]">
          <fa-icon [icon]="faLayerGroup" />
        </div>
        <div>
          <h1 class="text-2xl font-semibold text-[rgb(var(--text))]">Каналы</h1>
          <p class="text-sm text-[rgb(var(--text-muted))]">
            {{ store.channels().length }} всего · {{ store.runningCount() }} в эфире
          </p>
        </div>
      </div>

      <ui-button
        [label]="showForm() ? 'Отмена' : 'Новый канал'"
        [icon]="showForm() ? undefined : faPlus"
        [variant]="showForm() ? 'secondary' : 'primary'"
        (clicked)="showForm.set(!showForm())" />
    </div>

    <!-- ФОРМА СОЗДАНИЯ -->
    @if (showForm()) {
      <div class="mb-6">
        <app-create-channel (created)="onCreated()" />
      </div>
    }

    <!-- СПИСОК -->
    @if (store.loading()) {
      <div class="glass rounded-2xl p-10 text-center text-[rgb(var(--text-muted))]">
        Загрузка…
      </div>
    } @else if (store.channels().length === 0) {
      <div class="glass rounded-2xl p-12 text-center">
        <fa-icon [icon]="faTowerBroadcast" class="text-4xl text-[rgb(var(--text-muted))] opacity-30 mb-3" />
        <p class="text-[rgb(var(--text-muted))]">Пока нет каналов</p>
        <p class="text-sm text-[rgb(var(--text-muted))] opacity-70 mt-1">
          Создайте первый канал, чтобы начать трансляцию
        </p>
      </div>
    } @else {
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        @for (ch of store.channels(); track ch.channelId) {
          <a [routerLink]="['/channels', ch.channelId]"
            class="glass rounded-2xl p-5 transition
                   hover:border-[rgb(var(--accent)/0.4)] hover:-translate-y-0.5 duration-200 group">
            <div class="flex items-center justify-between mb-3">
              <span class="text-xs font-mono text-[rgb(var(--text-muted))] truncate max-w-[55%]">
                {{ ch.channelId.slice(0, 8) }}
              </span>
              <ui-status-badge [status]="ch.status" />
            </div>
            <p class="text-sm text-[rgb(var(--text))] truncate flex items-center gap-2">
              <fa-icon [icon]="faVideo" class="text-xs text-[rgb(var(--text-muted))] opacity-60" />
              {{ ch.sourceUrl }}
            </p>
          </a>
        }
      </div>
    }
  `,
})
export class ChannelListComponent implements OnInit {
  readonly store = inject(ChannelsStore);
  readonly showForm = signal(false);

  readonly faPlus = faPlus;
  readonly faVideo = faVideo;
  readonly faLayerGroup = faLayerGroup;
  readonly faTowerBroadcast = faTowerBroadcast;

  ngOnInit(): void { this.store.loadAll(); }

  onCreated(): void { this.showForm.set(false); }
}
