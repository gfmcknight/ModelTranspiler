using System;
using System.Collections.Generic;
using System.Text;

namespace TestSamples
{
    [Transpile]
    class MethodsClass
    {
        public int MyValue { get; set; }

        [TranspileDirect(@"
this.MyValue = value;
return value;
")]
        public int SetValue(int value)
        {
            MyValue = value;
            return value;
        }

        [TranspileRPC]
        public int SetValueRemote(int value)
        {
            MyValue = value;
            return value;
        }

    }
}
