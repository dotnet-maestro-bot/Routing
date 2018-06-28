// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// A builder for produding a mapping of keys to see <see cref="IRouteConstraint"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="EndpointMatchConstraintBuilder"/> allows iterative building a set of route constraints, and will
    /// merge multiple entries for the same key.
    /// </remarks>
    public class EndpointMatchConstraintBuilder
    {
        private readonly IInlineEndpointMatchConstraintResolver _inlineConstraintResolver;
        private readonly string _displayName;

        private readonly Dictionary<string, List<IEndpointMatchConstraint>> _constraints;
        private readonly HashSet<string> _optionalParameters;
        /// <summary>
        /// Creates a new <see cref="RouteConstraintBuilder"/> instance.
        /// </summary>
        /// <param name="inlineConstraintResolver">The <see cref="IInlineConstraintResolver"/>.</param>
        /// <param name="displayName">The display name (for use in error messages).</param>
        public EndpointMatchConstraintBuilder(
            IInlineEndpointMatchConstraintResolver inlineConstraintResolver,
            string displayName)
        {
            if (inlineConstraintResolver == null)
            {
                throw new ArgumentNullException(nameof(inlineConstraintResolver));
            }

            if (displayName == null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            _inlineConstraintResolver = inlineConstraintResolver;
            _displayName = displayName;

            _constraints = new Dictionary<string, List<IEndpointMatchConstraint>>(StringComparer.OrdinalIgnoreCase);
            _optionalParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds a mapping of constraints.
        /// </summary>
        /// <returns>An <see cref="IDictionary{String, IEndpointMatchConstraint}"/> of the constraints.</returns>
        public IDictionary<string, IEndpointMatchConstraint> Build()
        {
            var constraints = new Dictionary<string, IEndpointMatchConstraint>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _constraints)
            {
                IEndpointMatchConstraint constraint;
                if (kvp.Value.Count == 1)
                {
                    constraint = kvp.Value[0];
                }
                else
                {
                    constraint = new CompositeEndpointMatchConstraint(kvp.Value.ToArray());
                }

                if (_optionalParameters.Contains(kvp.Key))
                {
                    var optionalConstraint = new OptionalEndpointMatchConstraint(constraint);
                    constraints.Add(kvp.Key, optionalConstraint);
                }
                else
                {
                    constraints.Add(kvp.Key, constraint);
                }
            }

            return constraints;
        }

        /// <summary>
        /// Adds a constraint instance for the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">
        /// The constraint instance. Must either be a string or an instance of <see cref="IRouteConstraint"/>.
        /// </param>
        /// <remarks>
        /// If the <paramref name="value"/> is a string, it will be converted to a <see cref="RegexRouteConstraint"/>.
        ///
        /// For example, the string <code>Product[0-9]+</code> will be converted to the regular expression
        /// <code>^(Product[0-9]+)</code>. See <see cref="System.Text.RegularExpressions.Regex"/> for more details.
        /// </remarks>
        public void AddConstraint(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var constraint = value as IEndpointMatchConstraint;
            if (constraint == null)
            {
                var regexPattern = value as string;
                if (regexPattern == null)
                {
                    throw new RouteCreationException(
                        Resources.FormatRouteConstraintBuilder_ValidationMustBeStringOrCustomConstraint(
                            key,
                            value,
                            _displayName,
                            typeof(IEndpointMatchConstraint)));
                }

                var constraintsRegEx = "^(" + regexPattern + ")$";
                constraint = new RegexEndpointMatchConstraint();
                constraint.Initialize(constraintsRegEx);
            }

            Add(key, constraint);
        }

        /// <summary>
        /// Adds a constraint for the given key, resolved by the <see cref="IInlineConstraintResolver"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="constraintText">The text to be resolved by <see cref="IInlineConstraintResolver"/>.</param>
        /// <remarks>
        /// The <see cref="IInlineConstraintResolver"/> can create <see cref="IRouteConstraint"/> instances
        /// based on <paramref name="constraintText"/>. See <see cref="RouteOptions.ConstraintMap"/> to register
        /// custom constraint types.
        /// </remarks>
        public void AddResolvedConstraint(string key, string constraintText)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (constraintText == null)
            {
                throw new ArgumentNullException(nameof(constraintText));
            }

            var constraint = _inlineConstraintResolver.ResolveConstraint(constraintText);
            if (constraint == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatRouteConstraintBuilder_CouldNotResolveConstraint(
                        key,
                        constraintText,
                        _displayName,
                        _inlineConstraintResolver.GetType().Name));
            }

            Add(key, constraint);
        }

        /// <summary>
        /// Sets the given key as optional.
        /// </summary>
        /// <param name="key">The key.</param>
        public void SetOptional(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _optionalParameters.Add(key);
        }

        private void Add(string key, IEndpointMatchConstraint constraint)
        {
            List<IEndpointMatchConstraint> list;
            if (!_constraints.TryGetValue(key, out list))
            {
                list = new List<IEndpointMatchConstraint>();
                _constraints.Add(key, list);
            }

            list.Add(constraint);
        }
    }
}
