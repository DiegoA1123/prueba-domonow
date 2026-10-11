const MINUTE_MS = 60_000;
const HOUR_MS = 60 * MINUTE_MS;

export function formatElapsed(since: Date, now: number): string {
  const elapsed = Math.max(0, now - since.getTime());
  if (elapsed < MINUTE_MS) {
    return 'hace menos de 1 min';
  }
  if (elapsed < HOUR_MS) {
    return `hace ${Math.floor(elapsed / MINUTE_MS)} min`;
  }
  const hours = Math.floor(elapsed / HOUR_MS);
  const minutes = Math.floor((elapsed % HOUR_MS) / MINUTE_MS);
  return minutes === 0 ? `hace ${hours} h` : `hace ${hours} h ${minutes} min`;
}
