import { Component, ElementRef, input, viewChild, effect, OnDestroy } from '@angular/core';
import Hls from 'hls.js';

@Component({
  selector: 'app-player',
  standalone: true,
  template: `<video #video controls autoplay style="width:100%"></video>`
})
export class PlayerComponent implements OnDestroy {
  readonly channelId = input.required<string>();
  private readonly videoRef = viewChild.required<ElementRef<HTMLVideoElement>>('video');
  private hls?: Hls;

  // базовый URL Nginx-раздачи
  private readonly hlsBase = 'http://localhost:3088';

  constructor() {
    effect(() => {
      const id = this.channelId();
      const video = this.videoRef().nativeElement;
      const url = `${this.hlsBase}/${id}/live/live.m3u8`;

      this.hls?.destroy();

      if (Hls.isSupported()) {
        this.hls = new Hls();
        this.hls.loadSource(url);
        this.hls.attachMedia(video);
      } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = url;   // Safari
      }
    });
  }

  ngOnDestroy(): void { this.hls?.destroy(); }
}
