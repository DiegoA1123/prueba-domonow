import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import {
  ActiveAssignment,
  CheckInRequest,
  ParkingSpot,
  ParkingSpotStatus,
} from '../domain/parking.models';
import { ParkingRepository } from '../domain/parking.repository';
import { LOAD_TIMEOUT_MS, POLL_INTERVAL_MS, ParkingStore } from './parking.store';

const assignment = (spotId: string, plate: string): ActiveAssignment => ({
  assignmentId: `a-${spotId}`,
  parkingSpotId: spotId,
  spotCode: spotId.toUpperCase(),
  licensePlate: plate,
  visitorName: 'Visitante',
  destinationUnit: '302',
  checkedInAt: new Date('2026-10-11T15:00:00Z'),
});

const spot = (id: string, status: ParkingSpotStatus): ParkingSpot => ({
  id,
  code: id.toUpperCase(),
  status,
  activeAssignment: status === 'Occupied' ? assignment(id, 'XYZ987') : null,
});

class FakeParkingRepository extends ParkingRepository {
  spots: ParkingSpot[] = [
    spot('p01', 'Available'),
    spot('p02', 'Occupied'),
    spot('p04', 'OutOfService'),
  ];
  spotsResponse: () => Observable<ParkingSpot[]> = () => of(this.spots);
  checkInResponse: Observable<ActiveAssignment> = of(assignment('p01', 'ABC123'));
  checkOutResponse: Observable<void> = of(undefined);
  getSpotsCalls = 0;
  checkInCalls = 0;
  checkedOutIds: string[] = [];

  getSpots(): Observable<ParkingSpot[]> {
    this.getSpotsCalls++;
    return this.spotsResponse();
  }

  getActiveAssignments(): Observable<ActiveAssignment[]> {
    return of(this.spots.flatMap((s) => (s.activeAssignment ? [s.activeAssignment] : [])));
  }

  checkIn(): Observable<ActiveAssignment> {
    this.checkInCalls++;
    return this.checkInResponse;
  }

  checkOut(assignmentId: string): Observable<void> {
    this.checkedOutIds.push(assignmentId);
    return this.checkOutResponse;
  }
}

describe('ParkingStore', () => {
  let repository: FakeParkingRepository;
  let store: ParkingStore;
  let visibility: DocumentVisibilityState;
  const request: CheckInRequest = {
    parkingSpotId: 'p01',
    licensePlate: 'abc-123',
    visitorName: 'Ana',
    destinationUnit: '101',
  };

  beforeEach(() => {
    vi.useFakeTimers();
    visibility = 'visible';
    Object.defineProperty(document, 'visibilityState', {
      configurable: true,
      get: () => visibility,
    });
    repository = new FakeParkingRepository();
    TestBed.configureTestingModule({
      providers: [ParkingStore, { provide: ParkingRepository, useValue: repository }],
    });
    store = TestBed.inject(ParkingStore);
  });

  afterEach(() => {
    vi.useRealTimers();
    Reflect.deleteProperty(document, 'visibilityState');
  });

  const setVisibility = (state: DocumentVisibilityState): void => {
    visibility = state;
    document.dispatchEvent(new Event('visibilitychange'));
  };

  it('loads spots and vehicles inside as soon as it starts', async () => {
    await vi.advanceTimersByTimeAsync(0);

    expect(store.spots()).toHaveLength(3);
    expect(store.activeAssignments().map((a) => a.licensePlate)).toEqual(['XYZ987']);
    expect(store.loadState()).toBe('ready');
    expect(store.lastUpdated()).not.toBeNull();
  });

  it('derives counts and the visible spots from the selected filter', async () => {
    await vi.advanceTimersByTimeAsync(0);

    store.filter.set('Occupied');

    expect(store.counts()).toEqual({ All: 3, Available: 1, Occupied: 1, OutOfService: 1 });
    expect(store.visibleSpots().map((s) => s.code)).toEqual(['P02']);
  });

  it('narrows the visible spots by search text within the selected filter', async () => {
    await vi.advanceTimersByTimeAsync(0);

    store.search.set('xyz');
    expect(store.visibleSpots().map((s) => s.code)).toEqual(['P02']);

    store.filter.set('Available');
    expect(store.visibleSpots()).toEqual([]);

    store.search.set('');
    expect(store.visibleSpots().map((s) => s.code)).toEqual(['P01']);
  });

  it('polls again every 30 seconds', async () => {
    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);

    expect(repository.getSpotsCalls).toBe(2);
  });

  it('pauses polling while the tab is hidden and reloads when it is visible again', async () => {
    await vi.advanceTimersByTimeAsync(0);
    setVisibility('hidden');
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS * 3);
    expect(repository.getSpotsCalls).toBe(1);

    setVisibility('visible');
    await vi.advanceTimersByTimeAsync(0);

    expect(repository.getSpotsCalls).toBe(2);
  });

  it('keeps showing the last data with an error when a refresh fails', async () => {
    await vi.advanceTimersByTimeAsync(0);
    repository.spotsResponse = () => throwError(() => new HttpErrorResponse({ status: 0 }));

    store.refresh();
    await vi.advanceTimersByTimeAsync(0);

    expect(store.spots()).toHaveLength(3);
    expect(store.loadState()).toBe('ready');
    expect(store.loadError()?.message).toContain('No se pudo conectar');
  });

  it('marks the spot busy during check-in and refreshes afterwards', async () => {
    await vi.advanceTimersByTimeAsync(0);
    const response = new Subject<ActiveAssignment>();
    repository.checkInResponse = response;

    const pending = store.checkIn(request);
    expect(store.busySpotIds().has('p01')).toBe(true);

    response.next(assignment('p01', 'ABC123'));
    response.complete();
    await expect(pending).resolves.toEqual({ ok: true });
    await vi.advanceTimersByTimeAsync(0);

    expect(store.busySpotIds().has('p01')).toBe(false);
    expect(repository.getSpotsCalls).toBe(2);
  });

  it('returns the backend error when check-in is rejected', async () => {
    repository.checkInResponse = throwError(
      () =>
        new HttpErrorResponse({
          status: 409,
          error: {
            detail: 'El vehículo ABC123 ya se encuentra dentro de la propiedad.',
            code: 'Parking.VehicleAlreadyInside',
          },
        }),
    );

    const result = await store.checkIn(request);

    expect(result).toEqual({
      ok: false,
      error: {
        status: 409,
        code: 'Parking.VehicleAlreadyInside',
        message: 'El vehículo ABC123 ya se encuentra dentro de la propiedad.',
      },
    });
    expect(store.busySpotIds().size).toBe(0);
  });

  it('refreshes the board after a rejected action so the operator sees the real state', async () => {
    await vi.advanceTimersByTimeAsync(0);
    repository.checkInResponse = throwError(
      () => new HttpErrorResponse({ status: 409, error: { code: 'Parking.SpotOccupied' } }),
    );

    await store.checkIn(request);
    await vi.advanceTimersByTimeAsync(0);

    expect(repository.getSpotsCalls).toBe(2);
  });

  it('checks out the given assignment and refreshes the board', async () => {
    await vi.advanceTimersByTimeAsync(0);

    const result = await store.checkOut('p02', 'a-p02');
    await vi.advanceTimersByTimeAsync(0);

    expect(result).toEqual({ ok: true });
    expect(repository.checkedOutIds).toEqual(['a-p02']);
    expect(repository.getSpotsCalls).toBe(2);
  });

  it('ignores a second action on the same spot while the first is still in flight', async () => {
    await vi.advanceTimersByTimeAsync(0);
    const response = new Subject<ActiveAssignment>();
    repository.checkInResponse = response;

    const first = store.checkIn(request);
    const second = await store.checkIn(request);

    expect(repository.checkInCalls).toBe(1);
    expect(second.ok).toBe(false);
    response.next(assignment('p01', 'ABC123'));
    response.complete();
    await expect(first).resolves.toEqual({ ok: true });
  });

  it('reports a hung API instead of waiting forever', async () => {
    repository.spotsResponse = () => NEVER;

    await vi.advanceTimersByTimeAsync(LOAD_TIMEOUT_MS);

    expect(store.loadState()).toBe('error');
    expect(store.loadError()?.message).toContain('tarda demasiado');
  });
});
