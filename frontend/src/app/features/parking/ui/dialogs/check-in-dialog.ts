import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  minLength,
  pattern,
  required,
  submit,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ParkingStore } from '../../application/parking.store';
import { isValidPlate, normalizePlate } from '../../domain/license-plate';
import { ParkingSpot } from '../../domain/parking.models';

export interface CheckInDialogData {
  readonly spot: ParkingSpot;
}

interface CheckInFormModel {
  licensePlate: string;
  visitorName: string;
  destinationUnit: string;
}

// Acepta lo que el backend normaliza: mayúsculas/minúsculas y un separador opcional (espacio, guion o punto).
const PLATE_INPUT = /^\s*[a-zA-Z]{3}[\s.-]?(\d{3}|\d{2}[a-zA-Z])\s*$/;

@Component({
  selector: 'app-check-in-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './check-in-dialog.html',
  styleUrl: './check-in-dialog.scss',
})
export class CheckInDialog {
  readonly #store = inject(ParkingStore);
  readonly #dialogRef = inject<MatDialogRef<CheckInDialog, boolean>>(MatDialogRef);
  protected readonly spot = inject<CheckInDialogData>(MAT_DIALOG_DATA).spot;

  protected readonly model = signal<CheckInFormModel>({
    licensePlate: '',
    visitorName: '',
    destinationUnit: '',
  });
  protected readonly checkInForm = form(this.model, (path) => {
    required(path.licensePlate, { message: 'La placa es obligatoria.' });
    pattern(path.licensePlate, PLATE_INPUT, {
      message: 'Formato válido: ABC123 (carro) o ABC12D (moto).',
    });
    required(path.visitorName, { message: 'El nombre del visitante es obligatorio.' });
    minLength(path.visitorName, 2, { message: 'Mínimo 2 caracteres.' });
    maxLength(path.visitorName, 100, { message: 'Máximo 100 caracteres.' });
    required(path.destinationUnit, { message: 'La unidad que visita es obligatoria.' });
    maxLength(path.destinationUnit, 20, { message: 'Máximo 20 caracteres.' });
  });

  protected readonly normalizedPlate = computed(() => {
    const raw = this.model().licensePlate;
    return isValidPlate(raw) ? normalizePlate(raw) : null;
  });
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.checkInForm, async () => {
      this.submitting.set(true);
      this.serverError.set(null);
      const { licensePlate, visitorName, destinationUnit } = this.model();
      const result = await this.#store.checkIn({
        parkingSpotId: this.spot.id,
        licensePlate,
        visitorName: visitorName.trim(),
        destinationUnit: destinationUnit.trim(),
      });
      this.submitting.set(false);
      if (result.ok) {
        this.#dialogRef.close(true);
      } else {
        this.serverError.set(result.error.message);
      }
      return undefined;
    });
  }
}
