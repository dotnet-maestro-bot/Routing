// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/APIs-guru/openapi-directory
    // Use https://editor2.swagger.io/ to convert from yaml to json-
    public partial class MatcherSelectCandidatesGithubBenchmark : MatcherBenchmarkBase
    {
        private BarebonesMatcher _baseline;
        private MatcherBase _dfa;
        private MatcherBase _instruction;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = (MatcherBase)SetupMatcher(new DfaMatcherBuilder());
            _instruction = (MatcherBase)SetupMatcher(new InstructionMatcherBuilder());
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = EndpointCount)]
        public unsafe void Baseline()
        {
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = _requests[i];

                var path = httpContext.Request.Path.Value;
                var segments = new ReadOnlySpan<PathSegment>(Array.Empty<PathSegment>());

                var candidates = new CandidateSet(path, segments);
                _baseline._matchers[i].SelectCandidates(httpContext, ref candidates);

                var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
                Validate(httpContext, _endpoints[i], endpoint);
            }
        }

        [Benchmark( OperationsPerInvoke = EndpointCount)]
        public unsafe void Dfa()
        {
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = _requests[i];

                var path = httpContext.Request.Path.Value;
                var buffer = stackalloc PathSegment[32];
                var count = FastPathTokenizer.Tokenize(path, buffer, 32);
                var segments = new ReadOnlySpan<PathSegment>((void*)buffer, count);

                var candidates = new CandidateSet(path, segments);
                _dfa.SelectCandidates(httpContext, ref candidates);

                var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
                Validate(httpContext, _endpoints[i], endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = EndpointCount)]
        public unsafe void Instruction()
        {
            for (var i = 0; i < EndpointCount; i++)
            {
                var httpContext = _requests[i];

                var path = httpContext.Request.Path.Value;
                var buffer = stackalloc PathSegment[32];
                var count = FastPathTokenizer.Tokenize(path, buffer, 32);
                var segments = new ReadOnlySpan<PathSegment>((void*)buffer, count);

                var candidates = new CandidateSet(path, segments);
                _instruction.SelectCandidates(httpContext, ref candidates);

                var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
                Validate(httpContext, _endpoints[i], endpoint);
            }
        }
    }
}