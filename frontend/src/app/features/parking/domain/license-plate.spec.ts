import { isValidPlate, normalizePlate } from './license-plate';

describe('license plate rules (UX mirror of the backend value object)', () => {
  it.each([
    [' abc-123 ', 'ABC123'],
    ['abc.12d', 'ABC12D'],
    ['ABC 123', 'ABC123'],
  ])('normalizes %j to %s', (raw, expected) => {
    expect(normalizePlate(raw)).toBe(expected);
    expect(isValidPlate(raw)).toBe(true);
  });

  it.each(['', 'AB-12', 'ABCD123', '123ABC', 'ABC1234'])('rejects %j', (raw) => {
    expect(isValidPlate(raw)).toBe(false);
  });
});
