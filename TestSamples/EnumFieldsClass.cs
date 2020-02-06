using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;

namespace TestSamples
{

    [JsonConverter(typeof(StringEnumConverter))]
    public enum NonTranspiledStringEnum
    {
        NTSEValueA,
        NTSEValueB,
        NTSEValueC
    }

    public enum NonTranspiledIntEnum
    {
        NTIEValueA = 3,
        NTIEValueB,
        NTIEValueC
    }

    [Transpile]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TranspiledStringEnum
    {
        TSEValueA,
        TSEValueB,
        TSEValueC
    }

    [Transpile]
    public enum TranspiledIntEnum
    {
        TIEValueA = 3,
        TIEValueB,
        TIEValueC
    }

    [Transpile]
    public class EnumFieldsClass
    {
        [TranspiledType("string")]
        public NonTranspiledStringEnum NTSE { get; set; }

        [TranspiledType("int")]
        public NonTranspiledIntEnum NTIE { get; set; }


        public TranspiledStringEnum TSE { get; set; }
        public TranspiledIntEnum TIE { get; set; }

        [TranspileRPC]
        [TranspiledType("string")]
        public NonTranspiledStringEnum GetNextNTSE(
            [TranspiledType("string")] NonTranspiledStringEnum current)
        {
            switch (current)
            {
                case NonTranspiledStringEnum.NTSEValueA:
                    return NonTranspiledStringEnum.NTSEValueB;
                case NonTranspiledStringEnum.NTSEValueB:
                    return NonTranspiledStringEnum.NTSEValueC;
                case NonTranspiledStringEnum.NTSEValueC:
                    return NonTranspiledStringEnum.NTSEValueA;
            }
            return NonTranspiledStringEnum.NTSEValueA;
        }

        [TranspileRPC]
        [TranspiledType("int")]
        public NonTranspiledIntEnum GetNextNTIE(
            [TranspiledType("int")] NonTranspiledIntEnum current)
        {
            switch (current)
            {
                case NonTranspiledIntEnum.NTIEValueA:
                    return NonTranspiledIntEnum.NTIEValueB;
                case NonTranspiledIntEnum.NTIEValueB:
                    return NonTranspiledIntEnum.NTIEValueC;
                case NonTranspiledIntEnum.NTIEValueC:
                    return NonTranspiledIntEnum.NTIEValueA;
            }
            return NonTranspiledIntEnum.NTIEValueA;
        }

        [TranspileRPC]
        public TranspiledStringEnum GetNextTSE(TranspiledStringEnum current)
        {
            switch (current)
            {
                case TranspiledStringEnum.TSEValueA:
                    return TranspiledStringEnum.TSEValueB;
                case TranspiledStringEnum.TSEValueB:
                    return TranspiledStringEnum.TSEValueC;
                case TranspiledStringEnum.TSEValueC:
                    return TranspiledStringEnum.TSEValueA;
            }
            return TranspiledStringEnum.TSEValueA;
        }

        [TranspileRPC]
        public TranspiledIntEnum GetNextTIE(TranspiledIntEnum current)
        {
            switch (current)
            {
                case TranspiledIntEnum.TIEValueA:
                    return TranspiledIntEnum.TIEValueB;
                case TranspiledIntEnum.TIEValueB:
                    return TranspiledIntEnum.TIEValueC;
                case TranspiledIntEnum.TIEValueC:
                    return TranspiledIntEnum.TIEValueA;
            }
            return TranspiledIntEnum.TIEValueA;
        }

        public static object DefaultValue()
        {
            return new EnumFieldsClass
            {
                NTSE = NonTranspiledStringEnum.NTSEValueC,
                NTIE = NonTranspiledIntEnum.NTIEValueA,
                TSE = TranspiledStringEnum.TSEValueB,
                TIE = TranspiledIntEnum.TIEValueC
            };
        }


    }
}
