using System;
using System.Collections.Generic;
using System.Text;

namespace TestSamples
{
    [Transpile]
    public class Class1
    {
        public int MyNumber { get; private set; }
        public double MyOtherProp { get; set; }

        public static object DefaultValue()
        {
            return new Class1
            {
                MyOtherProp = 7
            };
        }
    }
}
