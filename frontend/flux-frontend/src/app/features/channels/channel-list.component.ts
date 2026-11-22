import { Component, computed, effect, inject, OnInit, signal, viewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { form, FormField } from '@angular/forms/signals';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortHeader } from '@angular/material/sort';
import { MatButtonToggle, MatButtonToggleGroup } from '@angular/material/button-toggle';
import { MatFormField, MatPrefix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';

import { ChannelsStore } from './channels.store';
import { ChannelActions } from './channel-actions';
import { ProfilesStore } from '../profiles/profiles.store';
import { CreateChannelComponent } from './create-channel.component';
import { StatusTagComponent } from '../../shared/status-tag.component';
import { BitratePipe, DurationPipe, RetentionPipe } from '../../core/format/format.pipes';
import { ChannelStatus, canRestart, canStop, isInactive } from '../../core/api/models';

type StatusFilter = 'all' | 'live' | 'problems' | 'off';

/** Порядок сортировки по статусу: проблемные — наверх. */
const STATUS_ORDER: Record<ChannelStatus, number> = {
  Failed: 0, Degraded: 1, Starting: 2, Running: 3, Stopped: 4, Idle: 5,
};

const FILTERS: Record<StatusFilter, (s: ChannelStatus) => boolean> = {
  all: () => true,
  live: s => s === 'Running' || s === 'Starting',
  problems: s => s === 'Failed' || s === 'Degraded',
  off: s => s === 'Stopped' || s === 'Idle',
};

interface ChannelRow {
  id: string;
  shortId: string;
  sourceUrl: string;
  status: ChannelStatus;
  profileName: string;
  bitrate: number | null;
  uptime: number | null;
  dropped: number | null;
  retentionMinutes: number;
}

@Component({
  selector: 'app-channel-list',
  imports: [
    RouterLink, FormField, MatTableModule, MatSort, MatSortHeader, MatButtonToggle, MatButtonToggleGroup,
    MatFormField, MatPrefix, MatInput, MatButton, MatIconButton, MatIcon, MatTooltip,
    StatusTagComponent, BitratePipe, DurationPipe, RetentionPipe,
  ],
  template: `
    <div class="flex items-center justify-between gap-4 flex-wrap mb-5">
      <div>
        <h1 class="text-2xl font-semibold">Каналы</h1>
        <p class="text-sm text-muted mt-0.5">
          <span class="num">{{ store.channels().length }}</span> всего ·
          <span class="num">{{ store.runningCount() }}</span> в эфире
          @if (store.problemCount() > 0) {
            · <span class="text-error"><span class="num">{{ store.problemCount() }}</span> с проблемами</span>
          }
        </p>
      </div>
      <button matButton="filled" (click)="openCreate()">
        <mat-icon>add</mat-icon> Новый канал
      </button>
    </div>

    <div class="panel overflow-hidden">
      <div class="flex items-center justify-between gap-3 flex-wrap px-4 py-3 border-b border-outline-variant">
        <mat-button-toggle-group [formField]="filters.status" hideSingleSelectionIndicator aria-label="Фильтр по статусу">
          @for (f of filterOptions(); track f.value) {
            <mat-button-toggle [value]="f.value">{{ f.label }} <span class="num text-muted">{{ f.count }}</span></mat-button-toggle>
          }
        </mat-button-toggle-group>

        <mat-form-field class="w-80 max-w-full dense-field">
          <mat-icon matPrefix>search</mat-icon>
          <input matInput type="search" placeholder="Поиск по источнику, ID, профилю"
                 aria-label="Поиск" [formField]="filters.search" />
        </mat-form-field>
      </div>

      <!-- relative: иначе sr-only в заголовке выходит за прокрутку и расширяет страницу -->
      <div class="relative overflow-x-auto">
        <table mat-table [dataSource]="dataSource" matSort matSortActive="status" matSortDirection="asc"
               class="w-full min-w-[60rem]">
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef mat-sort-header class="w-40">Статус</th>
            <td mat-cell *matCellDef="let row"><app-status-tag [status]="row.status" /></td>
          </ng-container>

          <ng-container matColumnDef="sourceUrl">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Источник</th>
            <td mat-cell *matCellDef="let row" class="max-w-0 w-full">
              <a [routerLink]="['/channels', row.id]" (click)="$event.stopPropagation()"
                 class="block truncate font-medium hover:text-primary">{{ row.sourceUrl }}</a>
              <span class="text-xs font-mono text-muted">{{ row.shortId }}</span>
            </td>
          </ng-container>

          <ng-container matColumnDef="profileName">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Профиль</th>
            <td mat-cell *matCellDef="let row" class="whitespace-nowrap">{{ row.profileName }}</td>
          </ng-container>

          <ng-container matColumnDef="bitrate">
            <th mat-header-cell *matHeaderCellDef mat-sort-header arrowPosition="before" class="!text-right">Битрейт</th>
            <td mat-cell *matCellDef="let row" class="text-right num whitespace-nowrap">{{ row.bitrate | bitrate }}</td>
          </ng-container>

          <ng-container matColumnDef="uptime">
            <th mat-header-cell *matHeaderCellDef mat-sort-header arrowPosition="before" class="!text-right">Аптайм</th>
            <td mat-cell *matCellDef="let row" class="text-right num whitespace-nowrap">{{ row.uptime | duration }}</td>
          </ng-container>

          <ng-container matColumnDef="dropped">
            <th mat-header-cell *matHeaderCellDef class="!text-right">Дропы</th>
            <td mat-cell *matCellDef="let row" class="text-right num"
                [class.text-amber-600]="(row.dropped ?? 0) > 0">{{ row.dropped ?? '—' }}</td>
          </ng-container>

          <ng-container matColumnDef="retention">
            <th mat-header-cell *matHeaderCellDef class="!text-right">Архив</th>
            <td mat-cell *matCellDef="let row" class="text-right whitespace-nowrap text-muted">
              {{ row.retentionMinutes | retention }}
            </td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef class="w-36"><span class="sr-only">Действия</span></th>
            <td mat-cell *matCellDef="let row" class="text-right whitespace-nowrap" (click)="$event.stopPropagation()">
              @if (isInactive(row.status)) {
                <button matIconButton class="success-button" matTooltip="Запустить" aria-label="Запустить"
                        (click)="actions.start(row.id)"><mat-icon>play_arrow</mat-icon></button>
              }
              @if (canRestart(row.status)) {
                <button matIconButton matTooltip="Перезапустить" aria-label="Перезапустить"
                        (click)="actions.restart(row.id)"><mat-icon>restart_alt</mat-icon></button>
              }
              @if (canStop(row.status)) {
                <button matIconButton class="danger-button" matTooltip="Остановить" aria-label="Остановить"
                        (click)="actions.stop(row.id, row.sourceUrl)"><mat-icon>stop</mat-icon></button>
              }
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns" class="clickable" (click)="open(row.id)"></tr>

          <tr *matNoDataRow>
            <td [attr.colspan]="columns.length">
              @if (!store.loaded()) {
                <div class="flex flex-col gap-3 p-4" aria-busy="true">
                  @for (i of [1, 2, 3, 4]; track i) {
                    <div class="h-8 rounded-md bg-surface-high animate-pulse"></div>
                  }
                </div>
              } @else {
                <div class="py-12 flex flex-col items-center gap-2 text-center">
                  <span class="material-symbols-outlined !text-4xl text-muted" aria-hidden="true">live_tv</span>
                  @if (store.channels().length === 0) {
                    <p class="font-medium">Пока нет каналов</p>
                    <p class="text-sm text-muted">Создайте первый канал, чтобы начать трансляцию</p>
                    <button matButton="tonal" class="mt-2" (click)="openCreate()">
                      <mat-icon>add</mat-icon> Новый канал
                    </button>
                  } @else {
                    <p class="font-medium">Ничего не найдено</p>
                    <p class="text-sm text-muted">Измените фильтр или строку поиска</p>
                  }
                </div>
              }
            </td>
          </tr>
        </table>
      </div>
    </div>
  `,
})
export class ChannelListComponent implements OnInit {
  protected readonly store = inject(ChannelsStore);
  protected readonly actions = inject(ChannelActions);
  private readonly profiles = inject(ProfilesStore);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  protected readonly columns = ['status', 'sourceUrl', 'profileName', 'bitrate', 'uptime', 'dropped', 'retention', 'actions'];

  protected readonly isInactive = isInactive;
  protected readonly canRestart = canRestart;
  protected readonly canStop = canStop;

  private readonly filterModel = signal<{ status: StatusFilter; search: string }>({ status: 'all', search: '' });
  protected readonly filters = form(this.filterModel);

  protected readonly filterOptions = computed(() => {
    const list = this.store.channels();
    const count = (f: StatusFilter) => list.filter(c => FILTERS[f](c.status)).length;
    return [
      { value: 'all', label: 'Все', count: list.length },
      { value: 'live', label: 'В эфире', count: count('live') },
      { value: 'problems', label: 'Проблемы', count: count('problems') },
      { value: 'off', label: 'Остановлены', count: count('off') },
    ];
  });

  /** Каналы + живые метрики из SignalR + имя профиля; фильтр и поиск. */
  private readonly rows = computed<ChannelRow[]>(() => {
    const { status, search } = this.filterModel();
    const health = this.store.health();
    const profiles = this.profiles.byId();
    const q = search.trim().toLowerCase();

    return this.store.channels()
      .filter(c => FILTERS[status](c.status))
      .map(c => {
        const h = isInactive(c.status) ? undefined : health[c.channelId];
        return {
          id: c.channelId,
          shortId: c.channelId.slice(0, 8),
          sourceUrl: c.sourceUrl,
          status: c.status,
          profileName: profiles.get(c.profileId)?.name ?? '—',
          bitrate: h?.outputBitrateKbps ?? null,
          uptime: h?.uptimeSeconds ?? null,
          dropped: h?.droppedFrames ?? null,
          retentionMinutes: c.retentionMinutes,
        };
      })
      .filter(r => !q
        || r.sourceUrl.toLowerCase().includes(q)
        || r.id.toLowerCase().includes(q)
        || r.profileName.toLowerCase().includes(q));
  });

  protected readonly dataSource = new MatTableDataSource<ChannelRow>([]);
  private readonly sort = viewChild(MatSort);

  constructor() {
    this.dataSource.sortingDataAccessor = (row, column) => {
      if (column === 'status') return STATUS_ORDER[row.status] ?? 9;
      const value = row[column as keyof ChannelRow];
      return typeof value === 'number' || typeof value === 'string' ? value : -1;
    };
    effect(() => { this.dataSource.data = this.rows(); });
    effect(() => {
      const sort = this.sort();
      if (sort) this.dataSource.sort = sort;
    });
  }

  ngOnInit(): void {
    this.store.loadAll();
    if (!this.profiles.loaded()) this.profiles.loadAll();
  }

  protected openCreate(): void {
    this.dialog.open(CreateChannelComponent, { width: '32rem', maxWidth: '95vw' });
  }

  protected open(id: string): void {
    this.router.navigate(['/channels', id]);
  }
}
