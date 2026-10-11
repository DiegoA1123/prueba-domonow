import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Injector, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';
import { Notifier } from '../../../../core/notifications/notifier';
import { ConfirmDialog, ConfirmDialogData } from '../../../../shared/ui/confirm-dialog';
import { ParkingStore } from '../../application/parking.store';
import { ParkingSpot } from '../../domain/parking.models';
import { ActiveVehiclesTable } from '../components/active-vehicles-table';
import { SpotCard } from '../components/spot-card';
import { StatusFilter } from '../components/status-filter';
import { CheckInDialog, CheckInDialogData } from '../dialogs/check-in-dialog';

@Component({
  selector: 'app-parking-board-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkingStore],
  imports: [
    ActiveVehiclesTable,
    DatePipe,
    MatButtonModule,
    MatProgressBarModule,
    SpotCard,
    StatusFilter,
  ],
  templateUrl: './parking-board-page.html',
  styleUrl: './parking-board-page.scss',
})
export class ParkingBoardPage {
  protected readonly store = inject(ParkingStore);
  readonly #dialog = inject(MatDialog);
  readonly #injector = inject(Injector);
  readonly #notifier = inject(Notifier);

  protected async openCheckIn(spot: ParkingSpot): Promise<void> {
    // El injector de la página permite que el diálogo use el mismo ParkingStore.
    const dialogRef = this.#dialog.open<CheckInDialog, CheckInDialogData, boolean>(CheckInDialog, {
      data: { spot },
      injector: this.#injector,
    });
    if (await firstValueFrom(dialogRef.afterClosed())) {
      this.#notifier.success(`Ingreso registrado en ${spot.code}.`);
    }
  }

  protected async confirmCheckOut(spot: ParkingSpot): Promise<void> {
    const assignment = spot.activeAssignment;
    if (!assignment) {
      return;
    }
    const confirmed = await firstValueFrom(
      this.#dialog
        .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
          data: {
            title: `Registrar salida · ${spot.code}`,
            message: `¿Confirmar la salida del vehículo ${assignment.licensePlate}?`,
            confirmText: 'Registrar salida',
          },
        })
        .afterClosed(),
    );
    if (!confirmed) {
      return;
    }
    const result = await this.store.checkOut(spot.id, assignment.assignmentId);
    if (result.ok) {
      this.#notifier.success(`Salida registrada: ${assignment.licensePlate} liberó ${spot.code}.`);
    } else {
      this.#notifier.error(result.error.message);
    }
  }
}
