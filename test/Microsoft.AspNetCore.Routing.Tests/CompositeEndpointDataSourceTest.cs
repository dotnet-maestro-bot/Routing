// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class CompositeEndpointDataSourceTest
    {
        [Fact]
        public void Endpoints_ReturnsAllEndpoints_FromMultipleDataSources()
        {
        }

        [Fact]
        public void Endpoints_ReflectsChangesToEndpoints_OnDataSourceChanges()
        {
            // Arrange-1
            var endpoint1 = CreateEndpoint("/a");

            // Act-1
            var dynamicDataSource = new DynamicEndpointDataSource(endpoint1);
            var datasource = new CompositeEndpointDataSource(new[] { dynamicDataSource });

            // Assert-1
            var endpoint = Assert.Single(datasource.Endpoints);
            Assert.Same(endpoint1, endpoint);

            // Arrange-2
            var endpoint2 = CreateEndpoint("/b");

            // Act-2
            dynamicDataSource.AddEndpoint(endpoint2);

            // Assert-2
            Assert.Equal(2, datasource.Endpoints.Count);
            Assert.Same(endpoint1, datasource.Endpoints[0]);
            Assert.Same(endpoint2, datasource.Endpoints[1]);

            // Arrange-3
            var endpoint3 = CreateEndpoint("/c");

            // Act-3
            dynamicDataSource.AddEndpoint(endpoint3);

            // Assert-3
            Assert.Equal(3, datasource.Endpoints.Count);
            Assert.Same(endpoint1, datasource.Endpoints[0]);
            Assert.Same(endpoint2, datasource.Endpoints[1]);
            Assert.Same(endpoint3, datasource.Endpoints[2]);
        }

        [Fact]
        public void EndpointsChanges_AreReflected_InConsumerCallback()
        {
            // Arrange1 & Act1
            var endpoint1 = CreateEndpoint("/a");
            var dynamicDataSource = new DynamicEndpointDataSource(endpoint1);
            var compositeDataSource = new CompositeEndpointDataSource(new[] { dynamicDataSource });

            IEnumerable<Endpoint> actual = null;
            compositeDataSource.ChangeToken.RegisterChangeCallback(
                (state) =>
                {
                    var _compositeDataSource = (CompositeEndpointDataSource)state;
                    actual = _compositeDataSource.Endpoints;
                },
                state: compositeDataSource);

            // Assert1
            Assert.Collection(actual, (ep) => Assert.Same(endpoint1, ep));

            // Arrange2
            var endpoint2 = CreateEndpoint("/b");

            // Act2
            dynamicDataSource.AddEndpoint(endpoint2);

            // Assert2
            Assert.Collection(
                actual,
                (ep) => Assert.Same(endpoint1, ep),
                (ep) => Assert.Same(endpoint2, ep));

            // Arrange3
            var endpoint3 = CreateEndpoint("/b");

            // Act3
            dynamicDataSource.AddEndpoint(endpoint3);

            // Assert3
            Assert.Collection(
                actual,
                (ep) => Assert.Same(endpoint1, ep),
                (ep) => Assert.Same(endpoint2, ep),
                (ep) => Assert.Same(endpoint3, ep));
        }

        private MatcherEndpoint CreateEndpoint(
            string template,
            object defaultValues = null,
            object requiredValues = null,
            int order = 0,
            string routeName = null)
        {
            var defaults = defaultValues == null ? new RouteValueDictionary() : new RouteValueDictionary(defaultValues);
            var required = requiredValues == null ? new RouteValueDictionary() : new RouteValueDictionary(requiredValues);

            return new MatcherEndpoint(
                next => (httpContext) => Task.CompletedTask,
                template,
                defaults,
                required,
                order,
                EndpointMetadataCollection.Empty,
                null);
        }
    }
}
