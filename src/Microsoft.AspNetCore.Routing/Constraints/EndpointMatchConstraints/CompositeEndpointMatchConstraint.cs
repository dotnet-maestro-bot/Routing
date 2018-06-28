using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    /// <summary>
    /// Constrains a route by several child constraints.
    /// </summary>
    internal class CompositeEndpointMatchConstraint : IEndpointMatchConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeRouteConstraint" /> class.
        /// </summary>
        /// <param name="constraints">The child constraints that must match for this constraint to match.</param>
        public CompositeEndpointMatchConstraint(IEnumerable<IEndpointMatchConstraint> constraints)
        {
            if (constraints == null)
            {
                throw new ArgumentNullException(nameof(constraints));
            }

            Constraints = constraints;
        }

        /// <summary>
        /// Gets the child constraints that must match for this constraint to match.
        /// </summary>
        public IEnumerable<IEndpointMatchConstraint> Constraints { get; private set; }

        public void Initialize(string parameter)
        {
            foreach (var constraint in Constraints)
            {
                constraint.Initialize(parameter);
            }
        }

        /// <inheritdoc />
        public bool Match(
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (var constraint in Constraints)
            {
                if (!constraint.Match(routeKey, values, routeDirection))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
