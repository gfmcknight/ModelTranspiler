using System;
using System.Collections.Generic;
using System.Text;

namespace TestSamples
{
    [Transpile]
    class Class5
    {
        public Class2 Class2Prop { get; set; }

        public static object DefaultValue()
        {
            return new Class5
            {
                Class2Prop = new Class2
                {
                    MyNumber = 4
                }
            };
        }
    }
}
