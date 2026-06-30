import { Injectable, inject, signal } from '@angular/core';
import { ProfilesApi, ProfileView, CreateProfileRequest } from '../../core/api/profiles.api';

@Injectable({ providedIn: 'root' })
export class ProfilesStore {
  private readonly api = inject(ProfilesApi);
  private readonly _profiles = signal<ProfileView[]>([]);
  private readonly _loading = signal(false);

  readonly profiles = this._profiles.asReadonly();
  readonly loading = this._loading.asReadonly();

  loadAll(): void {
    this._loading.set(true);
    this.api.getAll().subscribe({
      next: list => { this._profiles.set(list); this._loading.set(false); },
      error: () => this._loading.set(false),
    });
  }

  create(body: CreateProfileRequest): Promise<void> {
    return new Promise((resolve, reject) => {
      this.api.create(body).subscribe({
        next: () => { this.loadAll(); resolve(); },
        error: err => reject(err),
      });
    });
  }
}
