﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Dispatcher;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    public class TreeMatcherFactory : IDefaultMatcherFactory
    {
        public MatcherEntry CreateDispatcher(DispatcherDataSource dataSource, IEnumerable<EndpointSelector> endpointSelectors)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            var matcher = new TreeMatcher()
            {
                DataSource = dataSource,
            };

            foreach (var endpointSelector in endpointSelectors)
            {
                matcher.Selectors.Add(endpointSelector);
            }

            return new MatcherEntry()
            {
                AddressProvider = matcher,
                Matcher = matcher,
                EndpointProvider = matcher,
            };
        }
    }
}