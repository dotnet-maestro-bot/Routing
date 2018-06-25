// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal sealed class ComplexSegmentMatchProcessor : MatchProcessor
    {
        private readonly int _index;
        private readonly TemplateSegment _segment;

        public ComplexSegmentMatchProcessor(int index, TemplateSegment segment)
        {
            _index = index;
            _segment = segment;
        }

        public override bool Process(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            if (segments.Length > _index)
            {
                var segment = segments[_index];
                var text = path.Substring(segment.Start, segment.Length);
                return TemplateMatcher.MatchComplexSegment(_segment, text, new RouteValueDictionary(), values);
            }

            return false;
        }
    }
}
