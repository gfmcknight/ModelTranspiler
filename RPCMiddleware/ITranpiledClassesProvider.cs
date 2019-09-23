using System;
using System.Collections.Generic;
using System.Text;

namespace RPCMiddleware
{
    public interface ITranpiledClassesProvider
    {
        //IEnumerator<Type> GetEnumerator();
        Type GetFromName(string name);
    }
}
