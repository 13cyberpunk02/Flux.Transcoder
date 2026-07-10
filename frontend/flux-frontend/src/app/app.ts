import {Component, inject, signal, ChangeDetectionStrategy} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {ThemeService} from './core/theme/theme.service';
import {ToastContainerComponent} from './core/toast/toast-container.component';
import {ConfirmModalComponent} from './core/confirm/confirm-modal.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainerComponent, ConfirmModalComponent],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.css'
})
export class App {
  private readonly theme = inject(ThemeService);
}
