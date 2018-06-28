using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing
{
    public interface IEndpointMatchConstraintMapProvider
    {
        IDictionary<string, Type> GetConstraintMap();
    }
}
