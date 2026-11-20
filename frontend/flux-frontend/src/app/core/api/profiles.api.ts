import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

/** Значения enum EncoderType на бэкенде (передаются числом). */
export enum EncoderType { X264 = 0, Nvenc = 1, Qsv = 2 }

export const CODEC_OPTIONS = [
  { value: EncoderType.X264, label: 'x264 (CPU)', short: 'x264' },
  { value: EncoderType.Nvenc, label: 'NVENC (NVIDIA)', short: 'NVENC' },
  { value: EncoderType.Qsv, label: 'QSV (Intel)', short: 'QSV' },
];

/** Codec в RenditionView приходит строкой имени enum: "X264" / "Nvenc" / "Qsv". */
export const codecShort = (codec: string) =>
  CODEC_OPTIONS.find(o => EncoderType[o.value] === codec)?.short ?? codec;

export interface RenditionView { width: number; height: number; bitrateKbps: number; codec: string; }
export interface ProfileView { profileId: string; name: string; renditions: RenditionView[]; }

export interface CreateRendition { width: number; height: number; bitrateKbps: number; codec: EncoderType; }
export interface CreateProfileRequest { name: string; renditions: CreateRendition[]; }

@Injectable({ providedIn: 'root' })
export class ProfilesApi {
  private readonly http = inject(HttpClient);
  getAll() { return this.http.get<ProfileView[]>('/api/profiles'); }
  create(body: CreateProfileRequest) { return this.http.post<{ profileId: string }>('/api/profiles', body); }
}
