// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal sealed class RouteConstraintMatchProcessor : MatchProcessor
    {
        private readonly string _name;
        private readonly IRouteConstraint _constraint;

        public RouteConstraintMatchProcessor(string name, IRouteConstraint constraint)
        {
            _name = name;
            _constraint = constraint;
        }

        public override bool Process(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            return _constraint.Match(httpContext, NullRouter.Instance, _name, values, RouteDirection.IncomingRequest);
        }

        private class NullRouter : IRouter
        {
            public static readonly NullRouter Instance = new NullRouter();

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                throw new NotImplementedException();
            }

            public Task RouteAsync(RouteContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
