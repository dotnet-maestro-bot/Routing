// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing.TestObjects
{
    internal class TestChangeToken : IChangeToken
    {
        private Action _callback;

        public bool ActiveChangeCallbacks => true;

        public bool HasChanged { get; private set; }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            _callback = () => callback(state);
            return new NoopDisposable();
        }

        public void Changed()
        {
            HasChanged = true;
            _callback();
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
