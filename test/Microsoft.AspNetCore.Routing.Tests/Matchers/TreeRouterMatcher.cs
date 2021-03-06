﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // This is an adapter to use TreeRouter in the conformance tests
    internal class TreeRouterMatcher : Matcher
    {
        private readonly TreeRouter _inner;

        internal TreeRouterMatcher(TreeRouter inner)
        {
            _inner = inner;
        }

        public async override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var context = new RouteContext(httpContext);
            await _inner.RouteAsync(context);

            if (context.Handler != null)
            {
                feature.Values = context.RouteData.Values;
                await context.Handler(httpContext);
            }
        }
    }
}

