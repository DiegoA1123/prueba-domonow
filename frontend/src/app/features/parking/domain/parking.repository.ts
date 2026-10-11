import { Observable } from 'rxjs';
import { ActiveAssignment, CheckInRequest, ParkingSpot } from './parking.models';

/** Puerto de salida del feature. La implementación HTTP se enlaza en los providers de la ruta. */
export abstract class ParkingRepository {
  abstract getSpots(): Observable<ParkingSpot[]>;
  abstract getActiveAssignments(): Observable<ActiveAssignment[]>;
  abstract checkIn(request: CheckInRequest): Observable<ActiveAssignment>;
  abstract checkOut(assignmentId: string): Observable<void>;
}
