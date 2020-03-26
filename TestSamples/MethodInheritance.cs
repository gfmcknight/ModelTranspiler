using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;

namespace TestSamples
{
    [Transpile]
    public class BaseMethodClass
    {
        public int Value { get; set; }
        
        [TranspileRPC]
        public virtual void BaseMethod(int newVal)
        {
            throw new NotImplementedException();
        }

        [TranspileRPC]
        public void NonVirtualBaseMethod(int newVal)
        {
            Value = newVal;
        }
    }

    [Transpile]
    public class InheritedMethodClass : BaseMethodClass
    {
        public override void BaseMethod(int newVal)
        {
            Value = newVal;
        }
    }

}
