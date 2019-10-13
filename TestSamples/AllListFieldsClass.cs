using System;
using System.Collections.Generic;
using System.Text;
using TranspilerDirectives;

namespace TestSamples
{
    [Transpile]
    class AllListFieldsClass
    {
        public List<int> IntListProperty { get; set; }
        public List<bool> BoolListProperty { get; set; }
        public List<string> StringListProperty { get; set; }
        public List<Guid> GuidListProperty { get; set; }
        public List<DateTime> DateTimeListProperty { get; set; }
        public List<double> DoubleListProperty { get; set; }

        public List<List<int>> NestedListProperty { get; set; }

        public static object DefaultValue()
        {
            return new AllListFieldsClass
            {
                IntListProperty = new List<int>(new int[] { 0, 1, 2, 3 }),
                BoolListProperty = new List<bool>(new bool[] { true, true, false }),
                StringListProperty = new List<string>(new string[] { "hello", "world" }),
                GuidListProperty = new List<Guid>(
                    new Guid[] { Guid.Empty, Guid.Parse("8bd30961-c052-457f-b78e-dcd64f50c277") }),
                DateTimeListProperty = new List<DateTime>(new DateTime[] {
                    new DateTime(2019, 6, 26, 11, 11, 11),
                    new DateTime(2019, 10, 12, 8, 22, 32)
                }),
                DoubleListProperty = new List<double>(new double[]{ 30.5, 44.4, 88.6 }),
                NestedListProperty = new List<List<int>>(new List<int>[]
                {
                    new List<int>(new int[] {1, 2}),
                    new List<int>(new int[] {3, 4})
                })

            };
        }
    }
}
