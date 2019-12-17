using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;
using JsonSubTypes;
using static JsonSubTypes.JsonSubtypes;

namespace TestSamples
{
    [Transpile]
    [JsonConverter(typeof(JsonSubtypes), "BaseDiscriminatingField")]
    [KnownSubType(typeof(SubTypeA), "A")]
    [KnownSubType(typeof(SubTypeB), "B")]
    class BaseType
    {
        public Guid BaseValueField { get; set; }
        public virtual string BaseDiscriminatingField { get; }
    }
}
