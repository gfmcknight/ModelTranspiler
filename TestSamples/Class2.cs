using System;
using System.Collections.Generic;
using System.Text;

namespace TestSamples
{
    [Transpile]
    class Class2
    {
        [JsonProperty("differentNumberName")]
        public int MyNumber { get; private set; }
        [JsonIgnore]
        public double MyOtherProp { get; set; }
    }
}
