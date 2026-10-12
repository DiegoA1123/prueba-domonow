import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class Notifier {
  readonly #snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.#snackBar.open(message, 'Cerrar', { duration: 4000 });
  }

  error(message: string): void {
    this.#snackBar.open(message, 'Cerrar', {
      duration: 6000,
      panelClass: 'notifier--error',
      politeness: 'assertive',
    });
  }
}
