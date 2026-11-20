export type ChannelStatus = 'Idle' | 'Starting' | 'Running' | 'Degraded' | 'Failed' | 'Stopped';

/** Статусы, в которых канал не транслируется: его можно запустить или удалить. */
export const INACTIVE_STATUSES: readonly ChannelStatus[] = ['Idle', 'Stopped', 'Failed'];
export const isInactive = (s: ChannelStatus) => INACTIVE_STATUSES.includes(s);
/** Рестарт разрешён доменом из этих статусов. */
export const canRestart = (s: ChannelStatus) =>
  s === 'Starting' || s === 'Running' || s === 'Degraded' || s === 'Failed';
export const canStop = (s: ChannelStatus) => s !== 'Idle' && s !== 'Stopped';

export interface ChannelView {
  channelId: string;
  sourceUrl: string;
  profileId: string;
  status: ChannelStatus;
  retentionMinutes: number;
}

export interface MetricPoint {
  time: string;
  avgBitrate: number;
  maxBitrate: number;
  droppedFrames: number;
  uptimeSeconds: number;
}

export interface ChannelHealthReport {
  channelId: string;
  status: string;
  inputBitrateKbps: number;
  outputBitrateKbps: number;
  droppedFrames: number;
  uptimeSeconds: number;
  reportedAt: string;
}

export interface CreateChannelRequest { sourceUrl: string; profileId: string; }
export interface LoginRequest { username: string; password: string; }
export interface LoginResponse { token: string; }
