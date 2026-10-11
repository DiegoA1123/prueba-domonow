import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { vi } from 'vitest';
import { ParkingStore } from '../../application/parking.store';
import { ParkingSpot } from '../../domain/parking.models';
import { CheckInDialog } from './check-in-dialog';

const spot: ParkingSpot = { id: 's-1', code: 'P01', status: 'Available', activeAssignment: null };

describe('CheckInDialog', () => {
  const checkIn = vi.fn();
  const close = vi.fn();
  let fixture: ComponentFixture<CheckInDialog>;
  let element: HTMLElement;

  beforeEach(async () => {
    checkIn.mockReset();
    close.mockReset();
    TestBed.configureTestingModule({
      imports: [CheckInDialog],
      providers: [
        { provide: ParkingStore, useValue: { checkIn } },
        { provide: MatDialogRef, useValue: { close } },
        { provide: MAT_DIALOG_DATA, useValue: { spot } },
      ],
    });
    fixture = TestBed.createComponent(CheckInDialog);
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  async function fill(plate: string, visitor: string, unit: string): Promise<void> {
    const [plateInput, visitorInput, unitInput] = Array.from(element.querySelectorAll('input'));
    const entries: readonly [HTMLInputElement, string][] = [
      [plateInput, plate],
      [visitorInput, visitor],
      [unitInput, unit],
    ];
    for (const [input, value] of entries) {
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    await fixture.whenStable();
  }

  async function submitForm(): Promise<void> {
    element.querySelector('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('does not send anything while the form is invalid', async () => {
    await fill('12-AB', '', '');
    await submitForm();

    expect(checkIn).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Formato válido: ABC123');
  });

  it('previews the normalized plate while typing', async () => {
    await fill(' abc-123 ', '', '');

    expect(element.textContent).toContain('Se registrará como ABC123');
  });

  it('registers the entry and closes the dialog on success', async () => {
    checkIn.mockResolvedValue({ ok: true });
    await fill('abc-123', '  Carlos Pérez ', '302');

    await submitForm();

    expect(checkIn).toHaveBeenCalledWith({
      parkingSpotId: 's-1',
      licensePlate: 'abc-123',
      visitorName: 'Carlos Pérez',
      destinationUnit: '302',
    });
    expect(close).toHaveBeenCalledWith(true);
  });

  it('stays open and shows the backend message when the entry is rejected', async () => {
    checkIn.mockResolvedValue({
      ok: false,
      error: {
        status: 409,
        code: 'Parking.VehicleAlreadyInside',
        message: 'El vehículo ABC123 ya se encuentra dentro de la propiedad.',
      },
    });
    await fill('abc-123', 'Carlos Pérez', '302');

    await submitForm();

    expect(close).not.toHaveBeenCalled();
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'ya se encuentra dentro',
    );
  });
});
