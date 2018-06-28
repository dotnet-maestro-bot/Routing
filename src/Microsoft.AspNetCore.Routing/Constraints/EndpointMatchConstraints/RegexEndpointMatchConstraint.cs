using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    internal class RegexEndpointMatchConstraint : IEndpointMatchConstraint
    {
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(10);

        public Regex Constraint { get; private set; }

        public void Initialize(string parameter)
        {
            Constraint = new Regex(
                parameter,
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
                RegexMatchTimeout);
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

                return Constraint.IsMatch(parameterValueString);
            }

            return false;
        }
    }
}
