// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing.TestObjects
{
    public class DynamicEndpointDataSource : EndpointDataSource
    {
        private readonly List<Endpoint> _endpoints;
        private readonly TestChangeToken _testChangeToken;

        public DynamicEndpointDataSource(params Endpoint[] endpoints)
        {
            _endpoints = new List<Endpoint>();
            _endpoints.AddRange(endpoints);
            _testChangeToken = new TestChangeToken();
        }

        public override IChangeToken ChangeToken => _testChangeToken;

        public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

        // To trigger change
        public void AddEndpoint(Endpoint endpoint)
        {
            _endpoints.Add(endpoint);
            _testChangeToken.Changed();
        }
    }
}
