import { Pipe, PipeTransform } from '@angular/core';
import { formatBitrate, formatDuration, formatRetention } from './format';

@Pipe({ name: 'bitrate' })
export class BitratePipe implements PipeTransform {
  transform(kbps: number | null | undefined): string {
    return formatBitrate(kbps);
  }
}

@Pipe({ name: 'duration' })
export class DurationPipe implements PipeTransform {
  transform(seconds: number | null | undefined): string {
    return formatDuration(seconds);
  }
}

@Pipe({ name: 'retention' })
export class RetentionPipe implements PipeTransform {
  transform(minutes: number | null | undefined): string {
    return formatRetention(minutes);
  }
}
