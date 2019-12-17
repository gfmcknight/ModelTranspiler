using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;

namespace TestSamples
{
    [Transpile]
    class SubTypeB : BaseType
    {
        public int SubTypeBIntField { get; set; }
    }
}
