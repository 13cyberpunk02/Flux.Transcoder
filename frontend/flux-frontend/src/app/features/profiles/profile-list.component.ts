import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProfilesStore } from './profiles.store';
import { CreateRendition } from '../../core/api/profiles.api';

@Component({
  selector: 'app-profile-list',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-semibold">Профили кодирования</h1>
      <button (click)="showForm.set(!showForm())"
        class="px-4 py-2 rounded-md bg-indigo-600 hover:bg-indigo-500 transition font-medium">
        {{ showForm() ? 'Отмена' : '+ Новый профиль' }}
      </button>
    </div>

    @if (showForm()) {
      <div class="rounded-xl border border-slate-800 bg-slate-900/50 p-6 mb-8">
        <label class="block text-sm mb-1 text-slate-300">Название</label>
        <input [(ngModel)]="name"
          class="w-full mb-5 px-3 py-2 rounded-md bg-slate-800 border border-slate-700
                 focus:border-indigo-500 outline-none transition" placeholder="например 720p-ladder" />

        <div class="flex items-center justify-between mb-2">
          <span class="text-sm text-slate-300">Рендишены (ABR-лесенка)</span>
          <button (click)="addRendition()"
            class="text-sm px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 transition">+ строка</button>
        </div>

        <div class="space-y-3 mb-5">
          @for (r of renditions(); track $index) {
            <div class="grid grid-cols-12 gap-2 items-center">
              <input type="number" [(ngModel)]="r.width" placeholder="W"
                class="col-span-2 px-2 py-1.5 rounded bg-slate-800 border border-slate-700 outline-none" />
              <input type="number" [(ngModel)]="r.height" placeholder="H"
                class="col-span-2 px-2 py-1.5 rounded bg-slate-800 border border-slate-700 outline-none" />
              <input type="number" [(ngModel)]="r.bitrateKbps" placeholder="kbps"
                class="col-span-3 px-2 py-1.5 rounded bg-slate-800 border border-slate-700 outline-none" />
              <select [(ngModel)]="r.codec"
                class="col-span-4 px-2 py-1.5 rounded bg-slate-800 border border-slate-700 outline-none">
                <option [ngValue]="0">X264</option>
                <option [ngValue]="1">Nvenc</option>
                <option [ngValue]="2">Qsv</option>
              </select>
              <button (click)="removeRendition($index)"
                class="col-span-1 text-rose-400 hover:text-rose-300">✕</button>
            </div>
          }
        </div>

        @if (error()) { <p class="text-sm text-rose-400 mb-3">{{ error() }}</p> }

        <button (click)="save()" [disabled]="saving()"
          class="px-4 py-2 rounded-md bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 transition font-medium">
          {{ saving() ? 'Сохраняем…' : 'Создать профиль' }}
        </button>
      </div>
    }

    @if (store.loading()) {
      <p class="text-slate-400">Загрузка…</p>
    } @else if (store.profiles().length === 0) {
      <div class="rounded-xl border border-dashed border-slate-700 p-10 text-center text-slate-400">
        Пока нет профилей
      </div>
    } @else {
      <div class="grid gap-4 sm:grid-cols-2">
        @for (p of store.profiles(); track p.profileId) {
          <div class="rounded-xl border border-slate-800 bg-slate-900/50 p-5">
            <div class="flex items-center justify-between mb-3">
              <h3 class="font-medium">{{ p.name }}</h3>
              <span class="text-xs font-mono text-slate-500">{{ p.profileId.slice(0, 8) }}</span>
            </div>
            <div class="space-y-1">
              @for (r of p.renditions; track $index) {
                <div class="text-sm text-slate-400 flex justify-between">
                  <span>{{ r.width }}×{{ r.height }}</span>
                  <span>{{ r.bitrateKbps }} kbps · {{ r.codec }}</span>
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

  readonly showForm = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  name = '';
  readonly renditions = signal<CreateRendition[]>([
    { width: 1280, height: 720, bitrateKbps: 2500, codec: 0 },
  ]);

  ngOnInit(): void { this.store.loadAll(); }

  addRendition(): void {
    this.renditions.update(list => [...list, { width: 854, height: 480, bitrateKbps: 1200, codec: 0 }]);
  }
  removeRendition(i: number): void {
    this.renditions.update(list => list.filter((_, idx) => idx !== i));
  }

  async save(): Promise<void> {
    this.error.set(null);
    if (!this.name.trim()) { this.error.set('Укажите название'); return; }
    if (this.renditions().length === 0) { this.error.set('Нужен минимум один рендишен'); return; }

    this.saving.set(true);
    try {
      await this.store.create({ name: this.name, renditions: this.renditions() });
      this.name = '';
      this.renditions.set([{ width: 1280, height: 720, bitrateKbps: 2500, codec: 0 }]);
      this.showForm.set(false);
    } catch {
      this.error.set('Не удалось создать профиль');
    } finally {
      this.saving.set(false);
    }
  }
}
