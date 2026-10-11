namespace DomoNow.Parking.Application.Authentication;

public interface IAccessTokenIssuer
{
    AccessToken Issue(AuthenticatedOperator authenticatedOperator);
}
