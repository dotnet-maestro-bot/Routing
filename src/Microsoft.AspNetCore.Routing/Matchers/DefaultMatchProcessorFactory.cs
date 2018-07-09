﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DefaultMatchProcessorFactory : MatchProcessorFactory
    {
        private readonly RouteOptions _options;
        private readonly ILogger<DefaultMatchProcessorFactory> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DefaultMatchProcessorFactory(
            IOptions<RouteOptions> options,
            ILogger<DefaultMatchProcessorFactory> logger,
            IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public override MatchProcessor Create(MatchProcessorReference matchProcessorReference)
        {
            if (matchProcessorReference == null)
            {
                throw new ArgumentNullException(nameof(matchProcessorReference));
            }

            if (matchProcessorReference.MatchProcessor != null)
            {
                return matchProcessorReference.MatchProcessor;
            }

            // Example:
            // {productId:regex(\d+)}
            //
            // ParameterName: productId
            // ConstraintText: regex(\d+)
            // ConstraintName: regex
            // ConstraintArgument: \d+

            (var constraintName, var constraintArgument) = Parse(matchProcessorReference.ConstraintText);

            if (!_options.ConstraintMap.TryGetValue(constraintName, out var constraintType))
            {
                throw new InvalidOperationException(
                    $"No constraint has been registered with name '{constraintName}'.");
            }

            var processor = ResolveMatchProcessor(
                matchProcessorReference.ParameterName,
                matchProcessorReference.Optional,
                constraintType,
                constraintArgument);

            if (processor != null)
            {
                return processor;
            }

            if (!typeof(IRouteConstraint).GetTypeInfo().IsAssignableFrom(constraintType.GetTypeInfo()))
            {
                throw new RouteCreationException(
                            Resources.FormatDefaultInlineConstraintResolver_TypeNotConstraint(
                                                        constraintType, constraintName, typeof(IRouteConstraint).Name));
            }

            try
            {
                return CreateMatchProcessorFromRouteConstraint(
                    matchProcessorReference.ParameterName,
                    constraintType,
                    constraintArgument);
            }
            catch (RouteCreationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new RouteCreationException(
                    $"An error occurred while trying to create an instance of route constraint '{constraintType.FullName}'.",
                    exception);
            }
        }

        private MatchProcessor CreateMatchProcessorFromRouteConstraint(
            string parameterName,
            Type constraintType,
            string constraintArgument)
        {
            var routeConstraint = DefaultInlineConstraintResolver.CreateConstraint(constraintType, constraintArgument);
            return MatchProcessorReference.From(parameterName, routeConstraint).MatchProcessor;
        }

        private MatchProcessor ResolveMatchProcessor(
            string parameterName,
            bool optional,
            Type constraintType,
            string constraintArgument)
        {
            if (constraintType == null)
            {
                throw new ArgumentNullException(nameof(constraintType));
            }

            if (!typeof(MatchProcessor).GetTypeInfo().IsAssignableFrom(constraintType.GetTypeInfo()))
            {
                // Since a constraint type could be of type IRouteConstraint, do not throw
                return null;
            }

            var registeredProcessor = _serviceProvider.GetRequiredService(constraintType);
            if (registeredProcessor is MatchProcessor matchProcessor)
            {
                if (optional)
                {
                    matchProcessor = new OptionalMatchProcessor(matchProcessor);
                }

                matchProcessor.Initialize(parameterName, constraintArgument);
                return matchProcessor;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Registered constraint type '{constraintType}' is not of type '{typeof(MatchProcessor)}'.");
            }
        }

        private (string constraintName, string constraintArgument) Parse(string constraintText)
        {
            string constraintName;
            string constraintArgument;
            var indexOfFirstOpenParens = constraintText.IndexOf('(');
            if (indexOfFirstOpenParens >= 0 && constraintText.EndsWith(")", StringComparison.Ordinal))
            {
                constraintName = constraintText.Substring(0, indexOfFirstOpenParens);
                constraintArgument = constraintText.Substring(
                    indexOfFirstOpenParens + 1,
                    constraintText.Length - indexOfFirstOpenParens - 2);
            }
            else
            {
                constraintName = constraintText;
                constraintArgument = null;
            }
            return (constraintName, constraintArgument);
        }
    }
}
