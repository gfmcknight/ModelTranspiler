using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;
using static JsonSubTypes.JsonSubtypes;

namespace TestSamples
{
    [Transpile]
    [JsonConverter(typeof(JsonSubtypes), "SubTypeADiscriminatingField")]
    [KnownSubType(typeof(SubASubTypeA), 1)]
    [KnownSubType(typeof(SubASubTypeA), 2)]
    class SubTypeA : BaseType
    {
        public override string BaseDiscriminatingField { get; } = "Simple";
        public string SubTypeAStringField { get; set; }
        public int SubTypeADiscriminatingField { get; set; }
    }
}
