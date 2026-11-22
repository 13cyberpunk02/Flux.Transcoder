import { Component, inject, signal } from '@angular/core';
import {
  applyEach, form, FormField, maxLength, min, required, submit, validate,
} from '@angular/forms/signals';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect, MatOption } from '@angular/material/select';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';

import { ProfilesStore } from './profiles.store';
import { CODEC_OPTIONS, EncoderType } from '../../core/api/profiles.api';

interface RenditionModel { width: number; height: number; bitrateKbps: number; codec: EncoderType; }

const PRESETS: { label: string; r: Omit<RenditionModel, 'codec'> }[] = [
  { label: '4K', r: { width: 3840, height: 2160, bitrateKbps: 15000 } },
  { label: '1080p', r: { width: 1920, height: 1080, bitrateKbps: 6000 } },
  { label: '720p', r: { width: 1280, height: 720, bitrateKbps: 3000 } },
  { label: '480p', r: { width: 854, height: 480, bitrateKbps: 1500 } },
];

/** Диалог создания профиля кодирования (открывается через MatDialog). */
@Component({
  selector: 'app-create-profile',
  imports: [
    FormField, MatDialogModule, MatFormField, MatLabel, MatError, MatInput, MatSelect, MatOption,
    MatButton, MatIconButton, MatIcon, MatTooltip,
  ],
  template: `
    <form (submit)="onSubmit($event)" novalidate>
      <h2 mat-dialog-title>Новый профиль</h2>

      <mat-dialog-content>
        <div class="flex flex-col gap-3 pt-2">
        <mat-form-field>
          <mat-label>Название</mat-label>
          <input matInput autocomplete="off" placeholder="например, 1080p-nvenc" [formField]="profileForm.name" />
          @if (profileForm.name().invalid()) {
            <mat-error>{{ profileForm.name().errors()[0]?.message }}</mat-error>
          }
        </mat-form-field>

        <div class="flex items-center justify-between gap-2 flex-wrap">
          <span class="text-sm font-medium">Рендишены</span>
          <div class="flex items-center gap-1 flex-wrap">
            <span class="text-xs text-muted mr-1">Добавить:</span>
            @for (p of presets; track p.label) {
              <button matButton="outlined" type="button" (click)="addRendition(p.r)">{{ p.label }}</button>
            }
          </div>
        </div>

        @for (r of profileForm.renditions; track $index) {
          <div class="grid grid-cols-[1fr_1fr_1.2fr_1.5fr_auto] gap-2 items-start">
            <mat-form-field>
              <mat-label>Ширина</mat-label>
              <input matInput type="number" [formField]="r.width" />
            </mat-form-field>
            <mat-form-field>
              <mat-label>Высота</mat-label>
              <input matInput type="number" [formField]="r.height" />
            </mat-form-field>
            <mat-form-field>
              <mat-label>Битрейт, кбит/с</mat-label>
              <input matInput type="number" [formField]="r.bitrateKbps" />
            </mat-form-field>
            <mat-form-field>
              <mat-label>Кодек</mat-label>
              <mat-select [formField]="r.codec">
                @for (c of codecs; track c.value) {
                  <mat-option [value]="c.value">{{ c.label }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <button matIconButton type="button" class="danger-button mt-1" matTooltip="Удалить"
                    aria-label="Удалить рендишен" [disabled]="profileForm.renditions.length === 1"
                    (click)="removeRendition($index)">
              <mat-icon>delete</mat-icon>
            </button>
          </div>
        }

        @if (renditionsError(); as err) {
          <p class="text-xs text-error">{{ err }}</p>
        } @else {
          <p class="text-xs text-muted">Ширина и высота — от 16 px, битрейт — от 100 кбит/с. В эфир идёт старший рендишен.</p>
        }
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button matButton type="button" mat-dialog-close>Отмена</button>
        <button matButton="filled" type="submit" [disabled]="profileForm().submitting()">Создать</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class CreateProfileComponent {
  private readonly store = inject(ProfilesStore);
  private readonly dialogRef = inject(MatDialogRef<CreateProfileComponent>);

  protected readonly presets = PRESETS;
  protected readonly codecs = CODEC_OPTIONS;

  private readonly model = signal({
    name: '',
    renditions: [{ ...PRESETS[1].r, codec: EncoderType.X264 }] as RenditionModel[],
  });
  protected readonly profileForm = form(this.model, p => {
    required(p.name, { message: 'Укажите название профиля' });
    maxLength(p.name, 128, { message: 'Не длиннее 128 символов' });
    validate(p.renditions, ({ value }) =>
      value().length === 0 ? { kind: 'required', message: 'Нужен хотя бы один рендишен' } : undefined);
    applyEach(p.renditions, r => {
      min(r.width, 16, { message: 'Ширина — от 16 px' });
      min(r.height, 16, { message: 'Высота — от 16 px' });
      min(r.bitrateKbps, 100, { message: 'Битрейт — от 100 кбит/с' });
    });
  });

  /** Первая ошибка по рендишенам — одной строкой под списком. */
  protected renditionsError(): string | undefined {
    const own = this.profileForm.renditions().errors()[0]?.message;
    if (own) return own;
    for (const r of this.profileForm.renditions) {
      for (const f of [r.width, r.height, r.bitrateKbps]) {
        const e = f().errors()[0];
        if (e) return e.message;
      }
    }
    return undefined;
  }

  protected addRendition(r: Omit<RenditionModel, 'codec'>): void {
    const codec = this.model().renditions.at(-1)?.codec ?? EncoderType.X264;
    this.model.update(m => ({ ...m, renditions: [...m.renditions, { ...r, codec }] }));
  }

  protected removeRendition(index: number): void {
    this.model.update(m => ({ ...m, renditions: m.renditions.filter((_, i) => i !== index) }));
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    submit(this.profileForm, async () => {
      const { name, renditions } = this.model();
      if (await this.store.create({ name: name.trim(), renditions })) {
        this.dialogRef.close(true);
      }
    });
  }
}
