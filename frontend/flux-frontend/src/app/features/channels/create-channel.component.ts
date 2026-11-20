import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { form, FormField, pattern, required, submit } from '@angular/forms/signals';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormField, MatLabel, MatError, MatHint } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatButton } from '@angular/material/button';

import { ChannelsStore } from './channels.store';
import { ProfilesStore } from '../profiles/profiles.store';

const SOURCE_SCHEME = /^(srt|rtmp|udp):\/\/.+/i;

/** Диалог создания канала (открывается через MatDialog). */
@Component({
  selector: 'app-create-channel',
  imports: [
    FormField, RouterLink, MatDialogModule, MatFormField, MatLabel, MatError, MatHint,
    MatInput, MatSelect, MatOption, MatButton,
  ],
  template: `
    <form (submit)="onSubmit($event)" novalidate>
      <h2 mat-dialog-title>Новый канал</h2>

      <mat-dialog-content>
        <!-- раскладка во внутренней обёртке: стили Material (вне @layer) перекрывают утилиты Tailwind на самом mat-dialog-content -->
        <div class="flex flex-col gap-2 pt-2">
        <mat-form-field>
          <mat-label>Источник</mat-label>
          <input matInput autocomplete="off" placeholder="srt://host:9000"
                 [formField]="channelForm.sourceUrl" />
          <mat-hint>SRT, RTMP или UDP: srt://…, rtmp://…, udp://…</mat-hint>
          @if (channelForm.sourceUrl().invalid()) {
            <mat-error>{{ channelForm.sourceUrl().errors()[0]?.message }}</mat-error>
          }
        </mat-form-field>

        <mat-form-field>
          <mat-label>Профиль кодирования</mat-label>
          <mat-select [formField]="channelForm.profileId">
            @for (p of profileOptions(); track p.value) {
              <mat-option [value]="p.value">{{ p.label }}</mat-option>
            }
          </mat-select>
          @if (channelForm.profileId().invalid()) {
            <mat-error>{{ channelForm.profileId().errors()[0]?.message }}</mat-error>
          }
        </mat-form-field>

        @if (profiles.loaded() && profiles.profiles().length === 0) {
          <div class="flex items-center gap-2 rounded-lg px-3 py-2 text-sm bg-amber-500/15 text-amber-800 dark:text-amber-300">
            <span class="material-symbols-outlined" aria-hidden="true">info</span>
            <span>Профилей пока нет —
              <a routerLink="/profiles" class="underline" mat-dialog-close>создайте профиль</a>.</span>
          </div>
        }
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button matButton type="button" mat-dialog-close>Отмена</button>
        <button matButton="filled" type="submit" [disabled]="channelForm().submitting()">Создать</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class CreateChannelComponent implements OnInit {
  private readonly channels = inject(ChannelsStore);
  private readonly dialogRef = inject(MatDialogRef<CreateChannelComponent>);
  protected readonly profiles = inject(ProfilesStore);

  private readonly model = signal({ sourceUrl: '', profileId: '' });
  protected readonly channelForm = form(this.model, p => {
    required(p.sourceUrl, { message: 'Укажите адрес источника' });
    pattern(p.sourceUrl, SOURCE_SCHEME, { message: 'Адрес должен начинаться с srt://, rtmp:// или udp://' });
    required(p.profileId, { message: 'Выберите профиль' });
  });

  protected readonly profileOptions = computed(() =>
    this.profiles.profiles().map(p => ({
      value: p.profileId,
      label: `${p.name} · ${p.renditions.length} рендишен(ов)`,
    })));

  ngOnInit(): void {
    if (!this.profiles.loaded()) this.profiles.loadAll();
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    submit(this.channelForm, async () => {
      const { sourceUrl, profileId } = this.model();
      if (await this.channels.create({ sourceUrl: sourceUrl.trim(), profileId })) {
        this.dialogRef.close(true);
      }
    });
  }
}
