import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faPlus, faSliders, faLayerGroup, faTrash, faTag,
} from '@fortawesome/free-solid-svg-icons';

import { ProfilesStore } from './profiles.store';
import { CreateRendition } from '../../core/api/profiles.api';
import { InputComponent } from '../../shared/ui/input.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { IconButtonComponent } from '../../shared/ui/icon-button.component';
import { ToastService } from '../../core/toast/toast.service';
import {SelectComponent} from '../../shared/ui/select.component';

@Component({
  selector: 'app-profile-list',
  standalone: true,
  imports: [FormsModule, FontAwesomeModule, InputComponent, ButtonComponent, IconButtonComponent, SelectComponent],
  template: `
    <!-- ЗАГОЛОВОК -->
    <div class="flex items-center justify-between mb-6">
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
        [icon]="showForm() ? undefined : faPlus"
        [variant]="showForm() ? 'secondary' : 'primary'"
        (clicked)="showForm.set(!showForm())" />
    </div>

    <!-- ФОРМА СОЗДАНИЯ -->
    @if (showForm()) {
      <div class="glass rounded-2xl p-6 mb-8 relative z-20">
        <div class="flex flex-col gap-5">
          <ui-input label="Название профиля" [labelIcon]="faTag"
            placeholder="например 1080p-nvenc" [(value)]="name" />

          <!-- рендишены -->
          <div>
            <div class="flex items-center justify-between mb-3">
              <span class="text-sm text-[rgb(var(--text-muted))]">Рендишены (ABR-лесенка)</span>
              <ui-button label="Строка" [icon]="faPlus" variant="secondary" size="sm"
                (clicked)="addRendition()" />
            </div>

            <div class="flex flex-col gap-2">
              @for (r of renditions(); track $index) {
                <div class="flex items-center gap-2">
                  <input type="number" [(ngModel)]="r.width" placeholder="Ширина"
                    class="w-24 px-3 py-2 rounded-xl outline-none text-sm
                           bg-[rgb(var(--surface)/var(--surface-opacity))]
                           border border-[rgb(var(--border)/var(--border-opacity))]
                           text-[rgb(var(--text))] focus:border-[rgb(var(--accent)/0.5)]" />
                  <span class="text-[rgb(var(--text-muted))]">×</span>
                  <input type="number" [(ngModel)]="r.height" placeholder="Высота"
                    class="w-24 px-3 py-2 rounded-xl outline-none text-sm
                           bg-[rgb(var(--surface)/var(--surface-opacity))]
                           border border-[rgb(var(--border)/var(--border-opacity))]
                           text-[rgb(var(--text))] focus:border-[rgb(var(--accent)/0.5)]" />
                  <input type="number" [(ngModel)]="r.bitrateKbps" placeholder="kbps"
                    class="w-28 px-3 py-2 rounded-xl outline-none text-sm
                           bg-[rgb(var(--surface)/var(--surface-opacity))]
                           border border-[rgb(var(--border)/var(--border-opacity))]
                           text-[rgb(var(--text))] focus:border-[rgb(var(--accent)/0.5)]" />
                  <ui-select
                    class="flex-1"
                    [options]="codecOptions"
                    [value]="r.codec"
                    (changed)="r.codec = $event" />
                  <ui-icon-button [icon]="faTrash" variant="danger" size="sm"
                    (clicked)="removeRendition($index)" />
                </div>
              }
            </div>
          </div>

          <ui-button class="block self-start"
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
      </div>
    } @else {
      <div class="grid gap-4 sm:grid-cols-2">
        @for (p of store.profiles(); track p.profileId) {
          <div class="glass rounded-2xl p-5">
            <div class="flex items-center justify-between mb-3">
              <h3 class="font-medium text-[rgb(var(--text))] flex items-center gap-2">
                <fa-icon [icon]="faTag" class="text-[rgb(var(--accent))] text-sm" />
                {{ p.name }}
              </h3>
              <span class="text-xs font-mono text-[rgb(var(--text-muted))]">{{ p.profileId.slice(0, 8) }}</span>
            </div>
            <div class="flex flex-col gap-1.5">
              @for (r of p.renditions; track $index) {
                <div class="flex justify-between items-center text-sm px-3 py-1.5 rounded-lg
                            bg-[rgb(var(--surface)/var(--surface-opacity))]">
                  <span class="text-[rgb(var(--text))]">{{ r.width }}×{{ r.height }}</span>
                  <span class="text-[rgb(var(--text-muted))]">{{ r.bitrateKbps }} kbps · {{ r.codec }}</span>
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
  readonly renditions = signal<CreateRendition[]>([
    { width: 1920, height: 1080, bitrateKbps: 6000, codec: 0 },
  ]);

  readonly codecOptions = [
    { value: 0, label: 'X264' },
    { value: 1, label: 'Nvenc (NVIDIA)' },
    { value: 2, label: 'QSV (Intel)' },
  ];

  readonly faPlus = faPlus;
  readonly faSliders = faSliders;
  readonly faTrash = faTrash;
  readonly faTag = faTag;

  ngOnInit(): void { this.store.loadAll(); }

  addRendition(): void {
    this.renditions.update(list => [...list, { width: 1280, height: 720, bitrateKbps: 3000, codec: 0 }]);
  }
  removeRendition(i: number): void {
    this.renditions.update(list => list.filter((_, idx) => idx !== i));
  }

  async save(): Promise<void> {
    if (!this.name().trim()) { this.toast.error('Укажите название'); return; }
    if (this.renditions().length === 0) { this.toast.error('Нужен минимум один рендишен'); return; }

    this.saving.set(true);
    try {
      await this.store.create({ name: this.name(), renditions: this.renditions() });
      this.toast.success('Профиль создан');
      this.name.set('');
      this.renditions.set([{ width: 1920, height: 1080, bitrateKbps: 6000, codec: 0 }]);
      this.showForm.set(false);
    } catch {
      this.toast.error('Не удалось создать профиль');
    } finally {
      this.saving.set(false);
    }
  }
}
