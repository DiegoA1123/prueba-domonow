import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  EMPTY,
  Observable,
  Subject,
  catchError,
  distinctUntilChanged,
  firstValueFrom,
  forkJoin,
  fromEvent,
  map,
  merge,
  of,
  startWith,
  switchMap,
  timer,
} from 'rxjs';
import { ApiError, toApiError } from '../../../core/http/api-error';
import {
  ActiveAssignment,
  CheckInRequest,
  ParkingSpot,
  ParkingSpotStatus,
  SpotCounts,
  SpotFilter,
} from '../domain/parking.models';
import { ParkingRepository } from '../domain/parking.repository';

export const POLL_INTERVAL_MS = 30_000;

export type ActionResult = { readonly ok: true } | { readonly ok: false; readonly error: ApiError };

type LoadState = 'loading' | 'ready' | 'error';
type Snapshot =
  | { readonly ok: true; readonly spots: ParkingSpot[]; readonly active: ActiveAssignment[] }
  | { readonly ok: false; readonly error: ApiError };

/**
 * Estado reactivo del tablero. Se provee a nivel de página: vive y hace polling solo mientras la
 * página existe. Las cargas son un flujo RxJS (polling visible + refrescos manuales) cuyo último
 * resultado se vuelca en signals.
 */
@Injectable()
export class ParkingStore {
  readonly #repository = inject(ParkingRepository);
  readonly #document = inject(DOCUMENT);
  readonly #refreshRequests = new Subject<void>();

  readonly #spots = signal<readonly ParkingSpot[]>([]);
  readonly #activeAssignments = signal<readonly ActiveAssignment[]>([]);
  readonly #loadState = signal<LoadState>('loading');
  readonly #loadError = signal<ApiError | null>(null);
  readonly #busySpotIds = signal<ReadonlySet<string>>(new Set());
  readonly #lastUpdated = signal<Date | null>(null);
  readonly #now = signal(Date.now());

  readonly filter = signal<SpotFilter>('All');
  readonly spots = this.#spots.asReadonly();
  readonly activeAssignments = this.#activeAssignments.asReadonly();
  readonly loadState = this.#loadState.asReadonly();
  readonly loadError = this.#loadError.asReadonly();
  readonly busySpotIds = this.#busySpotIds.asReadonly();
  readonly lastUpdated = this.#lastUpdated.asReadonly();
  readonly now = this.#now.asReadonly();

  readonly counts = computed<SpotCounts>(() => {
    const spots = this.#spots();
    const countOf = (status: ParkingSpotStatus): number =>
      spots.filter((spot) => spot.status === status).length;
    return {
      All: spots.length,
      Available: countOf('Available'),
      Occupied: countOf('Occupied'),
      OutOfService: countOf('OutOfService'),
    };
  });

  readonly visibleSpots = computed(() => {
    const filter = this.filter();
    const spots = this.#spots();
    return filter === 'All' ? spots : spots.filter((spot) => spot.status === filter);
  });

  readonly isInitialLoading = computed(
    () => this.#loadState() === 'loading' && this.#spots().length === 0,
  );

  constructor() {
    const isVisible = (): boolean => this.#document.visibilityState === 'visible';
    const polling$ = fromEvent(this.#document, 'visibilitychange').pipe(
      map(isVisible),
      startWith(isVisible()),
      distinctUntilChanged(),
      switchMap((visible) => (visible ? timer(0, POLL_INTERVAL_MS) : EMPTY)),
    );

    merge(polling$, this.#refreshRequests)
      .pipe(
        // switchMap: una carga nueva cancela la anterior, nunca se pinta una respuesta vieja.
        switchMap(() => this.#loadSnapshot()),
        takeUntilDestroyed(),
      )
      .subscribe((snapshot) => this.#apply(snapshot));
  }

  refresh(): void {
    this.#refreshRequests.next();
  }

  checkIn(request: CheckInRequest): Promise<ActionResult> {
    return this.#runForSpot(request.parkingSpotId, this.#repository.checkIn(request));
  }

  checkOut(spotId: string, assignmentId: string): Promise<ActionResult> {
    return this.#runForSpot(spotId, this.#repository.checkOut(assignmentId));
  }

  #loadSnapshot(): Observable<Snapshot> {
    return forkJoin({
      spots: this.#repository.getSpots(),
      active: this.#repository.getActiveAssignments(),
    }).pipe(
      map(({ spots, active }): Snapshot => ({ ok: true, spots, active })),
      catchError((error: unknown) => of<Snapshot>({ ok: false, error: toApiError(error) })),
    );
  }

  #apply(snapshot: Snapshot): void {
    this.#now.set(Date.now());
    if (snapshot.ok) {
      this.#spots.set(snapshot.spots);
      this.#activeAssignments.set(snapshot.active);
      this.#loadError.set(null);
      this.#loadState.set('ready');
      this.#lastUpdated.set(new Date());
      return;
    }
    // Se conserva el último tablero válido; solo se marca error si nunca hubo datos.
    this.#loadError.set(snapshot.error);
    this.#loadState.set(this.#spots().length === 0 ? 'error' : 'ready');
  }

  async #runForSpot(spotId: string, action$: Observable<unknown>): Promise<ActionResult> {
    this.#busySpotIds.update((ids) => new Set(ids).add(spotId));
    try {
      await firstValueFrom(action$, { defaultValue: undefined });
      return { ok: true };
    } catch (error) {
      return { ok: false, error: toApiError(error) };
    } finally {
      this.#busySpotIds.update((ids) => {
        const next = new Set(ids);
        next.delete(spotId);
        return next;
      });
      // Tras éxito o error (p. ej. 409 porque otro operador lo ocupó) se refresca el tablero.
      this.refresh();
    }
  }
}
