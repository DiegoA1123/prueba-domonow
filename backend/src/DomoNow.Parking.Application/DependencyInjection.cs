using DomoNow.Parking.Application.Authentication;
using DomoNow.Parking.Application.ParkingAssignments.CheckIn;
using DomoNow.Parking.Application.ParkingAssignments.CheckOut;
using Microsoft.Extensions.DependencyInjection;

namespace DomoNow.Parking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CheckInHandler>();
        services.AddScoped<CheckOutHandler>();
        services.AddScoped<LoginHandler>();
        return services;
    }
}
