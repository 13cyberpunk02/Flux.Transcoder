/**
 * Адреса внешних сервисов — в одном месте.
 * API и архивный плейлист идут относительными путями через dev-proxy (/api),
 * live-HLS отдаёт nginx напрямую.
 */
export const HLS_BASE_URL = 'http://localhost:3088';

export const livePlaylistUrl = (channelId: string) =>
  `${HLS_BASE_URL}/${channelId}/live/live.m3u8`;

export const archivePlaylistUrl = (channelId: string, fromIso: string) =>
  `/api/channels/${channelId}/archive?from=${encodeURIComponent(fromIso)}`;
