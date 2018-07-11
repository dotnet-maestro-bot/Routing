// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    public class CompositeEndpointDataSource : EndpointDataSource
    {
        private readonly EndpointDataSource[] _dataSources;
        private readonly object _lock;
        private IReadOnlyList<Endpoint> _endpoints;

        private IChangeToken _dataSourcesChangeToken;
        private IChangeToken _consumerChangeToken;
        private CancellationTokenSource _cts;

        internal CompositeEndpointDataSource(IEnumerable<EndpointDataSource> dataSources)
        {
            if (dataSources == null)
            {
                throw new ArgumentNullException(nameof(dataSources));
            }

            _dataSources = dataSources.ToArray();
            _lock = new object();
        }

        public override IChangeToken ChangeToken
        {
            get
            {
                EnsureInitialized();
                return _consumerChangeToken;
            }
        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                EnsureInitialized();
                return _endpoints;
            }
        }

        // Defer initialization to avoid doing lots of reflection on startup.
        private void EnsureInitialized()
        {
            if (_dataSourcesChangeToken == null)
            {
                Initialize();
            }
        }

        // Note: we can't use DataSourceDependantCache here because we also need to handle a list of change
        // tokens, which is a complication most of our code doesn't have.
        private void Initialize()
        {
            lock (_lock)
            {
                _cts = new CancellationTokenSource();
                _consumerChangeToken = new CancellationChangeToken(_cts.Token);
                _dataSourcesChangeToken = new CompositeChangeToken(_dataSources.Select(d => d.ChangeToken).ToArray());
                _endpoints = _dataSources.SelectMany(d => d.Endpoints).ToArray();

                _dataSourcesChangeToken.RegisterChangeCallback(
                    (state) =>
                    {
                        // raise consumer callbacks
                        _cts.Cancel();

                        Initialize();
                    },
                    state: null);
            }
        }
    }
}
