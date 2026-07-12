import {Component, inject, signal, output, OnInit, computed} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faLink, faSliders, faPlus } from '@fortawesome/free-solid-svg-icons';

import { ChannelsStore } from './channels.store';
import { ProfilesStore } from '../profiles/profiles.store';
import { InputComponent } from '../../shared/ui/input.component';
import { ButtonComponent } from '../../shared/ui/button.component';
import { ToastService } from '../../core/toast/toast.service';
import {SelectComponent} from '../../shared/ui/select.component';

@Component({
  selector: 'app-create-channel',
  standalone: true,
  imports: [FormsModule, FontAwesomeModule, InputComponent, ButtonComponent, SelectComponent],
  template: `
    <div class="glass rounded-2xl p-6 relative z-20">
      <h3 class="font-medium text-[rgb(var(--text))] mb-4 flex items-center gap-2">
        <fa-icon [icon]="faPlus" class="text-[rgb(var(--accent))] text-sm" />
        Новый канал
      </h3>

      <div class="flex flex-col gap-4">
        <!-- источник через ui-input -->
        <ui-input
          label="Источник" [labelIcon]="faLink" [icon]="faLink"
          placeholder="rtmp://... / srt://... / udp://..."
          [(value)]="sourceUrl" />

        <!-- профиль -->
        <div>
          <ui-select
            label="Профиль кодирования" [labelIcon]="faSliders"
            [options]="profileOptions()"
            placeholder="— выберите профиль —"
            [(value)]="profileId" />

          @if (profiles.profiles().length === 0) {
            <p class="text-sm text-amber-400 mt-2">
              Сначала создайте профиль в разделе «Профили»
            </p>
          }
        </div>

        <ui-button
          class="block self-start"
          label="Создать канал" [icon]="faPlus"
          variant="primary" [loading]="saving()"
          (clicked)="save()" />
      </div>
    </div>
  `,
})
export class CreateChannelComponent implements OnInit {
  private readonly channels = inject(ChannelsStore);
  readonly profiles = inject(ProfilesStore);
  private readonly toast = inject(ToastService);

  readonly created = output<void>();

  sourceUrl = signal('');
  readonly saving = signal(false);

  profileId = signal<string | null>(null);
  readonly profileOptions = computed(() =>
    this.profiles.profiles().map(p => ({
      value: p.profileId,
      label: `${p.name} (${p.renditions.length} рендишенов)`,
    })));

  readonly faLink = faLink;
  readonly faSliders = faSliders;
  readonly faPlus = faPlus;

  ngOnInit(): void { this.profiles.loadAll(); }

  save(): void {
    if (!this.sourceUrl().trim()) { this.toast.error('Укажите источник'); return; }
    if (!this.profileId()) { this.toast.error('Выберите профиль'); return; }

    this.saving.set(true);
    this.channels.create({ sourceUrl: this.sourceUrl(), profileId: this.profileId()! });
    this.sourceUrl.set('');
    this.profileId.set(null);
    this.saving.set(false);
    this.created.emit();
  }
}
