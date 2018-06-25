// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Generated from https://github.com/Azure/azure-rest-api-specs
    public partial class MatcherSelectCandidatesAzureBenchmark : MatcherBenchmarkBase
    {
        private const int SampleCount = 100;

        private BarebonesMatcher _baseline;
        private MatcherBase _dfa;
        private MatcherBase _instruction;

        private int[] _samples;

        [GlobalSetup]
        public void Setup()
        {
            SetupEndpoints();

            SetupRequests();

            // The perf is kinda slow for these benchmarks, so we do some sampling
            // of the request data.
            _samples = SampleRequests(EndpointCount, SampleCount);

            _baseline = (BarebonesMatcher)SetupMatcher(new BarebonesMatcherBuilder());
            _dfa = (MatcherBase)SetupMatcher(new DfaMatcherBuilder());
            _instruction = (MatcherBase)SetupMatcher(new InstructionMatcherBuilder());
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = SampleCount)]
        public unsafe void Baseline()
        {
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];

                var path = httpContext.Request.Path.Value;
                var segments = new ReadOnlySpan<PathSegment>(Array.Empty<PathSegment>());

                var candidates = new CandidateSet(path, segments);
                _baseline._matchers[sample].SelectCandidates(httpContext, ref candidates);

                var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
                Validate(httpContext, _endpoints[sample], endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public unsafe void Dfa()
        {
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];

                var path = httpContext.Request.Path.Value;
                var buffer = stackalloc PathSegment[32];
                var count = FastPathTokenizer.Tokenize(path, buffer, 32);
                var segments = new ReadOnlySpan<PathSegment>((void*)buffer, count);

                var candidates = new CandidateSet(path, segments);
                _dfa.SelectCandidates(httpContext, ref candidates);

                var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
                Validate(httpContext, _endpoints[sample], endpoint);
            }
        }

        [Benchmark(OperationsPerInvoke = SampleCount)]
        public unsafe void Instruction()
        {
            for (var i = 0; i < SampleCount; i++)
            {
                var sample = _samples[i];
                var httpContext = _requests[sample];

                var path = httpContext.Request.Path.Value;
                var buffer = stackalloc PathSegment[32];
                var count = FastPathTokenizer.Tokenize(path, buffer, 32);
                var segments = new ReadOnlySpan<PathSegment>((void*)buffer, count);

                var candidates = new CandidateSet(path, segments);
                _instruction.SelectCandidates(httpContext, ref candidates);

                var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
                Validate(httpContext, _endpoints[sample], endpoint);
            }
        }
    }
}