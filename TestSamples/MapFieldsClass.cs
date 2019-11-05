using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;

namespace TestSamples
{
    [Transpile]
    class MapFieldsClass
    {
        // We only care about certain sets of maps, and in particular
        // model-to-model maps aren't very useful, so aren't tested

        // Generic JSON (doesn't convert to models)
        public Dictionary<string, JObject> StringToObject { get; set; }

        // This should actually convert to models
        public Dictionary<string, Class2> StringToModel { get; set; }

        // The one is also likely to appear in the wild
        public Dictionary<string, string> StringToString { get; set; }

        public static object DefaultValue()
        {
            return new MapFieldsClass
            {
                StringToObject = new Dictionary<string, JObject>
                {
                    { "EmptyObject", new JObject() },
                    { "ValuesObject", 
                        new JObject
                        {
                            { "IntVal", 5 },
                            { "StringVal", "Hello, world" },
                            { "ArrayVal", new JArray(3, 4, 5) }
                        }
                    }
                },

                StringToModel = new Dictionary<string, Class2>
                {
                    { "ModelA", new Class2{ MyNumber = 6 } },
                    { "ModelB", new Class2{ MyNumber = 3 } }
                },

                StringToString = new Dictionary<string, string>
                {
                    { "Hello", "world!" },
                    { "LG", "TM!" }
                }
            };
        }
    }
}
