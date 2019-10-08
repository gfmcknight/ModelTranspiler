using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TranspilerDirectives;

namespace TestSamples
{
    [Transpile]
    public class MethodsClass
    {
        public int MyValue { get; set; }

        [TranspileDirect(@"
this.MyValue = value;
return value;
")]
        public int SetValue(int value)
        {
            MyValue = value;
            return value;
        }

        [TranspileRPC]
        public int SetValueRemote(int value)
        {
            MyValue = value;
            return value;
        }

        [TranspileRPC]
        public async Task<int> SetValueRemoteAsync(int value)
        {
            MyValue = value;
            await Task.Delay(10);
            return MyValue;
        }
    }
}
