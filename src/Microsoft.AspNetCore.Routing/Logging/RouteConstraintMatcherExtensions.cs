// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Logging
{
    internal static class RouteConstraintMatcherExtensions
    {
        private static readonly Action<ILogger, object, string, IRouteConstraint, Exception> _routeValueDoesNotMatchConstraint;
        private static readonly Action<ILogger, object, string, IEndpointMatchConstraint, Exception> _routeValueDidNotSatisfyEndpointMatchConstraint;

        static RouteConstraintMatcherExtensions()
        {
            _routeValueDoesNotMatchConstraint = LoggerMessage.Define<object, string, IRouteConstraint>(
                LogLevel.Debug,
                1,
                "Route value '{RouteValue}' with key '{RouteKey}' did not match " +
                            "the constraint '{RouteConstraint}'.");

            _routeValueDidNotSatisfyEndpointMatchConstraint = LoggerMessage.Define<object, string, IEndpointMatchConstraint>(
                LogLevel.Debug,
                1,
                "Route value '{RouteValue}' with key '{RouteKey}' did not match " +
                            "the constraint '{RouteConstraint}'.");
        }

        public static void RouteValueDoesNotMatchConstraint(
            this ILogger logger,
            object routeValue,
            string routeKey,
            IRouteConstraint routeConstraint)
        {
            _routeValueDoesNotMatchConstraint(logger, routeValue, routeKey, routeConstraint, null);
        }

        public static void RouteValueDidNotSatisfyEndpointMatchConstraint(
            this ILogger logger,
            object routeValue,
            string routeKey,
            IEndpointMatchConstraint routeConstraint)
        {
            _routeValueDidNotSatisfyEndpointMatchConstraint(logger, routeValue, routeKey, routeConstraint, null);
        }
    }
}
