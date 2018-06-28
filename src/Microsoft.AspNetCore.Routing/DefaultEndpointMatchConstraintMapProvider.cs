using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    internal class DefaultEndpointMatchConstraintMapProvider : IEndpointMatchConstraintMapProvider
    {
        private readonly DispatcherOptions _options;

        public DefaultEndpointMatchConstraintMapProvider(IOptions<DispatcherOptions> options)
        {
            _options = options.Value;
        }

        public IDictionary<string, Type> GetConstraintMap()
        {
            return _options.ConstraintMap;
        }
    }
}
