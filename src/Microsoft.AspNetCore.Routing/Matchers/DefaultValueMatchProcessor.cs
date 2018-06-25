// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal sealed class DefaultValueMatchProcessor : MatchProcessor
    {
        private readonly string _parameterName;
        private readonly object _default;

        public DefaultValueMatchProcessor(string parameterName, object @default)
        {
            _parameterName = parameterName;
            _default = @default;
        }

        public override bool Process(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            values[_parameterName] = _default;
            return true;
        }
    }
}