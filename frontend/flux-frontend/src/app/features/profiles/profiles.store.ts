import { Injectable, inject, signal, computed } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProfilesApi, ProfileView, CreateProfileRequest } from '../../core/api/profiles.api';
import { Notifier } from '../../core/notify/notifier';

@Injectable({ providedIn: 'root' })
export class ProfilesStore {
  private readonly api = inject(ProfilesApi);
  private readonly notify = inject(Notifier);

  private readonly _profiles = signal<ProfileView[]>([]);
  private readonly _loading = signal(false);
  private readonly _loaded = signal(false);

  readonly profiles = this._profiles.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly loaded = this._loaded.asReadonly();

  /** id -> профиль, для подписи профиля в таблице каналов. */
  readonly byId = computed(() => new Map(this._profiles().map(p => [p.profileId, p])));

  loadAll(): void {
    this._loading.set(true);
    this.api.getAll().subscribe({
      next: list => {
        this._profiles.set(list);
        this._loading.set(false);
        this._loaded.set(true);
      },
      error: err => {
        this._loading.set(false);
        this.notify.error('Не удалось загрузить профили', err);
      },
    });
  }

  async create(body: CreateProfileRequest): Promise<boolean> {
    try {
      await firstValueFrom(this.api.create(body));
      this.loadAll();
      this.notify.success('Профиль создан');
      return true;
    } catch (err) {
      this.notify.error('Не удалось создать профиль', err);
      return false;
    }
  }
}
