import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface RenditionView { width: number; height: number; bitrateKbps: number; codec: string; }
export interface ProfileView { profileId: string; name: string; renditions: RenditionView[]; }

export interface CreateRendition { width: number; height: number; bitrateKbps: number; codec: string; }
export interface CreateProfileRequest { name: string; renditions: CreateRendition[]; }

@Injectable({ providedIn: 'root' })
export class ProfilesApi {
  private readonly http = inject(HttpClient);
  getAll() { return this.http.get<ProfileView[]>('/api/profiles'); }
  create(body: CreateProfileRequest) { return this.http.post<{ profileId: string }>('/api/profiles', body); }
}
