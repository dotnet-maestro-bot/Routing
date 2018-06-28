using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Routing
{
    public interface IInlineEndpointMatchConstraintResolver
    {
        IEndpointMatchConstraint ResolveConstraint(string inlineConstraint);
    }
}
