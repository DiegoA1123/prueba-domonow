import { ActiveAssignment, ParkingSpot } from '../domain/parking.models';
import { ActiveAssignmentDto, ParkingSpotDto } from './parking-api.dto';

export function toActiveAssignment(dto: ActiveAssignmentDto): ActiveAssignment {
  return {
    assignmentId: dto.assignmentId,
    parkingSpotId: dto.parkingSpotId,
    spotCode: dto.spotCode,
    licensePlate: dto.licensePlate,
    visitorName: dto.visitorName,
    destinationUnit: dto.destinationUnit,
    checkedInAt: new Date(dto.checkedInAt),
  };
}

export function toParkingSpot(dto: ParkingSpotDto): ParkingSpot {
  return {
    id: dto.id,
    code: dto.code,
    status: dto.status,
    activeAssignment: dto.activeAssignment ? toActiveAssignment(dto.activeAssignment) : null,
  };
}
