// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Routing.Matchers.DfaMatcher;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DfaMatcherBuilder : MatcherBuilder
    {
        private readonly List<MatcherBuilderEntry> _entries = new List<MatcherBuilderEntry>();
        private readonly IInlineConstraintResolver _constraintResolver = new DefaultInlineConstraintResolver(Options.Create(new RouteOptions()));

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            _entries.Add(new MatcherBuilderEntry(endpoint));
        }

        public DfaNode BuildDfaTree()
        {
            // We build the tree by doing a BFS over the list of entries. This is important
            // because a 'parameter' node can also traverse the same paths that literal nodes
            // traverse. This means that we need to order the entries first, or else we will
            // miss possible edges in the DFA.
            _entries.Sort();

            // Since we're doing a BFS we will process each 'level' of the tree in stages
            // this list will hold the set of items we need to process at the current
            // stage.
            var work = new List<(MatcherBuilderEntry entry, List<DfaNode> parents)>();

            var root = new DfaNode() { Depth = 0, Label = "/" };

            // To prepare for this we need to compute the max depth, as well as
            // a seed list of items to process (entry, root).
            var maxDepth = 0;
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                maxDepth = Math.Max(maxDepth, entry.Pattern.Segments.Count);

                work.Add((entry, new List<DfaNode>() { root, }));
            }

            // Now we process the entries a level at a time.
            for (var depth = 0; depth <= maxDepth; depth++)
            {
                // As we process items, collect the next set of items.
                var nextWork = new List<(MatcherBuilderEntry entry, List<DfaNode> parents)>();
                
                for (var i = 0; i < work.Count; i++)
                {
                    var (entry, parents) = work[i];

                    if (!HasAdditionalRequiredSegments(entry, depth))
                    {
                        for (var j = 0; j < parents.Count; j++)
                        {
                            var parent = parents[j];
                            parent.Matches.Add(entry);
                        }
                    }

                    // Find the parents of this edge at the current depth
                    var nextParents = new List<DfaNode>();
                    var segment = GetCurrentSegment(entry, depth);
                    if (segment == null)
                    {
                        continue;
                    }

                    for (var j = 0; j < parents.Count; j++)
                    {
                        var parent = parents[j];
                        if (segment.IsSimple && segment.Parts[0].IsLiteral)
                        {
                            var literal = segment.Parts[0].Text;
                            if (!parent.Literals.TryGetValue(literal, out var next))
                            {
                                next = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + literal + "/",
                                };
                                parent.Literals.Add(literal, next);
                            }

                            nextParents.Add(next);
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsCatchAll)
                        {
                            // A catch all should traverse all literal nodes as well as parameter nodes
                            // we don't need to create the parameter node here because of ordering
                            // all catchalls will be processed after all parameters.
                            nextParents.AddRange(parent.Literals.Values);
                            if (parent.Parameters != null)
                            {
                                nextParents.Add(parent.Parameters);
                            }
                            
                            // We also create a 'catchall' here. We don't do further traversals
                            // on the catchall node because only catchalls can end up here. The
                            // catchall node allows us to capture an unlimited amount of segments
                            // and also to match a zero-length segment, which a parameter node
                            // doesn't allow.
                            if (parent.CatchAll == null)
                            {
                                parent.CatchAll = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + "{*...}/",
                                };

                                // The catchall node just loops.
                                parent.CatchAll.Parameters = parent.CatchAll;
                                parent.CatchAll.CatchAll = parent.CatchAll;
                            }

                            parent.CatchAll.Matches.Add(entry);
                        }
                        else if (segment.IsSimple && segment.Parts[0].IsParameter)
                        {
                            if (parent.Parameters == null)
                            {
                                parent.Parameters = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + "{...}/",
                                };
                            }

                            // A parameter should traverse all literal nodes as well as the parameter node
                            nextParents.AddRange(parent.Literals.Values);
                            nextParents.Add(parent.Parameters);
                        }
                        else
                        {
                            // Complex segment - we treat these are parameters here and do the
                            // expensive processing later. We don't want to spend time processing
                            // complex segments unless they are the best match, and treating them
                            // like parameters in the DFA allows us to do just that.
                            if (parent.Parameters == null)
                            {
                                parent.Parameters = new DfaNode()
                                {
                                    Depth = parent.Depth + 1,
                                    Label = parent.Label + "{...}/",
                                };
                            }

                            nextParents.AddRange(parent.Literals.Values);
                            nextParents.Add(parent.Parameters);
                        }
                    }

                    if (nextParents.Count > 0)
                    {
                        nextWork.Add((entry, nextParents));
                    }
                }

                // Prepare the process the next stage.
                work = nextWork;
            }

            return root;
        }

        private TemplateSegment GetCurrentSegment(MatcherBuilderEntry entry, int depth)
        {
            if (depth < entry.Pattern.Segments.Count)
            {
                return entry.Pattern.Segments[depth];
            }

            if (entry.Pattern.Segments.Count == 0)
            {
                return null;
            }

            var lastSegment = entry.Pattern.Segments[entry.Pattern.Segments.Count - 1];
            if (lastSegment.IsSimple && lastSegment.Parts[0].IsCatchAll)
            {
                return lastSegment;
            }

            return null;
        }

        public override Matcher Build()
        {
            var root = BuildDfaTree();

            var states = new List<State>();
            var tables = new List<JumpTableBuilder>();
            AddNode(root, states, tables);

            var exit = states.Count;
            states.Add(new State()
            {
                Candidates = Array.Empty<Candidate>(),
                CandidateIndices = Array.Empty<int>(),
                CandidateGroups = Array.Empty<int>(),
            });
            tables.Add(new JumpTableBuilder() { Default = exit, Exit = exit, });

            for (var i = 0; i < tables.Count; i++)
            {
                if (tables[i].Default == -1)
                {
                    tables[i].Default = exit;
                }

                if (tables[i].Exit == -1)
                {
                    tables[i].Exit = exit;
                }
            }

            for (var i = 0; i < states.Count; i++)
            {
                states[i] = new State()
                {
                    Candidates = states[i].Candidates,
                    CandidateIndices = states[i].CandidateIndices,
                    CandidateGroups = states[i].CandidateGroups,
                    Transitions = tables[i].Build(),
                };
            }

            return new DfaMatcher(states.ToArray());
        }

        private int AddNode(DfaNode node, List<State> states, List<JumpTableBuilder> tables)
        {
            node.Matches.Sort();

            var index = states.Count;
            states.Add(new State()
            {
                Candidates = node.Matches.Select(CreateCandidate).ToArray(),
                CandidateIndices = Enumerable.Range(0, node.Matches.Count).ToArray(),
                CandidateGroups = CreateCandidateGroups(node),
            });

            var table = new JumpTableBuilder() { Default = -1, Exit = -1, };
            tables.Add(table);

            foreach (var kvp in node.Literals)
            {
                if (kvp.Key == null)
                {
                    continue;
                }
                
                var transition = Transition(kvp.Value);
                table.AddEntry(kvp.Key, transition);
            }

            if (node.Parameters != null && 
                node.CatchAll != null && 
                ReferenceEquals(node.Parameters, node.CatchAll))
            {
                // This node has a single transition to but it should accept zero-width segments
                // this can happen when a node only has catchall parameters.
                table.Default = Transition(node.Parameters);
                table.Exit = table.Default;
            }
            else if (node.Parameters != null && node.CatchAll != null)
            {
                // This node has a separate transition for zero-width segments
                // this can happen when a node has both parameters and catchall parameters.
                table.Default = Transition(node.Parameters);
                table.Exit = Transition(node.CatchAll);
            }
            else if (node.Parameters != null)
            {
                // This node has paramters but no catchall.
                table.Default = Transition(node.Parameters);
            }
            else if (node.CatchAll != null)
            {
                // This node has a catchall but no parameters
                table.Default = Transition(node.CatchAll);
                table.Exit = table.Default;
            }

            return index;

            int Transition(DfaNode next)
            {
                // Break cycles
                return ReferenceEquals(node, next) ? index : AddNode(next, states, tables);
            }
        }

        private Candidate CreateCandidate(MatcherBuilderEntry entry)
        {
            var processors = new List<MatchProcessor>();
            for (var i = 0; i < entry.Pattern.Segments.Count; i++)
            {
                var segment = entry.Pattern.Segments[i];
                if (segment.IsSimple && segment.Parts[0].IsParameter)
                {
                    var hasDefaultValue =
                        entry.Endpoint.Defaults.TryGetValue(segment.Parts[0].Name, out var @default) ||
                        segment.Parts[0].IsCatchAll ||
                        segment.Parts[0].DefaultValue != null;

                    @default = @default ?? segment.Parts[0].DefaultValue;

                    var processor = new ParameterSegmentMatchProcessor(
                        i,
                        segment.Parts[0].Name,
                        segment.Parts[0].IsCatchAll,
                        hasDefaultValue,
                        @default);
                    processors.Add(processor);
                }
                else if (!segment.IsSimple)
                {
                    var processor = new ComplexSegmentMatchProcessor(i, segment);
                    processors.Add(processor);
                }
            }

            for (var i = 0; i < entry.Pattern.Parameters.Count; i++)
            {
                var parameter = entry.Pattern.Parameters[i];
                foreach (var text in parameter.InlineConstraints)
                {
                    var constraint = _constraintResolver.ResolveConstraint(text.Constraint);
                    if (parameter.IsOptional)
                    {
                        constraint = new OptionalRouteConstraint(constraint);
                    }

                    var processor = new RouteConstraintMatchProcessor(parameter.Name, constraint);
                    processors.Add(processor);
                }
            }

            foreach (var kvp in entry.Endpoint.Defaults)
            {
                if (entry.Pattern.GetParameter(kvp.Key) == null)
                {
                    // This is a default that doesn't match a parameters
                    processors.Add(new DefaultValueMatchProcessor(kvp.Key, kvp.Value));
                }
            }

            if (entry.HttpMethod != null)
            {
                processors.Insert(0, new HttpMethodMatchProcessor(entry.HttpMethod));
            }

            return new Candidate(entry.Endpoint, processors.ToArray());
        }

        private static int[] CreateCandidateGroups(DfaNode node)
        {
            if (node.Matches.Count == 0)
            {
                return Array.Empty<int>();
            }

            var groups = new List<int>();

            var order = node.Matches[0].Order;
            var precedence = node.Matches[0].Precedence;
            var httpMethodScore = GetHttpMethodScore(node.Matches[0].Endpoint);
            var length = 1;

            for (var i = 1; i < node.Matches.Count; i++)
            {
                if (node.Matches[i].Order != order ||
                    node.Matches[i].Precedence != precedence ||
                    GetHttpMethodScore(node.Matches[i].Endpoint) != httpMethodScore)
                {
                    groups.Add(length);
                    length = 0;
                }

                length++;
            }

            groups.Add(length);

            return groups.ToArray();

            int GetHttpMethodScore(Endpoint endpoint)
            {
                if (endpoint.Metadata.OfType<HttpMethodEndpointConstraint>().Any())
                {
                    return 1;
                }

                return 0;
            }
        }

        private static bool HasAdditionalRequiredSegments(MatcherBuilderEntry entry, int depth)
        {
            for (var i = depth; i < entry.Pattern.Segments.Count; i++)
            {
                var segment = entry.Pattern.Segments[i];
                if (!segment.IsSimple)
                {
                    // Complex segments always require more processing
                    return true;
                }

                if (segment.Parts[0].IsLiteral)
                {
                    return true;
                }
                
                if (!segment.Parts[0].IsOptional &&
                    !segment.Parts[0].IsCatchAll &&
                    segment.Parts[0].DefaultValue == null &&
                    !entry.Endpoint.Defaults.ContainsKey(segment.Parts[0].Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
