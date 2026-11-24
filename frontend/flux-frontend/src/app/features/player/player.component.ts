import {
  Component, ElementRef, effect, inject, input, OnDestroy, output, signal, untracked, viewChild,
} from '@angular/core';
import Hls from 'hls.js';
import { TokenStore } from '../../core/auth/token.store';

const LIVE_RETRY_MS = 2000;

/**
 * HLS-плеер (live и DVR-архив).
 * backBufferLength ограничен: перемотку назад даёт плейлист, а не буфер в памяти —
 * с Infinity многочасовой просмотр упирается в QuotaExceeded.
 *
 * В live отсутствие плейлиста — не ошибка: сразу после старта канала его ещё нет,
 * а hls.js на 4xx сам не повторяет. Поэтому пробуем заново, пока поток не появится.
 */
@Component({
  selector: 'app-player',
  template: `
    <!-- [muted] свойством: атрибут muted у созданного скриптом video не действует, и autoplay блокируется -->
    <video #video controls autoplay playsinline [muted]="true" class="w-full h-full bg-black"></video>
    @if (waiting()) {
      <div role="status"
           class="absolute inset-0 flex flex-col items-center justify-center gap-3 text-white/80 pointer-events-none">
        <span class="material-symbols-outlined animate-spin text-4xl" aria-hidden="true">progress_activity</span>
        Ждём поток…
      </div>
    }
  `,
  host: { class: 'relative block aspect-video bg-black' },
})
export class PlayerComponent implements OnDestroy {
  readonly src = input.required<string>();
  /** Прямой эфир: ждём появления потока вместо ошибки. */
  readonly live = input(false);
  /** Архив не удалось загрузить (например, записи с этого момента нет). */
  readonly failed = output<string>();

  protected readonly waiting = signal(false);

  private readonly tokens = inject(TokenStore);

  private readonly videoRef = viewChild.required<ElementRef<HTMLVideoElement>>('video');
  private hls?: Hls;
  private retryTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      const url = this.src();
      const live = this.live();
      const video = this.videoRef().nativeElement;
      untracked(() => {
        this.waiting.set(live);
        this.load(url, live, video);
      });
    });
  }

  private load(url: string, live: boolean, video: HTMLVideoElement): void {
    this.destroyHls();

    const retryLive = () => {
      this.waiting.set(true);
      clearTimeout(this.retryTimer);
      this.retryTimer = setTimeout(() => this.load(url, live, video), LIVE_RETRY_MS);
    };

    if (!Hls.isSupported()) {
      // Safari — нативный HLS
      if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.onloadeddata = () => this.waiting.set(false);
        video.onerror = () => (live ? retryLive() : this.failed.emit('native'));
        video.src = url;
      }
      return;
    }

    const hls = new Hls({
      liveSyncDurationCount: 3,
      backBufferLength: 180,
      // Архивный плейлист отдаёт API, он требует JWT. Заголовок только для своего origin:
      // на nginx (другой origin) он вызвал бы CORS preflight, который nginx не разрешает.
      xhrSetup: (xhr, requestUrl) => {
        const token = this.tokens.token();
        if (token && new URL(requestUrl, location.href).origin === location.origin)
          xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      },
    });
    hls.on(Hls.Events.FRAG_BUFFERED, () => this.waiting.set(false));
    hls.on(Hls.Events.ERROR, (_, data) => {
      if (!data.fatal) return;
      if (data.type === Hls.ErrorTypes.MEDIA_ERROR) {
        hls.recoverMediaError();
      } else if (live) {
        retryLive();
      } else if (data.type === Hls.ErrorTypes.NETWORK_ERROR && data.details !== Hls.ErrorDetails.MANIFEST_LOAD_ERROR) {
        // временный сбой сети — пробуем продолжить
        this.retryTimer = setTimeout(() => hls.startLoad(), 3000);
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
