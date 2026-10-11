import { Routes } from '@angular/router';
import { guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Ingresar · DomoNow',
    loadComponent: () => import('./core/auth/login-page').then((m) => m.LoginPage),
  },
  { path: '**', redirectTo: 'login' },
];
