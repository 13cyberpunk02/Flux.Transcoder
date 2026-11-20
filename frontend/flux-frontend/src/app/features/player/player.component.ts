import { Component, ElementRef, effect, input, OnDestroy, output, viewChild } from '@angular/core';
import Hls from 'hls.js';

/**
 * HLS-плеер (live и DVR-архив).
 * backBufferLength ограничен: перемотку назад даёт плейлист, а не буфер в памяти —
 * с Infinity многочасовой просмотр упирается в QuotaExceeded.
 */
@Component({
  selector: 'app-player',
  template: `
    <video #video controls autoplay muted playsinline class="w-full h-full bg-black"></video>
  `,
  host: { class: 'block aspect-video bg-black' },
})
export class PlayerComponent implements OnDestroy {
  readonly src = input.required<string>();
  /** Плеер не смог восстановиться (например, плейлиста нет). */
  readonly failed = output<string>();

  private readonly videoRef = viewChild.required<ElementRef<HTMLVideoElement>>('video');
  private hls?: Hls;
  private retryTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => this.load(this.src(), this.videoRef().nativeElement));
  }

  private load(url: string, video: HTMLVideoElement): void {
    this.destroyHls();

    if (!Hls.isSupported()) {
      // Safari — нативный HLS
      if (video.canPlayType('application/vnd.apple.mpegurl')) video.src = url;
      return;
    }

    const hls = new Hls({
      liveSyncDurationCount: 3,
      backBufferLength: 180,
      manifestLoadingMaxRetry: 4,
    });
    hls.on(Hls.Events.ERROR, (_, data) => {
      if (!data.fatal) return;
      if (data.type === Hls.ErrorTypes.NETWORK_ERROR && data.details !== Hls.ErrorDetails.MANIFEST_LOAD_ERROR) {
        // временный сбой сети — пробуем продолжить
        this.retryTimer = setTimeout(() => hls.startLoad(), 3000);
      } else if (data.type === Hls.ErrorTypes.MEDIA_ERROR) {
        hls.recoverMediaError();
      } else {
        this.failed.emit(data.details);
      }
    });
    hls.loadSource(url);
    hls.attachMedia(video);
    this.hls = hls;
  }

  private destroyHls(): void {
    clearTimeout(this.retryTimer);
    this.hls?.destroy();
    this.hls = undefined;
  }

  ngOnDestroy(): void {
    this.destroyHls();
  }
}
