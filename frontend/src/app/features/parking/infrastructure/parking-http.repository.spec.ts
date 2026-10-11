import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL } from '../../../core/config/api-base-url';
import { ParkingHttpRepository } from './parking-http.repository';

describe('ParkingHttpRepository', () => {
  let repository: ParkingHttpRepository;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ParkingHttpRepository,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'http://api.test' },
      ],
    });
    repository = TestBed.inject(ParkingHttpRepository);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('posts the check-in contract expected by the API', async () => {
    const request = {
      parkingSpotId: 's-1',
      licensePlate: 'abc-123',
      visitorName: 'Ana',
      destinationUnit: '101',
    };
    const result = firstValueFrom(repository.checkIn(request));

    const call = controller.expectOne('http://api.test/api/parking-assignments');
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(request);
    call.flush({
      assignmentId: 'a-1',
      parkingSpotId: 's-1',
      spotCode: 'P01',
      licensePlate: 'ABC123',
      visitorName: 'Ana',
      destinationUnit: '101',
      checkedInAt: '2026-10-11T15:00:00Z',
    });

    expect((await result).licensePlate).toBe('ABC123');
  });

  it('checks out with PUT and an empty JSON body so the server sets the time', async () => {
    const result = firstValueFrom(repository.checkOut('a-1'));

    const call = controller.expectOne('http://api.test/api/parking-assignments/a-1/checkout');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({});
    call.flush({});

    await expect(result).resolves.toBeUndefined();
  });
});
