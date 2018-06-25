// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal sealed class ParameterSegmentMatchProcessor : MatchProcessor
    {
        private readonly int _segment;
        private readonly string _name;
        private readonly bool _isCatchAll;
        private readonly bool _hasDefaultValue;
        private readonly object _defaultValue;

        public ParameterSegmentMatchProcessor(int segment, string name, bool isCatchAll)
        {
            _segment = segment;
            _name = name;
            _isCatchAll = isCatchAll;
        }

        public ParameterSegmentMatchProcessor(
            int segment,
            string name,
            bool isCatchAll,
            bool hasDefaultValue,
            object defaultValue)
        {
            _segment = segment;
            _name = name;
            _isCatchAll = isCatchAll;
            _hasDefaultValue = hasDefaultValue;
            _defaultValue = defaultValue;
        }

        public override bool Process(
            HttpContext httpContext,
            string path,
            ReadOnlySpan<PathSegment> segments,
            RouteValueDictionary values)
        {
            if (segments.Length > _segment && _isCatchAll)
            {
                var segment = segments[_segment];
                values[_name] = path.Substring(segment.Start, path.Length - segment.Start);
                return true;
            }
            else if (segments.Length > _segment)
            {
                var segment = segments[_segment];
                values[_name] = path.Substring(segment.Start, segment.Length);
                return true;
            }
            else if (_hasDefaultValue)
            {
                values[_name] = _defaultValue;
                return true;
            }

            return true;
        }
    }
}