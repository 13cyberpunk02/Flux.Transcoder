export interface ChannelView {
  channelId: string;
  sourceUrl: string;
  profileId: string;
  status: 'Idle' | 'Starting' | 'Running' | 'Degraded' | 'Failed' | 'Stopped';
  retentionMinutes: number;
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
