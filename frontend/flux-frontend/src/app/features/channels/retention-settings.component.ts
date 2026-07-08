import { Component, inject, input, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChannelsStore } from './channels.store';

@Component({
  selector: 'app-retention-settings',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="rounded-xl border border-slate-800 bg-slate-900/50 p-5">
      <h3 class="font-medium mb-3">Хранение архива</h3>
      <p class="text-sm text-slate-400 mb-4">
        Сейчас: {{ currentLabel() }}
      </p>

      <div class="flex gap-2 items-end">
        <div>
          <label class="block text-xs text-slate-500 mb-1">Значение</label>
          <input type="number" [(ngModel)]="value" min="1"
            class="w-24 px-3 py-2 rounded-md bg-slate-800 border border-slate-700 outline-none" />
        </div>
        <div>
          <label class="block text-xs text-slate-500 mb-1">Единица</label>
          <select [(ngModel)]="unit"
            class="px-3 py-2 rounded-md bg-slate-800 border border-slate-700 outline-none">
            <option value="minutes">минут</option>
            <option value="hours">часов</option>
            <option value="days">дней</option>
          </select>
        </div>
        <button (click)="save()" [disabled]="saving()"
          class="px-4 py-2 rounded-md bg-indigo-600 hover:bg-indigo-500 disabled:opacity-40 transition font-medium">
          {{ saving() ? 'Сохраняем…' : 'Сохранить' }}
        </button>
      </div>

      @if (error()) { <p class="text-sm text-rose-400 mt-2">{{ error() }}</p> }
    </div>
  `,
})
export class RetentionSettingsComponent {
  readonly channelId = input.required<string>();
  readonly currentMinutes = input.required<number>();
  private readonly store = inject(ChannelsStore);

  value = 7;
  unit: 'minutes' | 'hours' | 'days' = 'days';
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly currentLabel = computed(() => {
    const m = this.currentMinutes();
    if (m % (24 * 60) === 0) return `${m / (24 * 60)} дн.`;
    if (m % 60 === 0) return `${m / 60} ч.`;
    return `${m} мин.`;
  });

  save(): void {
    this.error.set(null);
    const minutes = this.toMinutes();
    if (minutes < 1) { this.error.set('Минимум 1 минута'); return; }
    if (minutes > 14 * 24 * 60) { this.error.set('Максимум 14 дней'); return; }

    this.saving.set(true);
    this.store.updateRetention(this.channelId(), minutes);
    this.saving.set(false);
  }

  private toMinutes(): number {
    switch (this.unit) {
      case 'days': return this.value * 24 * 60;
      case 'hours': return this.value * 60;
      default: return this.value;
    }
  }
}
