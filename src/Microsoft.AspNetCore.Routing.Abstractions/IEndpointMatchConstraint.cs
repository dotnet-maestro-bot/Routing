namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointMatchConstraint
    {
        void Initialize(string parameter);

        bool Match(string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
    }
}
