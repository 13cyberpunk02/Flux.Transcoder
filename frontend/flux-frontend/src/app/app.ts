import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class App {
  // создаём сервис темы при старте — он применяет app-dark и color-scheme
  private readonly theme = inject(ThemeService);
}
