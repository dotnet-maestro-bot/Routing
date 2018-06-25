// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Template;
using static Microsoft.AspNetCore.Routing.Matchers.InstructionMatcher;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class InstructionMatcherBuilder : MatcherBuilder
    {
        private List<MatcherBuilderEntry> _entries = new List<MatcherBuilderEntry>();

        public override void AddEndpoint(MatcherEndpoint endpoint)
        {
            var parsed = TemplateParser.Parse(endpoint.Template);
            _entries.Add(new MatcherBuilderEntry(endpoint));
        }

        public override Matcher Build()
        {
            _entries.Sort();

            var roots = new List<OrderNode>();

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];

                var parent = (SequenceNode)GetOrCreateRootNode(roots, entry.Order);

                var depth = 0;
                for (; depth < entry.Pattern.Segments.Count; depth++)
                {
                    var segment = entry.Pattern.Segments[depth];
                    if (segment.IsSimple && segment.Parts[0].IsLiteral)
                    {
                        var text = segment.Parts[0].Text;
                        var branch = parent.GetNode<BranchNode>() ?? parent.AddNode(new BranchNode(depth));

                        var index = -1;
                        for (var j = 0; j < branch.Literals.Count; j++)
                        {
                            if (string.Equals(text, branch.Literals[j], StringComparison.OrdinalIgnoreCase))
                            {
                                index = j;
                                break;
                            }
                        }

                        if (index == -1)
                        {
                            branch.Literals.Add(text);
                            branch.AddNode(new SequenceNode(depth + 1));
                            index = branch.Children.Count - 1;
                        }

                        parent = (SequenceNode)branch.Children[index];
                    }
                    else if (segment.IsSimple && segment.Parts[0].IsParameter)
                    {
                        var branch = parent.GetNode<BranchNode>() ?? parent.AddNode(new BranchNode(depth));

                        var parameter = branch.GetNode<ParameterNode>();
                        if (parameter == null)
                        {
                            parameter = new ParameterNode(depth + 1);
                            branch.AddNode(parameter);
                            branch.Literals.Add(null);
                        }

                        parent = (SequenceNode)parameter;
                    }
                    else
                    {
                        throw new InvalidOperationException("Not implemented!");
                    }
                }

                parent.AddNode(new AcceptNode(depth, entry.Endpoint));
            }

            var builder = new InstructionBuilder();
            for (var i = 0; i < roots.Count; i++)
            {
                roots[i].Lower(builder);
            }

            var (instructions, endpoints, tables) = builder;
            var candidates = new Candidate[endpoints.Length];
            for (var i = 0; i < endpoints.Length; i++)
            {
                candidates[i] = CreateCandidate(endpoints[i]);
            }

            return new InstructionMatcher(instructions, candidates, tables);
        }

        private OrderNode GetOrCreateRootNode(List<OrderNode> roots, int order)
        {
            OrderNode root = null;
            for (var j = 0; j < roots.Count; j++)
            {
                if (roots[j].Order == order)
                {
                    root = roots[j];
                    break;
                }
            }

            if (root == null)
            {
                // Nodes are guaranteed to be in order because the entries are in order.
                root = new OrderNode(order);
                roots.Add(root);
            }

            return root;
        }

        private static Candidate CreateCandidate(MatcherEndpoint endpoint)
        {
            var parsed = TemplateParser.Parse(endpoint.Template);

            var processors = new List<MatchProcessor>();
            for (var i = 0; i < parsed.Segments.Count; i++)
            {
                var segment = parsed.Segments[i];
                if (segment.IsSimple && segment.Parts[0].IsParameter)
                {
                    var processor = new ParameterSegmentMatchProcessor(
                        i,
                        segment.Parts[0].Name,
                        segment.Parts[0].IsCatchAll,
                        segment.Parts[0].IsCatchAll || segment.Parts[0].DefaultValue != null,
                        segment.Parts[0].DefaultValue);
                    processors.Add(processor);
                }
            }

            var httpMethod = endpoint.Metadata.GetMetadata<HttpMethodEndpointConstraint>()?.HttpMethods.SingleOrDefault();
            if (httpMethod != null)
            {
                processors.Insert(0, new HttpMethodMatchProcessor(httpMethod));
            }

            return new Candidate(endpoint, processors.ToArray());
        }
        private class InstructionBuilder
        {
            private readonly List<Instruction> _instructions = new List<Instruction>();
            private readonly List<MatcherEndpoint> _endpoints = new List<MatcherEndpoint>();
            private readonly List<JumpTableBuilder> _tables = new List<JumpTableBuilder>();

            private readonly List<int> _blocks = new List<int>();

            public int Next => _instructions.Count;

            public void BeginBlock()
            {
                _blocks.Add(Next);
            }

            public void EndBlock()
            {
                var start = _blocks[_blocks.Count - 1];
                var end = Next;
                for (var i = start; i < end; i++)
                {
                    if (_instructions[i].Code == InstructionCode.Pop)
                    {
                        _instructions[i] = new Instruction()
                        {
                            Code = InstructionCode.Jump,
                            Depth = _instructions[i].Depth,
                            Payload = end,
                        };
                    }
                }

                _blocks.RemoveAt(_blocks.Count - 1);
            }

            public int AddInstruction(Instruction instruction)
            {
                _instructions.Add(instruction);
                return _instructions.Count - 1;
            }

            public int AddEndpoint(MatcherEndpoint endpoint)
            {
                _endpoints.Add(endpoint);
                return _endpoints.Count - 1;
            }

            public int AddJumpTable(JumpTableBuilder table)
            {
                _tables.Add(table);
                return _tables.Count - 1;
            }

            public void Deconstruct(
                out Instruction[] instructions,
                out MatcherEndpoint[] endpoints,
                out JumpTable[] tables)
            {
                instructions = _instructions.ToArray();
                endpoints = _endpoints.ToArray();

                tables = new JumpTable[_tables.Count];
                for (var i = 0; i < _tables.Count; i++)
                {
                    tables[i] = _tables[i].Build();
                }
            }
        }

        private abstract class Node
        {
            public int Depth { get; protected set; }
            public List<Node> Children { get; } = new List<Node>();

            public abstract void Lower(InstructionBuilder builder);

            public TNode GetNode<TNode>() where TNode : Node
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    if (Children[i] is TNode match)
                    {
                        return match;
                    }
                }

                return null;
            }

            public TNode AddNode<TNode>(TNode node) where TNode : Node
            {
                // We already ordered the routes into precedence order
                Children.Add(node);
                return node;
            }
        }

        private class SequenceNode : Node
        {
            public SequenceNode(int depth)
            {
                Depth = depth;
            }

            public override void Lower(InstructionBuilder builder)
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Lower(builder);
                }
            }
        }

        private class OrderNode : SequenceNode
        {
            public OrderNode(int order)
                : base(0)
            {
                Order = order;
            }

            public int Order { get; }

            public override void Lower(InstructionBuilder builder)
            {
                builder.BeginBlock();

                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Lower(builder);
                }

                builder.EndBlock();
            }
        }

        private class BranchNode : Node
        {
            public BranchNode(int depth)
            {
                Depth = depth;
            }

            public List<string> Literals { get; } = new List<string>();

            public override void Lower(InstructionBuilder builder)
            {
                var table = new JumpTableBuilder() { Default = -1, Exit = -1, };
                var index = builder.AddJumpTable(table);
                builder.AddInstruction(new Instruction()
                {
                    Code = InstructionCode.Branch,
                    Depth = (byte)Depth,
                    Payload = index
                });

                builder.BeginBlock();

                for (var i = 0; i < Children.Count; i++)
                {
                    if (Literals[i] != null)
                    {
                        table.AddEntry(Literals[i], builder.Next);
                        Children[i].Lower(builder);
                        builder.AddInstruction(new Instruction()
                        {
                            Code = InstructionCode.Pop,
                            Depth = (byte)Depth,
                        });
                    }
                    else
                    {
                        table.Default = builder.Next;
                        Children[i].Lower(builder);
                        builder.AddInstruction(new Instruction()
                        {
                            Code = InstructionCode.Pop,
                            Depth = (byte)Depth,
                        });
                    }
                }

                builder.EndBlock();

                if (table.Default == -1)
                {
                    table.Default = builder.Next;
                }

                table.Exit = builder.Next;
            }
        }

        private class ParameterNode : SequenceNode
        {
            public ParameterNode(int depth)
                : base(depth)
            {
            }
        }

        private class AcceptNode : Node
        {
            public AcceptNode(int depth, MatcherEndpoint endpoint)
            {
                Depth = depth;
                Endpoint = endpoint;
            }

            public MatcherEndpoint Endpoint { get; }

            public override void Lower(InstructionBuilder builder)
            {
                builder.AddInstruction(new Instruction()
                {
                    Code = InstructionCode.Accept,
                    Depth = (byte)Depth,
                    Payload = builder.AddEndpoint(Endpoint),
                });
            }
        }
    }
}
