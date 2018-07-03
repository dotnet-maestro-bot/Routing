// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteValueBasedEndpointFinderTest
    {
        [Fact]
        public void GetOutboundMatches_ReturnsNamedEndpoints_ImplementingIRouteNameMetadata()
        {
            // Arrange
            var dataSource = new CompositeEndpointDataSource(
                new[]
                {
                    new DefaultEndpointDataSource(
                        new Endpoint[]
                        {
                            new MatcherEndpoint(
                                next => hc => Task.CompletedTask,
                                "home/index",
                                new RouteValueDictionary(new { controller ="home", action="index" }),
                                new RouteValueDictionary(new { controller ="home", action="index" }),
                                0,
                                EndpointMetadataCollection.Empty,
                                "displayName"),
                            new MatcherEndpoint(
                                next => hc => Task.CompletedTask,
                                "products/index",
                                new RouteValueDictionary(new { controller ="products", action="index" }),
                                new RouteValueDictionary(new { controller ="prdoucts", action="index" }),
                                0,
                                new EndpointMetadataCollection(new[]{ new RouteNameMetadata("AllProducts") }),
                                "displayName"),
                            new MatcherEndpoint(
                                next => hc => Task.CompletedTask,
                                "customers/details/{id?}",
                                new RouteValueDictionary(new { controller ="customers", action="details" }),
                                new RouteValueDictionary(new { controller ="customers", action="details" }),
                                0,
                                new EndpointMetadataCollection(new[]{ new NameMetadata("CustomerDetails") }),
                                "displayName")
                        })
                });
            var expectedEndpoint = dataSource.Endpoints[1];
            var finder = new RouteValuesBasedEndpointFinder(
                dataSource,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(Options.Create(new RouteOptions())));

            // Act
            var (allMatches, namedMatches) = finder.GetOutboundMatches();

            // Assert
            Assert.True(namedMatches.TryGetValue("AllProducts", out var matches));
            var outboundMatch = Assert.Single(matches);
            Assert.NotNull(outboundMatch.Entry);
            var actualEndpoint = Assert.IsType<MatcherEndpoint>(outboundMatch.Entry.Data);
            Assert.Same(expectedEndpoint, actualEndpoint);
        }

        [Fact]
        public void GetOutboundMatches_ReturnsMatches_AfterChangeTokenIsFired()
        {
            // Arrange 1
            var actualDataSource = new ChangeEventProducingEndpointDatasource();
            var dataSource = new CompositeEndpointDataSource(new[] { actualDataSource });
            var finder = new RouteValuesBasedEndpointFinder(
                dataSource,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                new DefaultInlineConstraintResolver(Options.Create(new RouteOptions())));

            // Act 1
            var (allMatches, namedMatches) = finder.GetOutboundMatches();

            // Assert 1
            Assert.False(namedMatches.TryGetValue("AllProducts", out var matches));

            // Arrange 2
            // Trigger a fake change
            actualDataSource.TriggerChange();
            var expectedEndpoint = dataSource.Endpoints[1];

            // Act 2
            (allMatches, namedMatches) = finder.GetOutboundMatches();

            // Assert 2
            Assert.True(namedMatches.TryGetValue("AllProducts", out matches));
            var outboundMatch = Assert.Single(matches);
            Assert.NotNull(outboundMatch.Entry);
            var actualEndpoint = Assert.IsType<MatcherEndpoint>(outboundMatch.Entry.Data);
            Assert.Same(expectedEndpoint, actualEndpoint);
        }

        private class RouteNameMetadata : IRouteNameMetadata
        {
            public RouteNameMetadata(string routeName)
            {
                Name = routeName;
            }
            public string Name { get; }
        }

        private class NameMetadata : INameMetadata
        {
            public NameMetadata(string routeName)
            {
                Name = routeName;
            }
            public string Name { get; }
        }

        private class ChangeEventProducingEndpointDatasource : EndpointDataSource
        {
            private readonly List<Endpoint> _endpoints;
            private readonly TestChangeToken _token;

            public ChangeEventProducingEndpointDatasource()
            {
                _endpoints = new List<Endpoint>();
                Endpoints = _endpoints;

                _endpoints.Add(
                    new MatcherEndpoint(
                        next => hc => Task.CompletedTask,
                        "home/index",
                        new RouteValueDictionary(new { controller = "home", action = "index" }),
                        new RouteValueDictionary(new { controller = "home", action = "index" }),
                        0,
                        EndpointMetadataCollection.Empty,
                        "displayName"));

                _token = new TestChangeToken();
                ChangeToken = _token;
            }

            public override IChangeToken ChangeToken { get; }

            public override IReadOnlyList<Endpoint> Endpoints { get; }

            public void TriggerChange()
            {
                _endpoints.Add(
                    new MatcherEndpoint(
                        next => hc => Task.CompletedTask,
                        "products/index",
                        new RouteValueDictionary(new { controller = "products", action = "index" }),
                        new RouteValueDictionary(new { controller = "prdoucts", action = "index" }),
                        0,
                        new EndpointMetadataCollection(new[] { new RouteNameMetadata("AllProducts") }),
                        "displayName"));

                foreach (var callback in _token.Callbacks)
                {
                    callback.Item1(callback.Item2);
                }
            }

            private class TestChangeToken : IChangeToken
            {
                public TestChangeToken()
                {
                    Callbacks = new List<(Action<object>, object)>();
                }

                public List<(Action<object>, object)> Callbacks { get; }

                public bool HasChanged { get; set; }

                public bool ActiveChangeCallbacks => true;

                public IDisposable RegisterChangeCallback(Action<object> callback, object state)
                {
                    Callbacks.Add((callback, state));
                    return new NullDisposable();
                }
            }

            private class NullDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}