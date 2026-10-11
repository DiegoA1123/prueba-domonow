import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { SpotCounts, SpotFilter } from '../../domain/parking.models';

const FILTER_OPTIONS: readonly { readonly value: SpotFilter; readonly label: string }[] = [
  { value: 'All', label: 'Todos' },
  { value: 'Available', label: 'Disponibles' },
  { value: 'Occupied', label: 'Ocupados' },
  { value: 'OutOfService', label: 'Fuera de servicio' },
];

@Component({
  selector: 'app-status-filter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonToggleModule],
  template: `
    <mat-button-toggle-group
      [value]="selected()"
      (change)="selected.set($event.value)"
      hideSingleSelectionIndicator
      aria-label="Filtrar parqueaderos por estado"
    >
      @for (option of options; track option.value) {
        <mat-button-toggle [value]="option.value"
          >{{ option.label }} ({{ counts()[option.value] }})</mat-button-toggle
        >
      }
    </mat-button-toggle-group>
  `,
  styles: `
    mat-button-toggle-group {
      flex-wrap: wrap;
    }
  `,
})
export class StatusFilter {
  readonly counts = input.required<SpotCounts>();
  readonly selected = model<SpotFilter>('All');
  protected readonly options = FILTER_OPTIONS;
}
