using System;

namespace TestSamples
{
    internal class TranspileDirectAttribute : Attribute
    {
        private string v;

        public TranspileDirectAttribute(string v)
        {
            this.v = v;
        }
    }
}