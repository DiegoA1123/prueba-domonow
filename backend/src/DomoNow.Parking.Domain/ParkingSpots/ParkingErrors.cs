using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Domain.ParkingSpots;

public static class ParkingErrors
{
    public static readonly Error LicensePlateRequired = Error.Validation(
        "Parking.LicensePlateRequired", "La placa del vehículo es obligatoria.");

    public static readonly Error InvalidVisitorName = Error.Validation(
        "Parking.InvalidVisitorName", "El nombre del visitante es obligatorio y debe tener entre 2 y 100 caracteres.");

    public static readonly Error InvalidDestinationUnit = Error.Validation(
        "Parking.InvalidDestinationUnit", "La unidad que visita es obligatoria y debe tener máximo 20 caracteres.");

    public static readonly Error CheckOutBeforeCheckIn = Error.BusinessRule(
        "Parking.CheckOutBeforeCheckIn", "La fecha de salida no puede ser anterior a la fecha de ingreso.");

    public static readonly Error CheckOutInFuture = Error.BusinessRule(
        "Parking.CheckOutInFuture", "La fecha de salida no puede estar en el futuro.");

    public static readonly Error SpotAlreadyTaken = Error.Conflict(
        "Parking.SpotAlreadyTaken", "El parqueadero acaba de ser asignado por otra solicitud. Actualice e intente de nuevo.");

    public static readonly Error ConcurrentModification = Error.Conflict(
        "Parking.ConcurrentModification", "El parqueadero fue modificado por otra solicitud. Actualice e intente de nuevo.");

    public static readonly Error VehicleAlreadyInsideConflict = Error.Conflict(
        "Parking.VehicleAlreadyInside", "El vehículo ya se encuentra dentro de la propiedad.");

    public static Error InvalidLicensePlate(string raw) => Error.Validation(
        "Parking.InvalidLicensePlate",
        $"La placa '{Truncate(raw)}' no es válida. Formatos aceptados: ABC123 (carro) o ABC12D (moto).");

    public static Error InvalidSpotCode(string? raw) => Error.Validation(
        "Parking.InvalidSpotCode", $"El código de parqueadero '{Truncate(raw ?? string.Empty)}' no es válido.");

    public static Error SpotNotFound(Guid spotId) => Error.NotFound(
        "Parking.SpotNotFound", $"No existe el parqueadero '{spotId}'.");

    public static Error ActiveAssignmentNotFound(Guid assignmentId) => Error.NotFound(
        "Parking.ActiveAssignmentNotFound", $"No existe una asignación activa con id '{assignmentId}'.");

    public static Error SpotOccupied(SpotCode code) => Error.Conflict(
        "Parking.SpotOccupied", $"El parqueadero {code} ya está ocupado.");

    public static Error SpotOutOfService(SpotCode code) => Error.BusinessRule(
        "Parking.SpotOutOfService", $"El parqueadero {code} está fuera de servicio.");

    public static Error AssignmentNotActive(Guid assignmentId) => Error.Conflict(
        "Parking.AssignmentNotActive", $"La asignación '{assignmentId}' no está activa.");

    public static Error VehicleAlreadyInside(LicensePlate plate) => Error.Conflict(
        "Parking.VehicleAlreadyInside", $"El vehículo {plate} ya se encuentra dentro de la propiedad.");

    private static string Truncate(string value) => value.Length <= 20 ? value : value[..20] + "…";
}
