// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePattern
    {
        private const string SeparatorString = "/";

        internal RoutePattern(
            string rawText,
            Dictionary<string, object> defaults,
            Dictionary<string, IReadOnlyList<RoutePatternConstraintReference>> constraints,
            RoutePatternParameterPart[] parameters,
            RoutePatternPathSegment[] pathSegments)
        {
            Debug.Assert(defaults != null);
            Debug.Assert(constraints != null);
            Debug.Assert(parameters != null);
            Debug.Assert(pathSegments != null);

            RawText = rawText;
            Defaults = defaults;
            Constraints = constraints;
            Parameters = parameters;
            PathSegments = pathSegments;
        }

        public IReadOnlyDictionary<string, object> Defaults { get; }

        public IReadOnlyDictionary<string, IReadOnlyList<RoutePatternConstraintReference>> Constraints { get; }

        public string RawText { get; }

        public IReadOnlyList<RoutePatternParameterPart> Parameters { get; }

        public IReadOnlyList<RoutePatternPathSegment> PathSegments { get; }

        public static RoutePattern Parse(string pattern)
        {
            return RoutePatternParser.Parse(pattern);
        }

        public static RoutePattern Parse(string pattern, object defaults, object constraints)
        {
            var original = RoutePatternParser.Parse(pattern);
            return RoutePatternFactory.Pattern(original.RawText, defaults, constraints, original.PathSegments);
        }

        /// <summary>
        /// Gets the parameter matching the given name.
        /// </summary>
        /// <param name="name">The name of the parameter to match.</param>
        /// <returns>The matching parameter or <c>null</c> if no parameter matches the given name.</returns>
        public RoutePatternParameterPart GetParameter(string name)
        {
            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return parameter;
                }
            }

            return null;
        }

        private string DebuggerToString()
        {
            return RawText ?? string.Join(SeparatorString, PathSegments.Select(s => s.DebuggerToString()));
        }
    }
}
