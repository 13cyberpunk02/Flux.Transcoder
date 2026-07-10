import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { SignalRService } from '../../core/realtime/signalr.service';
import {HeaderComponent} from '../../shared/components/header.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <div class="min-h-screen flex flex-col">
      <app-header />
      <main class="flex-1 mx-auto w-full max-w-6xl px-4 py-6">
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellComponent implements OnInit {
  private readonly signalr = inject(SignalRService);

  ngOnInit(): void {
    this.signalr.start();   // real-time стартует после входа
  }
}
