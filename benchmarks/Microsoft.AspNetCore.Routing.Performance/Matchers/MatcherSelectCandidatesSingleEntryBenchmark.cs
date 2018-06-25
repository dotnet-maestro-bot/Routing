// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class MatcherSelectCandidatesSingleEntryBenchmark : MatcherBenchmarkBase
    {
        private MatcherBase _baseline;
        private MatcherBase _dfa;
        private MatcherBase _instruction;

        [GlobalSetup]
        public void Setup()
        {
            _endpoints = new MatcherEndpoint[1];
            _endpoints[0] = CreateEndpoint("/plaintext");

            _requests = new HttpContext[1];
            _requests[0] = new DefaultHttpContext();
            _requests[0].RequestServices = CreateServices();
            _requests[0].Request.Path = "/plaintext";
            
            _baseline = (MatcherBase)SetupMatcher(new TrivialMatcherBuilder());
            _dfa = (MatcherBase)SetupMatcher(new DfaMatcherBuilder());
            _instruction = (MatcherBase)SetupMatcher(new InstructionMatcherBuilder());
        }

        private Matcher SetupMatcher(MatcherBuilder builder)
        {
            builder.AddEndpoint(_endpoints[0]);
            return builder.Build();
        }

        [Benchmark(Baseline = true)]
        public unsafe void Baseline()
        {
            var httpContext = _requests[0];
            var path = httpContext.Request.Path.Value;
            var segments = new ReadOnlySpan<PathSegment>(Array.Empty<PathSegment>());

            var candidates = new CandidateSet(path, segments);
            _baseline.SelectCandidates(httpContext, ref candidates);

            var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
            Validate(_requests[0], _endpoints[0], endpoint);
        }

        [Benchmark]
        public unsafe void Dfa()
        {
            var httpContext = _requests[0];
            var path = httpContext.Request.Path.Value;
            var buffer = stackalloc PathSegment[32];
            var count = FastPathTokenizer.Tokenize(path, buffer, 32);
            var segments = new ReadOnlySpan<PathSegment>((void*)buffer, count);

            var candidates = new CandidateSet(path, segments);
            _dfa.SelectCandidates(httpContext, ref candidates);

            var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
            Validate(_requests[0], _endpoints[0], endpoint);
        }

        [Benchmark]
        public unsafe void Instruction()
        {
            var httpContext = _requests[0];
            var path = httpContext.Request.Path.Value;
            var buffer = stackalloc PathSegment[32];
            var count = FastPathTokenizer.Tokenize(path, buffer, 32);
            var segments = new ReadOnlySpan<PathSegment>((void*)buffer, count);

            var candidates = new CandidateSet(path, segments);
            _instruction.SelectCandidates(httpContext, ref candidates);

            var endpoint = candidates.Candidates[candidates.CandidateIndices[0]].Endpoint;
            Validate(_requests[0], _endpoints[0], endpoint);
        }
    }
}
