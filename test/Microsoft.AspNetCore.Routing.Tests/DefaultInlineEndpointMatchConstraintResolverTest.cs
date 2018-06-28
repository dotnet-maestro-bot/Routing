using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class DefaultInlineEndpointMatchConstraintResolverTest
    {
        [Fact]
        public void ResolveConstraint_GetsAnInitializedConstraint()
        {
            // Arrange
            var map = new Dictionary<string, Type>();
            map.Add("str", typeof(TestStringMatchConstraint));
            var mapProvider = new TestConstraintMapProvider(map);
            var registeredConstraints = new IEndpointMatchConstraint[]
            {
                new TestStringMatchConstraint()
            };
            var resolver = new DefaultInlineEndpointMatchConstraintResolver(mapProvider, registeredConstraints);

            // Act
            var resolvedConstraint = resolver.ResolveConstraint("str('foo')");

            // Assert
            Assert.Same(registeredConstraints[0], resolvedConstraint);
            var constraint = Assert.IsType<TestStringMatchConstraint>(resolvedConstraint);
            Assert.Equal("'foo'", constraint.Parameter);
        }

        private class TestConstraintMapProvider : IEndpointMatchConstraintMapProvider
        {
            private readonly Dictionary<string, Type> _map;

            public TestConstraintMapProvider(Dictionary<string, Type> map)
            {
                _map = map;
            }

            public IDictionary<string, Type> GetConstraintMap()
            {
                return _map;
            }
        }

        private class TestStringMatchConstraint : IEndpointMatchConstraint
        {
            public string Parameter { get; private set; }

            public void Initialize(string parameter)
            {
                Parameter = parameter;
            }

            public bool Match(string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
            {
                if (routeKey == null)
                {
                    throw new ArgumentNullException(nameof(routeKey));
                }

                if (values == null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                if (values.TryGetValue(routeKey, out var routeValue) && routeValue != null)
                {
                    var parameterValueString = Convert.ToString(routeValue, CultureInfo.InvariantCulture);

                    return parameterValueString.Equals(Parameter, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
        }
    }
}
