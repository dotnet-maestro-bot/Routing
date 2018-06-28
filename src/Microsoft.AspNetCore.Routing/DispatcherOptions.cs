// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing
{
    public class DispatcherOptions
    {
        private IDictionary<string, Type> _constraintTypeMap = GetDefaultConstraintMap();

        public IList<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

        public IDictionary<string, Type> ConstraintMap
        {
            get
            {
                return _constraintTypeMap;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(ConstraintMap));
                }

                _constraintTypeMap = value;
            }
        }

        private static IDictionary<string, Type> GetDefaultConstraintMap()
        {
            return new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                // Type-specific constraints
                { "int", typeof(IntEndpointMatchConstraint) },
                { "regex", typeof(RegexEndpointMatchConstraint) },

                { "range", typeof(RangeEndpointMatchConstraint) },
            };
        }
    }
}
