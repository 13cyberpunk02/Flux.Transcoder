import { Routes } from '@angular/router';
import {authGuard} from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/shell/shell.component').then(m => m.ShellComponent),
    children: [
      { path: '', redirectTo: 'channels', pathMatch: 'full' },
      {
        path: 'channels',
        loadComponent: () =>
          import('./features/channels/channel-list.component').then(m => m.ChannelListComponent),
      },
      {
        path: 'channels/:id',
        loadComponent: () =>
          import('./features/channels/channel-detail.component').then(m => m.ChannelDetailComponent),
      },
      {
        path: 'profiles',
        loadComponent: () =>
          import('./features/profiles/profile-list.component').then(m => m.ProfileListComponent),
      }
    ],
  },
  { path: '**', redirectTo: '' },
];
