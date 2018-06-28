// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    /// <summary>
    /// Constraints a route parameter to be an integer within a given range of values.
    /// </summary>
    internal class RangeEndpointMatchConstraint : IEndpointMatchConstraint
    {
        /// <summary>
        /// Gets the minimum allowed value of the route parameter.
        /// </summary>
        public long Min { get; private set; }

        /// <summary>
        /// Gets the maximum allowed value of the route parameter.
        /// </summary>
        public long Max { get; private set; }

        public void Initialize(string parameter)
        {
            var arguments = parameter.Split(',').Select(argument => argument.Trim()).ToArray();
            if (arguments.Length != 2)
            {
                throw new ArgumentException("Expected 2 but got more");
            }

            if (!Int64.TryParse(arguments[0], out var min))
            {
                throw new ArgumentException("Min needs to be a number.");
            }

            if (!Int64.TryParse(arguments[1], out var max))
            {
                throw new ArgumentException("Max needs to be a number.");
            }

            if (min > max)
            {
                throw new InvalidOperationException("Min cannot be greater than Max");
            }

            Min = min;
            Max = max;
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

            object value;
            if (values.TryGetValue(routeKey, out value) && value != null)
            {
                long longValue;
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (Int64.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                {
                    return longValue >= Min && longValue <= Max;
                }
            }

            return false;
        }
    }
}