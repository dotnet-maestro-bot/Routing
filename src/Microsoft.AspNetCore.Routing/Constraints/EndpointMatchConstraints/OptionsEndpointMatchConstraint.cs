// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Routing.Constraints
{
    /// <summary>
    /// Defines a constraint on an optional parameter. If the parameter is present, then it is constrained by InnerConstraint. 
    /// </summary>
    internal class OptionalEndpointMatchConstraint : IEndpointMatchConstraint
    {
        public OptionalEndpointMatchConstraint(IEndpointMatchConstraint innerConstraint)
        {
            if (innerConstraint == null)
            {
                throw new ArgumentNullException(nameof(innerConstraint));
            }

            InnerConstraint = innerConstraint;
        }

        public IEndpointMatchConstraint InnerConstraint { get; }

        public void Initialize(string parameter)
        {
            InnerConstraint.Initialize(parameter);
        }

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
            if (values.TryGetValue(routeKey, out value))
            {
                return InnerConstraint.Match(routeKey,
                                             values,
                                             routeDirection);
            }

            return true;
        }
    }
}