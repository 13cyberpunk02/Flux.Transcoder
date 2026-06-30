import { Component, inject, signal, output, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChannelsStore } from './channels.store';
import { ProfilesStore } from '../profiles/profiles.store';

@Component({
  selector: 'app-create-channel',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="rounded-xl border border-slate-800 bg-slate-900/50 p-6">
      <h3 class="font-medium mb-4">Новый канал</h3>

      <label class="block text-sm mb-1 text-slate-300">Источник (srt:// / rtmp:// / udp://)</label>
      <input [(ngModel)]="sourceUrl"
             class="w-full mb-4 px-3 py-2 rounded-md bg-slate-800 border border-slate-700
               focus:border-indigo-500 outline-none transition" placeholder="srt://127.0.0.1:9000" />

      <label class="block text-sm mb-1 text-slate-300">Профиль</label>
      <select [(ngModel)]="profileId"
              class="w-full mb-5 px-3 py-2 rounded-md bg-slate-800 border border-slate-700 outline-none transition">
        <option [ngValue]="null" disabled>— выберите профиль —</option>
        @for (p of profiles.profiles(); track p.profileId) {
          <option [ngValue]="p.profileId">{{ p.name }} ({{ p.renditions.length }} рендишенов)</option>
        }
      </select>

      @if (profiles.profiles().length === 0) {
        <p class="text-sm text-amber-400 mb-3">
          Сначала создайте профиль в разделе «Профили»
        </p>
      }

      @if (error()) { <p class="text-sm text-rose-400 mb-3">{{ error() }}</p> }

      <button (click)="save()" [disabled]="saving()"
              class="px-4 py-2 rounded-md bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 transition font-medium">
        {{ saving() ? 'Создаём…' : 'Создать канал' }}
      </button>
    </div>
  `,
})
export class CreateChannelComponent implements OnInit {
  private readonly channels = inject(ChannelsStore);
  readonly profiles = inject(ProfilesStore);

  readonly created = output<void>();   // <- событие наружу

  sourceUrl = '';
  profileId: string | null = null;
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void { this.profiles.loadAll(); }

  save(): void {
    this.error.set(null);
    if (!this.sourceUrl.trim()) { this.error.set('Укажите источник'); return; }
    if (!this.profileId) { this.error.set('Выберите профиль'); return; }

    this.saving.set(true);
    this.channels.create({ sourceUrl: this.sourceUrl, profileId: this.profileId });
    this.sourceUrl = '';
    this.profileId = null;
    this.saving.set(false);
    this.created.emit();   // <- сообщаем списку, что создали
  }
}
