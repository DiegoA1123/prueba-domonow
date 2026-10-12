import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ActiveAssignment, ParkingSpot } from '../../domain/parking.models';
import { ParkingRepository } from '../../domain/parking.repository';
import { ParkingBoardPage } from './parking-board-page';

const available: ParkingSpot = {
  id: 'p01',
  code: 'P01',
  status: 'Available',
  activeAssignment: null,
};

class FakeParkingRepository extends ParkingRepository {
  spotsResponse: () => Observable<ParkingSpot[]> = () => of([available]);

  getSpots(): Observable<ParkingSpot[]> {
    return this.spotsResponse();
  }

  getActiveAssignments(): Observable<ActiveAssignment[]> {
    return of([]);
  }

  checkIn(): Observable<ActiveAssignment> {
    return throwError(() => new Error('not used'));
  }

  checkOut(): Observable<void> {
    return of(undefined);
  }
}

describe('ParkingBoardPage', () => {
  let repository: FakeParkingRepository;

  beforeEach(() => {
    vi.useFakeTimers();
    repository = new FakeParkingRepository();
    TestBed.configureTestingModule({
      imports: [ParkingBoardPage],
      providers: [{ provide: ParkingRepository, useValue: repository }],
    });
  });

  afterEach(() => vi.useRealTimers());

  it('closes its dialogs when the page is destroyed (e.g. a 401 sends the user to login)', async () => {
    const fixture = TestBed.createComponent(ParkingBoardPage);
    await vi.advanceTimersByTimeAsync(0);
    const dialog = TestBed.inject(MatDialog);

    void fixture.componentInstance['openCheckIn'](available);
    expect(dialog.openDialogs.length).toBe(1);

    fixture.destroy();
    await vi.advanceTimersByTimeAsync(1000);

    expect(dialog.openDialogs.length).toBe(0);
  });

  function buttonLabels(fixture: { nativeElement: unknown }): string[] {
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).map(
      (button) => button.textContent?.trim() ?? '',
    );
  }

  it('has no manual refresh button: the board refreshes by itself', async () => {
    const fixture = TestBed.createComponent(ParkingBoardPage);
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();

    expect(buttonLabels(fixture)).not.toContain('Actualizar');
  });

  it('still offers to retry when the board cannot be loaded', async () => {
    repository.spotsResponse = () => throwError(() => new HttpErrorResponse({ status: 0 }));
    const fixture = TestBed.createComponent(ParkingBoardPage);
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();

    expect(buttonLabels(fixture)).toContain('Reintentar');
  });

  it('filters the cards as the operator types and says when nothing matches', async () => {
    const fixture = TestBed.createComponent(ParkingBoardPage);
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const search = element.querySelector<HTMLInputElement>('input[type="search"]');
    if (!search) {
      throw new Error('search input not rendered');
    }

    search.value = 'p99';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(element.querySelectorAll('app-spot-card')).toHaveLength(0);
    expect(element.textContent).toContain('Ningún parqueadero coincide con «p99»');
  });

  it('does not claim the property is empty when the first load fails', async () => {
    repository.spotsResponse = () => throwError(() => new HttpErrorResponse({ status: 0 }));
    const fixture = TestBed.createComponent(ParkingBoardPage);
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('No se pudo conectar');
    expect(text).not.toContain('No hay vehículos de visitantes dentro');
    expect(text).not.toContain('No hay parqueaderos con este estado');
  });
});
