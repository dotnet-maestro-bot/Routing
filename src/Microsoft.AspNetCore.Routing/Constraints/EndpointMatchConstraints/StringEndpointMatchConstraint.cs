using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    internal class StringEndpointMatchConstraint : IEndpointMatchConstraint
    {
        public string Value { get; private set; }

        public void Initialize(string parameter)
        {
            Value = parameter;
        }

        public bool Match(string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.TryGetValue(routeKey, out var routeValue) && routeValue != null)
            {
                var parameterValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture);

                return parameterValueString.Equals(Value, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
