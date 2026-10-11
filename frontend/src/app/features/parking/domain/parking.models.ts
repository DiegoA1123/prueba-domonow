export type ParkingSpotStatus = 'Available' | 'Occupied' | 'OutOfService';
export type SpotFilter = 'All' | ParkingSpotStatus;
export type SpotCounts = Readonly<Record<SpotFilter, number>>;

export interface ActiveAssignment {
  readonly assignmentId: string;
  readonly parkingSpotId: string;
  readonly spotCode: string;
  readonly licensePlate: string;
  readonly visitorName: string;
  readonly destinationUnit: string;
  readonly checkedInAt: Date;
}

export interface ParkingSpot {
  readonly id: string;
  readonly code: string;
  readonly status: ParkingSpotStatus;
  readonly activeAssignment: ActiveAssignment | null;
}

export interface CheckInRequest {
  readonly parkingSpotId: string;
  readonly licensePlate: string;
  readonly visitorName: string;
  readonly destinationUnit: string;
}

export const STATUS_LABELS: Readonly<Record<ParkingSpotStatus, string>> = {
  Available: 'Disponible',
  Occupied: 'Ocupado',
  OutOfService: 'Fuera de servicio',
};

/** Reglas de presentación: la autoridad sigue siendo el agregado del backend. */
export function canCheckIn(spot: ParkingSpot): boolean {
  return spot.status === 'Available';
}

export function canCheckOut(spot: ParkingSpot): boolean {
  return spot.status === 'Occupied' && spot.activeAssignment !== null;
}
