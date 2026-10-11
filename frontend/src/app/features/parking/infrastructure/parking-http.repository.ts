import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-base-url';
import { ActiveAssignment, CheckInRequest, ParkingSpot } from '../domain/parking.models';
import { ParkingRepository } from '../domain/parking.repository';
import { ActiveAssignmentDto, CheckInResponseDto, ParkingSpotDto } from './parking-api.dto';
import { toActiveAssignment, toParkingSpot } from './parking.mapper';

@Injectable()
export class ParkingHttpRepository extends ParkingRepository {
  readonly #http = inject(HttpClient);
  readonly #baseUrl = inject(API_BASE_URL);

  getSpots(): Observable<ParkingSpot[]> {
    return this.#http
      .get<ParkingSpotDto[]>(`${this.#baseUrl}/api/parking-spots`)
      .pipe(map((spots) => spots.map(toParkingSpot)));
  }

  getActiveAssignments(): Observable<ActiveAssignment[]> {
    return this.#http
      .get<ActiveAssignmentDto[]>(`${this.#baseUrl}/api/parking-assignments/active`)
      .pipe(map((assignments) => assignments.map(toActiveAssignment)));
  }

  checkIn(request: CheckInRequest): Observable<ActiveAssignment> {
    return this.#http
      .post<CheckInResponseDto>(`${this.#baseUrl}/api/parking-assignments`, request)
      .pipe(map(toActiveAssignment));
  }

  checkOut(assignmentId: string): Observable<void> {
    // Cuerpo vacío: la hora de salida la pone el servidor.
    return this.#http
      .put<unknown>(
        `${this.#baseUrl}/api/parking-assignments/${encodeURIComponent(assignmentId)}/checkout`,
        {},
      )
      .pipe(map(() => undefined));
  }
}
