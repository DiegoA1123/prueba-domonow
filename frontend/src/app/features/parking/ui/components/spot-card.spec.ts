import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ParkingSpot } from '../../domain/parking.models';
import { SpotCard } from './spot-card';

const NOW = new Date('2026-10-11T17:05:00Z').getTime();
const occupied: ParkingSpot = {
  id: 's-2',
  code: 'P02',
  status: 'Occupied',
  activeAssignment: {
    assignmentId: 'a-1',
    parkingSpotId: 's-2',
    spotCode: 'P02',
    licensePlate: 'ABC123',
    visitorName: 'Carlos Pérez',
    destinationUnit: '302',
    checkedInAt: new Date('2026-10-11T15:00:00Z'),
  },
};
const available: ParkingSpot = {
  id: 's-1',
  code: 'P01',
  status: 'Available',
  activeAssignment: null,
};
const outOfService: ParkingSpot = {
  id: 's-4',
  code: 'P04',
  status: 'OutOfService',
  activeAssignment: null,
};

describe('SpotCard', () => {
  async function render(spot: ParkingSpot): Promise<ComponentFixture<SpotCard>> {
    const fixture = TestBed.createComponent(SpotCard);
    fixture.componentRef.setInput('spot', spot);
    fixture.componentRef.setInput('now', NOW);
    await fixture.whenStable();
    return fixture;
  }

  it('shows plate, visitor, unit and elapsed time of an occupied spot', async () => {
    const text = (await render(occupied)).nativeElement.textContent as string;

    expect(text).toContain('ABC123');
    expect(text).toContain('Carlos Pérez');
    expect(text).toContain('302');
    expect(text).toContain('hace 2 h 5 min');
  });

  it('labels the destination as apartment or house, not as the residential unit', async () => {
    // En Colombia "unidad" es el conjunto completo (unidad residencial); el portero lo leería mal.
    const text = (await render(occupied)).nativeElement.textContent as string;

    expect(text).toContain('Apto/casa');
    expect(text).not.toContain('Unidad');
  });

  it('emits checkIn when registering an entry on an available spot', async () => {
    const fixture = await render(available);
    const emitted = vi.fn();
    fixture.componentInstance.checkIn.subscribe(emitted);

    (fixture.nativeElement as HTMLElement).querySelector('button')?.click();

    expect(emitted).toHaveBeenCalledWith(available);
  });

  it('disables the action while the spot is busy', async () => {
    const fixture = await render(occupied);
    fixture.componentRef.setInput('busy', true);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('button')?.disabled).toBe(true);
  });

  it('offers no action on an out-of-service spot', async () => {
    expect((await render(outOfService)).nativeElement.querySelector('button')).toBeNull();
  });
});
