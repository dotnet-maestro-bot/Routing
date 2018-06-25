// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class DfaMatcherBuilderTest
    {
        [Fact]
        public void BuildDfaTree_SingleEndpoint_Empty()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint = CreateEndpoint("/");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches).Endpoint);
            Assert.Null(root.Parameters);
            Assert.Empty(root.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_Literals()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.Null(a.Parameters);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Same(endpoint, Assert.Single(c.Matches).Endpoint);
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_Parameters()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint = CreateEndpoint("{a}/{b}/{c}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Empty(root.Literals);

            var a = root.Parameters;
            Assert.Empty(a.Matches);
            Assert.Empty(a.Literals);

            var b = a.Parameters;
            Assert.Empty(b.Matches);
            Assert.Empty(b.Literals);

            var c = b.Parameters;
            Assert.Same(endpoint, Assert.Single(c.Matches).Endpoint);
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_CatchAll()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint = CreateEndpoint("{a}/{*b}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Empty(root.Literals);

            var a = root.Parameters;

            // The catch all can match a path like '/a'
            Assert.Same(endpoint, Assert.Single(a.Matches).Endpoint);
            Assert.Empty(a.Literals);
            Assert.Null(a.Parameters);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = a.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches).Endpoint);
            Assert.Empty(catchAll.Literals);
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_SingleEndpoint_CatchAllAtRoot()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint = CreateEndpoint("{*a}");
            builder.AddEndpoint(endpoint);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Same(endpoint, Assert.Single(root.Matches).Endpoint);
            Assert.Empty(root.Literals);

            // Catch-all nodes include an extra transition that loops to process
            // extra segments.
            var catchAll = root.CatchAll;
            Assert.Same(endpoint, Assert.Single(catchAll.Matches).Endpoint);
            Assert.Empty(catchAll.Literals);
            Assert.Same(catchAll, catchAll.Parameters);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndLiteral()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b1/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/b2/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);

            Assert.Equal(2, a.Literals.Count);

            var b1 = a.Literals["b1"];
            Assert.Empty(b1.Matches);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Same(endpoint1, Assert.Single(c1.Matches).Endpoint);
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var b2 = a.Literals["b2"];
            Assert.Empty(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches).Endpoint);
            Assert.Null(c2.Parameters);
            Assert.Empty(c2.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndParameter()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{b}/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b = next.Value;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var b2 = a.Parameters;
            Assert.Empty(b2.Matches);
            Assert.Null(b2.Parameters);

            next = Assert.Single(b2.Literals);
            Assert.Equal("c", next.Key);

            var c2 = next.Value;
            Assert.Same(endpoint2, Assert.Single(c2.Matches).Endpoint);
            Assert.Null(c2.Parameters);
            Assert.Empty(c2.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_ParameterAndParameter()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/{b1}/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{b2}/c");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Empty(a.Matches);
            Assert.Empty(a.Literals);

            var b = a.Parameters;
            Assert.Empty(b.Matches);
            Assert.Null(b.Parameters);

            next = Assert.Single(b.Literals);
            Assert.Equal("c", next.Key);

            var c = next.Value;
            Assert.Collection(
                c.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c.Parameters);
            Assert.Empty(c.Literals);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_LiteralAndCatchAll()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/b/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{*b}");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);

            next = Assert.Single(a.Literals);
            Assert.Equal("b", next.Key);

            var b1 = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches).Endpoint);
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        [Fact]
        public void BuildDfaTree_MultipleEndpoint_ParameterAndCatchAll()
        {
            // Arrange
            var builder = new DfaMatcherBuilder();

            var endpoint1 = CreateEndpoint("a/{b}/c");
            builder.AddEndpoint(endpoint1);

            var endpoint2 = CreateEndpoint("a/{*b}");
            builder.AddEndpoint(endpoint2);

            // Act
            var root = builder.BuildDfaTree();

            // Assert
            Assert.Empty(root.Matches);
            Assert.Null(root.Parameters);

            var next = Assert.Single(root.Literals);
            Assert.Equal("a", next.Key);

            var a = next.Value;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);
            Assert.Empty(a.Literals);

            var b1 = a.Parameters;
            Assert.Same(endpoint2, Assert.Single(a.Matches).Endpoint);
            Assert.Null(b1.Parameters);

            next = Assert.Single(b1.Literals);
            Assert.Equal("c", next.Key);

            var c1 = next.Value;
            Assert.Collection(
                c1.Matches,
                e => Assert.Same(endpoint1, e.Endpoint),
                e => Assert.Same(endpoint2, e.Endpoint));
            Assert.Null(c1.Parameters);
            Assert.Empty(c1.Literals);

            var catchAll = a.CatchAll;
            Assert.Same(endpoint2, Assert.Single(catchAll.Matches).Endpoint);
            Assert.Same(catchAll, catchAll.Parameters);
            Assert.Same(catchAll, catchAll.CatchAll);
        }

        private MatcherEndpoint CreateEndpoint(string template)
        {
            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                template,
                new RouteValueDictionary(),
                new RouteValueDictionary(),
                0,
                new EndpointMetadataCollection(Array.Empty<object>()),
                "test");
        }
    }
}
