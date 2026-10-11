import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { formatElapsed } from '../../../../shared/time/format-elapsed';
import { ActiveAssignment } from '../../domain/parking.models';

@Component({
  selector: 'app-active-vehicles-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatTableModule],
  templateUrl: './active-vehicles-table.html',
  styles: `
    .active-table {
      width: 100%;
      font-variant-numeric: tabular-nums;
    }

    .active-table__empty {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class ActiveVehiclesTable {
  readonly assignments = input.required<readonly ActiveAssignment[]>();
  readonly now = input.required<number>();

  protected readonly columns = [
    'spotCode',
    'licensePlate',
    'visitorName',
    'destinationUnit',
    'checkedInAt',
  ];
  protected readonly rows = computed(() =>
    this.assignments().map((assignment) => ({
      ...assignment,
      elapsed: formatElapsed(assignment.checkedInAt, this.now()),
    })),
  );
}
