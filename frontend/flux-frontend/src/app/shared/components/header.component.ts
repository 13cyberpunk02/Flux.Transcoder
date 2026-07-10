import {Component, inject, signal, HostListener} from '@angular/core';
import {RouterLink, RouterLinkActive, Router} from '@angular/router';
import {FontAwesomeModule} from '@fortawesome/angular-fontawesome';
import {
  faTowerBroadcast, faLayerGroup, faSliders, faSun, faMoon,
  faRightFromBracket, faUser, faChevronDown,
} from '@fortawesome/free-solid-svg-icons';

import {IconButtonComponent} from '../../shared/ui/icon-button.component';
import {ButtonComponent} from '../../shared/ui/button.component';
import {ChannelsStore} from '../../features/channels/channels.store';
import {ThemeService} from '../../core/theme/theme.service';
import {TokenStore} from '../../core/auth/token.store';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, FontAwesomeModule, IconButtonComponent, ButtonComponent],
  template: `
    <header class="glass sticky top-0 z-50 border-x-0 border-t-0 rounded-none">
      <div class="mx-auto max-w-6xl px-4 h-16 flex items-center justify-between gap-4">

        <!-- ЛОГО -->
        <a routerLink="/channels" class="flex items-center gap-2.5 group">
          <div class="flex items-center justify-center w-9 h-9 rounded-xl
                      bg-[rgb(var(--accent)/0.2)] text-[rgb(var(--accent))]
                      group-hover:bg-[rgb(var(--accent)/0.3)] transition">
            <fa-icon [icon]="faTowerBroadcast"/>
          </div>
          <span class="font-semibold tracking-tight text-[rgb(var(--text))]">
            Flux<span class="text-[rgb(var(--accent))]">Transcoder</span>
          </span>
        </a>

        <!-- НАВИГАЦИЯ -->
        <nav class="hidden md:flex items-center gap-1">
          <a routerLink="/channels" routerLinkActive="nav-active" class="nav-link">
            <fa-icon [icon]="faLayerGroup" class="text-sm"/>
            <span>Каналы</span>
          </a>
          <a routerLink="/profiles" routerLinkActive="nav-active" class="nav-link">
            <fa-icon [icon]="faSliders" class="text-sm"/>
            <span>Профили</span>
          </a>
        </nav>

        <!-- ПРАВАЯ ЧАСТЬ -->
        <div class="flex items-center gap-2">

          <!-- счётчик в эфире -->
          <div class="hidden sm:flex items-center gap-2 px-3 py-1.5 rounded-xl
                      bg-[rgb(var(--surface)/var(--surface-opacity))]
                      border border-[rgb(var(--border)/var(--border-opacity))]">
            <span class="relative flex h-2 w-2">
              @if (store.runningCount() > 0) {
                <span class="animate-ping absolute inline-flex h-full w-full rounded-full
                             bg-emerald-400 opacity-75"></span>
              }
              <span class="relative inline-flex rounded-full h-2 w-2"
                    [class]="store.runningCount() > 0 ? 'bg-emerald-400' : 'bg-slate-400'"></span>
            </span>
            <span class="text-sm text-[rgb(var(--text-muted))]">
              в эфире <span class="font-semibold text-[rgb(var(--text))]">{{ store.runningCount() }}</span>
            </span>
          </div>

          <!-- переключатель темы -->
          <ui-icon-button
            [icon]="theme.theme() === 'dark' ? faSun : faMoon"
            variant="surface"
            [title]="theme.theme() === 'dark' ? 'Светлая тема' : 'Тёмная тема'"
            (clicked)="theme.toggle()"/>

          <!-- меню пользователя -->
          <div class="relative user-menu-wrapper">
            <button (click)="toggleMenu($event)"
                    class="flex items-center gap-2 pl-2 pr-3 py-1.5 rounded-xl
                     bg-[rgb(var(--surface)/var(--surface-opacity))]
                     border border-[rgb(var(--border)/var(--border-opacity))]
                     hover:border-[rgb(var(--accent)/0.4)] transition">
              <div class="flex items-center justify-center w-6 h-6 rounded-lg
                          bg-[rgb(var(--accent)/0.2)] text-[rgb(var(--accent))]">
                <fa-icon [icon]="faUser" class="text-xs"/>
              </div>
              <fa-icon [icon]="faChevronDown"
                       class="text-xs text-[rgb(var(--text-muted))] transition"
                       [class.rotate-180]="menuOpen()"/>
            </button>

            @if (menuOpen()) {
              <div class="absolute right-0 mt-2 w-44 rounded-xl p-1.5 shadow-xl z-[60]
            border border-[rgb(var(--border)/var(--border-opacity))]
            bg-[rgb(var(--bg))] backdrop-blur-xl
            animate-[fadeIn_0.15s_ease-out]">
                <ui-button
                  class="block"
                  label="Выйти" [icon]="faRightFromBracket"
                  variant="danger" size="sm" [full]="true"
                  (clicked)="logout()"/>
              </div>
            }
          </div>
        </div>
      </div>

      <nav class="md:hidden flex items-center gap-1 px-4 pb-3">
        <a routerLink="/channels" routerLinkActive="nav-active" class="nav-link flex-1 justify-center">
          <fa-icon [icon]="faLayerGroup" class="text-sm"/>
          <span>Каналы</span>
        </a>
        <a routerLink="/profiles" routerLinkActive="nav-active" class="nav-link flex-1 justify-center">
          <fa-icon [icon]="faSliders" class="text-sm"/>
          <span>Профили</span>
        </a>
      </nav>
    </header>
  `,
  styles: [`
    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(-4px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .nav-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 0.875rem;
      border-radius: 0.75rem;
      font-size: 0.875rem;
      color: rgb(var(--text-muted));
      transition: all 0.15s ease;
    }

    .nav-link:hover {
      color: rgb(var(--text));
      background: rgb(var(--surface) / var(--surface-opacity));
    }
  `],
})
export class HeaderComponent {
  readonly store = inject(ChannelsStore);
  readonly theme = inject(ThemeService);
  private readonly tokens = inject(TokenStore);
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);

  readonly faTowerBroadcast = faTowerBroadcast;
  readonly faLayerGroup = faLayerGroup;
  readonly faSliders = faSliders;
  readonly faSun = faSun;
  readonly faMoon = faMoon;
  readonly faRightFromBracket = faRightFromBracket;
  readonly faUser = faUser;
  readonly faChevronDown = faChevronDown;

  toggleMenu(e: MouseEvent): void {
    e.stopPropagation();
    this.menuOpen.update(v => !v);
  }

  @HostListener('document:click')
  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.tokens.clear();
    this.menuOpen.set(false);
    this.router.navigate(['/login']);
  }
}
