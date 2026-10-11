import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { formatElapsed } from '../../../../shared/time/format-elapsed';
import {
  ParkingSpot,
  ParkingSpotStatus,
  STATUS_LABELS,
  canCheckIn,
  canCheckOut,
} from '../../domain/parking.models';

const STATUS_MODIFIERS: Readonly<Record<ParkingSpotStatus, string>> = {
  Available: 'available',
  Occupied: 'occupied',
  OutOfService: 'out-of-service',
};

@Component({
  selector: 'app-spot-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatButtonModule],
  templateUrl: './spot-card.html',
  styleUrl: './spot-card.scss',
  host: { '[class]': 'hostClass()' },
})
export class SpotCard {
  readonly spot = input.required<ParkingSpot>();
  readonly now = input.required<number>();
  readonly busy = input(false);
  readonly checkIn = output<ParkingSpot>();
  readonly checkOut = output<ParkingSpot>();

  protected readonly hostClass = computed(
    () => `spot-card spot-card--${STATUS_MODIFIERS[this.spot().status]}`,
  );
  protected readonly statusLabel = computed(() => STATUS_LABELS[this.spot().status]);
  protected readonly assignment = computed(() => this.spot().activeAssignment);
  protected readonly elapsed = computed(() => {
    const assignment = this.assignment();
    return assignment ? formatElapsed(assignment.checkedInAt, this.now()) : null;
  });
  protected readonly canCheckIn = computed(() => canCheckIn(this.spot()));
  protected readonly canCheckOut = computed(() => canCheckOut(this.spot()));
}
