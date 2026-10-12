// Espejo de UX del value object LicensePlate del backend (que sigue siendo la autoridad).
const PLATE_FORMAT = /^[A-Z]{3}(\d{3}|\d{2}[A-Z])$/;

export function normalizePlate(raw: string): string {
  return raw.replace(/[^a-z0-9]/gi, '').toUpperCase();
}

export function isValidPlate(raw: string): boolean {
  return PLATE_FORMAT.test(normalizePlate(raw));
}
