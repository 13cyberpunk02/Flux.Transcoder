import { Component, ElementRef, input, viewChild, effect, OnDestroy } from '@angular/core';
import Hls from 'hls.js';

@Component({
  selector: 'app-player',
  standalone: true,
  template: `<video #video controls autoplay style="width:100%"></video>`
})
export class PlayerComponent implements OnDestroy {
  readonly src = input.required<string>();
  private readonly videoRef = viewChild.required<ElementRef<HTMLVideoElement>>('video');
  private hls?: Hls;

  constructor() {
    effect(() => {
      const url = this.src();
      const video = this.videoRef().nativeElement;

      this.hls?.destroy();

      if (Hls.isSupported()) {
        this.hls = new Hls({
          liveSyncDurationCount: 3,
          backBufferLength: Infinity,
        });
        this.hls.loadSource(url);
        this.hls.attachMedia(video);
      } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = url;
      }
    });
  }

  ngOnDestroy(): void { this.hls?.destroy(); }
}
