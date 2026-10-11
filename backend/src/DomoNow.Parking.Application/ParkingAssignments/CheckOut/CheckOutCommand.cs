namespace DomoNow.Parking.Application.ParkingAssignments.CheckOut;

/// <param name="AssignmentId">Asignación activa a finalizar.</param>
/// <param name="CheckedOutAt">Opcional (registro tardío). Si es null se usa la hora del servidor.</param>
public sealed record CheckOutCommand(Guid AssignmentId, DateTimeOffset? CheckedOutAt);
