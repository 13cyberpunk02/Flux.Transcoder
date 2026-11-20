import { Component, computed, inject, OnInit } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';

import { ProfilesStore } from './profiles.store';
import { ChannelsStore } from '../channels/channels.store';
import { CreateProfileComponent } from './create-profile.component';
import { BitratePipe } from '../../core/format/format.pipes';
import { codecShort } from '../../core/api/profiles.api';

@Component({
  selector: 'app-profile-list',
  imports: [MatTableModule, MatButton, MatIcon, BitratePipe],
  template: `
    <div class="flex items-center justify-between gap-4 flex-wrap mb-5">
      <div>
        <h1 class="text-2xl font-semibold">Профили кодирования</h1>
        <p class="text-sm text-muted mt-0.5">Набор рендишенов (разрешение, битрейт, кодек), который используют каналы</p>
      </div>
      <button matButton="filled" (click)="openCreate()"><mat-icon>add</mat-icon> Новый профиль</button>
    </div>

    <div class="panel overflow-hidden">
      <div class="overflow-x-auto">
        <table mat-table [dataSource]="sorted()" class="min-w-[48rem]">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef class="w-64">Название</th>
            <td mat-cell *matCellDef="let p" class="font-medium">{{ p.name }}</td>
          </ng-container>

          <ng-container matColumnDef="renditions">
            <th mat-header-cell *matHeaderCellDef>Рендишены</th>
            <td mat-cell *matCellDef="let p" class="!py-2">
              <div class="flex flex-wrap gap-1.5">
                @for (r of p.renditions; track $index) {
                  <span class="inline-flex items-center gap-1.5 text-sm px-2 py-1 rounded-md bg-surface-container num">
                    {{ r.width }}×{{ r.height }}
                    <span class="text-muted">· {{ r.bitrateKbps | bitrate }}</span>
                    <span class="text-xs font-medium px-1.5 rounded bg-primary-container text-on-primary-container">
                      {{ codecShort(r.codec) }}
                    </span>
                  </span>
                }
              </div>
            </td>
          </ng-container>

          <ng-container matColumnDef="usage">
            <th mat-header-cell *matHeaderCellDef class="!text-right w-32">Каналов</th>
            <td mat-cell *matCellDef="let p" class="text-right num">{{ usage().get(p.profileId) ?? 0 }}</td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let p; columns: columns"></tr>

          <tr *matNoDataRow>
            <td [attr.colspan]="columns.length">
              @if (!store.loaded()) {
                <div class="flex flex-col gap-3 p-4" aria-busy="true">
                  @for (i of [1, 2, 3]; track i) {
                    <div class="h-8 rounded-md bg-surface-high animate-pulse"></div>
                  }
                </div>
              } @else {
                <div class="py-12 flex flex-col items-center gap-2 text-center">
                  <span class="material-symbols-outlined !text-4xl text-muted" aria-hidden="true">tune</span>
                  <p class="font-medium">Пока нет профилей</p>
                  <p class="text-sm text-muted">Профиль нужен, чтобы создать канал</p>
                  <button matButton="tonal" class="mt-2" (click)="openCreate()">
                    <mat-icon>add</mat-icon> Новый профиль
                  </button>
                </div>
              }
            </td>
          </tr>
        </table>
      </div>
    </div>
  `,
})
export class ProfileListComponent implements OnInit {
  protected readonly store = inject(ProfilesStore);
  private readonly channels = inject(ChannelsStore);
  private readonly dialog = inject(MatDialog);

  protected readonly columns = ['name', 'renditions', 'usage'];
  protected readonly codecShort = codecShort;

  protected readonly sorted = computed(() =>
    [...this.store.profiles()].sort((a, b) => a.name.localeCompare(b.name, 'ru')));

  /** Сколько каналов использует каждый профиль. */
  protected readonly usage = computed(() => {
    const map = new Map<string, number>();
    for (const c of this.channels.channels()) map.set(c.profileId, (map.get(c.profileId) ?? 0) + 1);
    return map;
  });

  ngOnInit(): void {
    this.store.loadAll();
  }

  protected openCreate(): void {
    this.dialog.open(CreateProfileComponent, { width: '48rem', maxWidth: '95vw' });
  }
}
