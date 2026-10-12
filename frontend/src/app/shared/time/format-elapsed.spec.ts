import { formatElapsed } from './format-elapsed';

describe('formatElapsed', () => {
  const since = new Date('2026-10-11T15:00:00Z');
  const after = (minutes: number): number => since.getTime() + minutes * 60_000;

  it.each([
    [0, 'hace menos de 1 min'],
    [25, 'hace 25 min'],
    [120, 'hace 2 h'],
    [125, 'hace 2 h 5 min'],
  ])('after %i minutes says %s', (minutes, expected) => {
    expect(formatElapsed(since, after(minutes))).toBe(expected);
  });

  it('never shows negative times when clocks drift', () => {
    expect(formatElapsed(since, after(-3))).toBe('hace menos de 1 min');
  });
});
