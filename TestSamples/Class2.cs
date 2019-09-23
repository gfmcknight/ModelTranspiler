using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;

namespace TestSamples
{
    [Transpile]
    public class Class2
    {
        [JsonProperty("differentNumberName")]
        public int MyNumber { get; set; }
        [JsonIgnore]
        public double MyOtherProp { get; set; }
    }
}
