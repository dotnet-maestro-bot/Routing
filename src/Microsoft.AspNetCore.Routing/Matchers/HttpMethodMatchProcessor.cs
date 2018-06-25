// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal sealed class HttpMethodMatchProcessor : MatchProcessor
    {
        private readonly string _httpMethod;

        public HttpMethodMatchProcessor(string httpMethod)
        {
            _httpMethod = httpMethod;
        }

        public override bool Process(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            return string.Equals(_httpMethod, httpContext.Request.Method, StringComparison.OrdinalIgnoreCase);
        }
    }
}
