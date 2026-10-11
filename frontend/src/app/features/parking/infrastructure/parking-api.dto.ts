import { ParkingSpotStatus } from '../domain/parking.models';

export interface ActiveAssignmentDto {
  readonly assignmentId: string;
  readonly parkingSpotId: string;
  readonly spotCode: string;
  readonly licensePlate: string;
  readonly visitorName: string;
  readonly destinationUnit: string;
  readonly checkedInAt: string;
}

export interface ParkingSpotDto {
  readonly id: string;
  readonly code: string;
  readonly status: ParkingSpotStatus;
  readonly isAvailable: boolean;
  readonly activeAssignment: ActiveAssignmentDto | null;
}

/** La respuesta del check-in tiene la misma forma que una asignación activa. */
export type CheckInResponseDto = ActiveAssignmentDto;
