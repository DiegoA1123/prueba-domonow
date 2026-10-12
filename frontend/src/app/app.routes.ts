import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { ParkingRepository } from './features/parking/domain/parking.repository';
import { ParkingHttpRepository } from './features/parking/infrastructure/parking-http.repository';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Ingresar · DomoNow',
    loadComponent: () => import('./core/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: '',
    pathMatch: 'full',
    canActivate: [authGuard],
    title: 'Parqueaderos de visitantes · DomoNow',
    // Hexagonal: aquí se enlaza el puerto del feature con su adaptador HTTP.
    providers: [{ provide: ParkingRepository, useClass: ParkingHttpRepository }],
    loadComponent: () =>
      import('./features/parking/ui/pages/parking-board-page').then((m) => m.ParkingBoardPage),
  },
  { path: '**', redirectTo: '' },
];
