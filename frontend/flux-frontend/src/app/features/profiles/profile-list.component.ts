import { Component, inject, signal, OnInit } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faPlus, faSliders, faTrash, faTag, faXmark } from '@fortawesome/free-solid-svg-icons';

import { ProfilesStore } from './profiles.store';
import { CreateRendition } from '../../core/api/profiles.api';
import { InputComponent } from '../../shared/ui/input.component';
import { SelectComponent, SelectOption } from '../../shared/ui/select.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { IconButtonComponent } from '../../shared/ui/icon-button.component';
import { BadgeComponent } from '../../shared/ui/badge.component';
import { ToastService } from '../../core/toast/toast.service';

interface RenditionRow {
  width: string;
  height: string;
  bitrateKbps: string;
  codec: number;
}

@Component({
  selector: 'app-profile-list',
  standalone: true,
  imports: [
    FontAwesomeModule, InputComponent, SelectComponent,
    ButtonComponent, IconButtonComponent, BadgeComponent,
  ],
  template: `
    <!-- ЗАГОЛОВОК -->
    <div class="flex items-center justify-between mb-6 flex-wrap gap-3">
      <div class="flex items-center gap-3">
        <div class="flex items-center justify-center w-10 h-10 rounded-xl
                    bg-[rgb(var(--accent)/0.15)] text-[rgb(var(--accent))]">
          <fa-icon [icon]="faSliders" />
        </div>
        <div>
          <h1 class="text-2xl font-semibold text-[rgb(var(--text))]">Профили кодирования</h1>
          <p class="text-sm text-[rgb(var(--text-muted))]">{{ store.profiles().length }} профилей</p>
        </div>
      </div>

      <ui-button
        [label]="showForm() ? 'Отмена' : 'Новый профиль'"
        [icon]="showForm() ? faXmark : faPlus"
        [variant]="showForm() ? 'secondary' : 'primary'"
        (clicked)="toggleForm()" />
    </div>

    <!-- ФОРМА СОЗДАНИЯ -->
    @if (showForm()) {
      <div class="glass rounded-2xl p-6 mb-8">
        <div class="flex flex-col gap-5">
          <ui-input
            label="Название профиля" [labelIcon]="faTag"
            placeholder="например 1080p-nvenc"
            [(value)]="name" />

          <!-- рендишены -->
          <div>
            <div class="flex items-center justify-between mb-3">
              <span class="text-sm text-[rgb(var(--text-muted))]">Рендишены (ABR-лесенка)</span>
              <ui-button label="Строка" [icon]="faPlus" variant="secondary" size="sm"
                (clicked)="addRendition()" />
            </div>

            <div class="flex flex-col gap-2">
              @for (r of renditions(); track $index) {
                <div class="flex items-center gap-2 flex-wrap p-2 rounded-xl
                            bg-[rgb(var(--surface)/var(--surface-opacity))]
                            border border-[rgb(var(--border)/var(--border-opacity))]">
                  <ui-input type="number" size="sm" class="w-24"
                    placeholder="Ширина"
                    [value]="r.width" (valueChange)="updateRendition($index, 'width', $event)" />
                  <span class="text-[rgb(var(--text-muted))]">×</span>
                  <ui-input type="number" size="sm" class="w-24"
                    placeholder="Высота"
                    [value]="r.height" (valueChange)="updateRendition($index, 'height', $event)" />
                  <ui-input type="number" size="sm" class="w-28"
                    placeholder="kbps"
                    [value]="r.bitrateKbps" (valueChange)="updateRendition($index, 'bitrateKbps', $event)" />
                  <ui-select size="sm" class="flex-1 min-w-32"
                    [options]="codecOptions"
                    [value]="r.codec"
                    (changed)="updateCodec($index, $event)" />
                  <ui-icon-button [icon]="faTrash" variant="danger" size="sm"
                    (clicked)="removeRendition($index)" />
                </div>
              }
            </div>
          </div>

          <ui-button class="self-start"
            label="Создать профиль" [icon]="faPlus"
            variant="primary" [loading]="saving()"
            (clicked)="save()" />
        </div>
      </div>
    }

    <!-- СПИСОК -->
    @if (store.loading()) {
      <div class="glass rounded-2xl p-10 text-center text-[rgb(var(--text-muted))]">Загрузка…</div>
    } @else if (store.profiles().length === 0) {
      <div class="glass rounded-2xl p-12 text-center">
        <fa-icon [icon]="faSliders" class="text-4xl text-[rgb(var(--text-muted))] opacity-30 mb-3" />
        <p class="text-[rgb(var(--text-muted))]">Пока нет профилей</p>
        <p class="text-sm text-[rgb(var(--text-muted))] opacity-70 mt-1">
          Создайте профиль кодирования, чтобы использовать его в каналах
        </p>
      </div>
    } @else {
      <div class="grid gap-4 sm:grid-cols-2">
        @for (p of store.profiles(); track p.profileId) {
          <div class="glass rounded-2xl p-5">
            <div class="flex items-center justify-between mb-4">
              <h3 class="font-medium text-[rgb(var(--text))] flex items-center gap-2">
                <fa-icon [icon]="faTag" class="text-[rgb(var(--accent))] text-sm" />
                {{ p.name }}
              </h3>
              <ui-badge tone="neutral">{{ p.renditions.length }} рендишенов</ui-badge>
            </div>

            <div class="flex flex-col gap-1.5">
              @for (r of p.renditions; track $index) {
                <div class="flex justify-between items-center text-sm px-3 py-2 rounded-lg
                            bg-[rgb(var(--surface)/var(--surface-opacity))]">
                  <span class="text-[rgb(var(--text))] font-medium">{{ r.width }}×{{ r.height }}</span>
                  <div class="flex items-center gap-2">
                    <span class="text-[rgb(var(--text-muted))]">{{ r.bitrateKbps }} kbps</span>
                    <ui-badge tone="accent">{{ r.codec }}</ui-badge>
                  </div>
                </div>
              }
            </div>
          </div>
        }
      </div>
    }
  `,
})
export class ProfileListComponent implements OnInit {
  readonly store = inject(ProfilesStore);
  private readonly toast = inject(ToastService);

  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly name = signal('');
  readonly renditions = signal<RenditionRow[]>([
    { width: '1920', height: '1080', bitrateKbps: '6000', codec: 0 },
  ]);

  readonly faPlus = faPlus;
  readonly faSliders = faSliders;
  readonly faTrash = faTrash;
  readonly faTag = faTag;
  readonly faXmark = faXmark;

  readonly codecOptions: SelectOption[] = [
    { value: 0, label: 'X264' },
    { value: 1, label: 'Nvenc (NVIDIA)' },
    { value: 2, label: 'QSV (Intel)' },
  ];

  ngOnInit(): void { this.store.loadAll(); }

  toggleForm(): void { this.showForm.update(v => !v); }

  addRendition(): void {
    this.renditions.update(list => [
      ...list, { width: '1280', height: '720', bitrateKbps: '3000', codec: 0 },
    ]);
  }

  removeRendition(i: number): void {
    this.renditions.update(list => list.filter((_, idx) => idx !== i));
  }

  updateRendition(i: number, field: 'width' | 'height' | 'bitrateKbps', value: string): void {
    this.renditions.update(list =>
      list.map((r, idx) => idx === i ? { ...r, [field]: value } : r));
  }

  updateCodec(i: number, codec: number): void {
    this.renditions.update(list =>
      list.map((r, idx) => idx === i ? { ...r, codec } : r));
  }

  async save(): Promise<void> {
    if (!this.name().trim()) { this.toast.error('Укажите название профиля'); return; }
    if (this.renditions().length === 0) { this.toast.error('Нужен минимум один рендишен'); return; }

    const renditions: CreateRendition[] = this.renditions().map(r => ({
      width: parseInt(r.width, 10) || 0,
      height: parseInt(r.height, 10) || 0,
      bitrateKbps: parseInt(r.bitrateKbps, 10) || 0,
      codec: r.codec,
    }));

    // валидация чисел
    if (renditions.some(r => r.width < 1 || r.height < 1 || r.bitrateKbps < 1)) {
      this.toast.error('Проверьте размеры и битрейт рендишенов');
      return;
    }

    this.saving.set(true);
    try {
      await this.store.create({ name: this.name(), renditions });
      this.toast.success('Профиль создан');
      this.name.set('');
      this.renditions.set([{ width: '1920', height: '1080', bitrateKbps: '6000', codec: 0 }]);
      this.showForm.set(false);
    } catch {
      this.toast.error('Не удалось создать профиль');
    } finally {
      this.saving.set(false);
    }
  }
}
