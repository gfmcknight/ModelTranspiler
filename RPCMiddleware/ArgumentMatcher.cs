using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RPCMiddleware
{
    public class ArgumentMatcher
    {
        protected IServiceProvider services;

        public ArgumentMatcher(IServiceProvider provider)
        {
            services = provider;
        }

        public virtual object[] DeriveArguments(string[] providedArgs, MethodInfo method)
        {
            ParameterInfo[] paramInfos = method.GetParameters();
            object[] arguments = new object[paramInfos.Length];

            int providedArgIndex = 0;
            for (int i = 0; i < arguments.Length; i++)
            {
                if (paramInfos[i].GetCustomAttribute(typeof(FromServicesAttribute)) != null)
                {
                    arguments[i] = services.GetService(paramInfos[i].ParameterType);
                    continue;
                }

                if (providedArgs[providedArgIndex] != null)
                {
                    arguments[i] = JsonConvert.DeserializeObject(
                        providedArgs[providedArgIndex], 
                        paramInfos[i].ParameterType
                    );
                }
                providedArgIndex++;
            }

            return arguments;
        }
    }
}
