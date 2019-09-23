using System;

namespace TranspilerDirectives
{
    public class TranspileDirectAttribute : Attribute
    {
        private string v;

        public TranspileDirectAttribute(string v)
        {
            this.v = v;
        }
    }
}