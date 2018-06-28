using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultInlineEndpointMatchConstraintResolver : IInlineEndpointMatchConstraintResolver
    {
        private readonly IDictionary<string, Type> _constraintMap;
        private readonly IEnumerable<IEndpointMatchConstraint> _registeredEndpointMatchConstraints;

        public DefaultInlineEndpointMatchConstraintResolver(
            IEndpointMatchConstraintMapProvider endpointMatchConstraintMapProvider,
            IEnumerable<IEndpointMatchConstraint> endpointMatchConstraints)
        {
            _constraintMap = endpointMatchConstraintMapProvider.GetConstraintMap();
            _registeredEndpointMatchConstraints = endpointMatchConstraints;
        }

        public IEndpointMatchConstraint ResolveConstraint(string inlineConstraint)
        {
            if (inlineConstraint == null)
            {
                throw new ArgumentNullException(nameof(inlineConstraint));
            }

            string constraintKey;
            string argumentString;
            var indexOfFirstOpenParens = inlineConstraint.IndexOf('(');
            if (indexOfFirstOpenParens >= 0 && inlineConstraint.EndsWith(")", StringComparison.Ordinal))
            {
                constraintKey = inlineConstraint.Substring(0, indexOfFirstOpenParens);
                argumentString = inlineConstraint.Substring(
                    indexOfFirstOpenParens + 1,
                    inlineConstraint.Length - indexOfFirstOpenParens - 2);
            }
            else
            {
                constraintKey = inlineConstraint;
                argumentString = null;
            }

            Type constraintType;
            if (!_constraintMap.TryGetValue(constraintKey, out constraintType))
            {
                // Cannot resolve the constraint key
                return null;
            }

            if (!typeof(IEndpointMatchConstraint).GetTypeInfo().IsAssignableFrom(constraintType.GetTypeInfo()))
            {
                throw new RouteCreationException(
                            Resources.FormatDefaultInlineConstraintResolver_TypeNotConstraint(
                                                        constraintType, constraintKey, typeof(IRouteConstraint).Name));
            }

            var registeredConstraint = GetAndInitializeConstraint(constraintType, argumentString);
            if (registeredConstraint == null)
            {
                throw new InvalidOperationException($"No registered constraint found for '{constraintKey}'.");
            }
            return registeredConstraint;
        }

        private IEndpointMatchConstraint GetAndInitializeConstraint(Type constraintType, string argumentString)
        {
            var registeredConstraint = _registeredEndpointMatchConstraints
                    .FirstOrDefault(epmc => constraintType.IsAssignableFrom(epmc.GetType()));

            if (registeredConstraint != null)
            {
                registeredConstraint.Initialize(argumentString);
            }

            return registeredConstraint;
        }
    }
}
