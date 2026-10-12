import { normalizePlate } from './license-plate';
import { ParkingSpot } from './parking.models';

/** Minúsculas y sin tildes: el portero escribe "maria" y encuentra a "María". */
function normalizeText(value: string): string {
  return value
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase()
    .trim();
}

/**
 * ¿El parqueadero coincide con lo que escribió el portero? Busca por código, placa (sin importar
 * guiones ni espacios), visitante y apartamento.
 */
export function matchesSearch(spot: ParkingSpot, term: string): boolean {
  const text = normalizeText(term);
  if (text === '') {
    return true;
  }
  const assignment = spot.activeAssignment;
  const plateTerm = normalizePlate(term);
  const plateMatches =
    assignment !== null && plateTerm !== '' && assignment.licensePlate.includes(plateTerm);
  const fields = [
    spot.code,
    ...(assignment ? [assignment.visitorName, assignment.destinationUnit] : []),
  ];
  return plateMatches || fields.some((field) => normalizeText(field).includes(text));
}
