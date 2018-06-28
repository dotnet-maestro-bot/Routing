using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    internal class IntEndpointMatchConstraint : IEndpointMatchConstraint
    {
        public void Initialize(string parameter)
        {
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

            if (values.TryGetValue(routeKey, out var value) && value != null)
            {
                if (value is int)
                {
                    return true;
                }

                int result;
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                return int.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            }

            return false;
        }
    }
}
