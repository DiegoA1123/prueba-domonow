import { ParkingSpot } from './parking.models';
import { matchesSearch } from './spot-search';

const occupied: ParkingSpot = {
  id: 'p07',
  code: 'P07',
  status: 'Occupied',
  activeAssignment: {
    assignmentId: 'a-p07',
    parkingSpotId: 'p07',
    spotCode: 'P07',
    licensePlate: 'ABC123',
    visitorName: 'María López',
    destinationUnit: 'Torre 3 Apto 502',
    checkedInAt: new Date('2026-10-11T15:00:00Z'),
  },
};

const available: ParkingSpot = {
  id: 'p08',
  code: 'P08',
  status: 'Available',
  activeAssignment: null,
};

describe('spot search', () => {
  it('matches every spot when the search is blank', () => {
    expect(matchesSearch(available, '')).toBe(true);
    expect(matchesSearch(occupied, '   ')).toBe(true);
  });

  it('finds a spot by its code ignoring case', () => {
    expect(matchesSearch(occupied, 'p07')).toBe(true);
    expect(matchesSearch(available, 'p07')).toBe(false);
  });

  it('finds a vehicle by plate however the operator types it', () => {
    expect(matchesSearch(occupied, 'abc-12')).toBe(true);
    expect(matchesSearch(occupied, 'ABC 123')).toBe(true);
  });

  it('finds a vehicle by visitor name ignoring case and accents', () => {
    expect(matchesSearch(occupied, 'maria')).toBe(true);
    expect(matchesSearch(occupied, 'LÓPEZ')).toBe(true);
  });

  it('finds a vehicle by the apartment it visits', () => {
    expect(matchesSearch(occupied, '502')).toBe(true);
    expect(matchesSearch(occupied, 'torre 3')).toBe(true);
  });

  it('does not match a free spot by vehicle data it does not have', () => {
    expect(matchesSearch(available, 'abc')).toBe(false);
    expect(matchesSearch(occupied, 'zzz')).toBe(false);
  });
});
