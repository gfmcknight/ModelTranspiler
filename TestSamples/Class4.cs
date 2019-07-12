using System;
using System.Collections.Generic;
using System.Text;

namespace TestSamples
{
    [Transpile]
    public class Class4
    {
        public Class1 Class1Prop { get; set; }

        public static object DefaultValue()
        {
            return new Class4
            {
                Class1Prop = new Class1
                {
                    MyOtherProp = 5
                }
            };
        }
    }
}
