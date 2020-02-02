using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;

namespace TestSamples
{
    [Transpile]
    class AllValueTypesClass
    {
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public string StringProperty { get; set; }
        public Guid GuidProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public double DoubleProperty { get; set; }
        public long LongProperty { get; set; }

        public JObject JsonObjectProperty { get; set; }

        public static object DefaultValue()
        {
            return new AllValueTypesClass
            {
                IntProperty = 11,
                BoolProperty = true,
                StringProperty = "test",
                GuidProperty = Guid.Parse("8bd30961-c052-457f-b78e-dcd64f50c277"),
                DateTimeProperty = new DateTime(2019, 6, 26, 11, 11, 11),
                DoubleProperty = 333.7,
                LongProperty = 24449929900,
                JsonObjectProperty = new JObject
                {
                    { "IntVal", 5 },
                    { "StringVal", "Hello, world" },
                    { "ArrayVal", new JArray(3, 4, 5) }
                }
            };
        }
    }
}
