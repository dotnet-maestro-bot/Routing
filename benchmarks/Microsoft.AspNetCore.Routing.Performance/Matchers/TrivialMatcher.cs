// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // A test-only matcher implementation - used as a baseline for simpler
    // perf tests. The idea with this matcher is that we can cheat on the requirements
    // to establish a lower bound for perf comparisons.
    internal class TrivialMatcher : MatcherBase
    {
        private readonly MatcherEndpoint _endpoint;
        private readonly Candidate[] _candidates;
        private readonly int[] _candidateIndices;
        private readonly int[] _candidateGroups;

        public TrivialMatcher(MatcherEndpoint endpoint)
        {
            _endpoint = endpoint;

            _candidates = new Candidate[]
            {
                new Candidate(endpoint, Array.Empty<MatchProcessor>()),
            };

            _candidateIndices = new int[] { 0, };
            _candidateGroups = new int[] { 1, };
        }

        public new Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var path = httpContext.Request.Path.Value;
            if (string.Equals(_endpoint.Template, path, StringComparison.OrdinalIgnoreCase))
            {
                feature.Endpoint = _endpoint;
                feature.Values = new RouteValueDictionary();
            }

            return Task.CompletedTask;
        }

        protected internal override void SelectCandidates(HttpContext httpContext, ref CandidateSet candidates)
        {
            var path = httpContext.Request.Path.Value;
            if (string.Equals(_endpoint.Template, path, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Candidates = _candidates;
                candidates.CandidateIndices = _candidateIndices;
                candidates.CandidateGroups = _candidateGroups;
            }
        }
    }
}
