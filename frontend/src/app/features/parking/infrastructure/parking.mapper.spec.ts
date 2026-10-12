import { toActiveAssignment, toParkingSpot } from './parking.mapper';

describe('parking mapper', () => {
  const assignmentDto = {
    assignmentId: 'a-1',
    parkingSpotId: 's-2',
    spotCode: 'P02',
    licensePlate: 'ABC123',
    visitorName: 'Carlos Pérez',
    destinationUnit: '302',
    checkedInAt: '2026-10-11T15:00:00Z',
  };

  it('converts the check-in time to a Date', () => {
    expect(toActiveAssignment(assignmentDto).checkedInAt).toEqual(new Date('2026-10-11T15:00:00Z'));
  });

  it('maps an occupied spot with its active assignment', () => {
    const spot = toParkingSpot({
      id: 's-2',
      code: 'P02',
      status: 'Occupied',
      isAvailable: false,
      activeAssignment: assignmentDto,
    });

    expect(spot).toEqual({
      id: 's-2',
      code: 'P02',
      status: 'Occupied',
      activeAssignment: toActiveAssignment(assignmentDto),
    });
  });

  it('maps a spot without assignment to null', () => {
    const spot = toParkingSpot({
      id: 's-1',
      code: 'P01',
      status: 'Available',
      isAvailable: true,
      activeAssignment: null,
    });

    expect(spot.activeAssignment).toBeNull();
  });
});
