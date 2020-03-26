using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TranspilerDirectives;

namespace RPCMiddleware
{
    public class RPCTranspilerMiddleware
    {
        private RequestDelegate next;

        public RPCTranspilerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITranpiledClassesProvider provider, ArgumentMatcher matcher)
        {
            if (!context.Request.Path.StartsWithSegments("/rpc"))
            {
                await next.Invoke(context);
                return;
            }

            // TODO: Find a better way to do this
            string[] pathParts = context.Request.Path.Value.Substring(1).Split('/');
            if (pathParts.Length < 3)
            {
                // Don't allow an empty rpc, for now
                context.Response.StatusCode = 400;
                return;
            }

            string className = pathParts[1];
            // The provider will handle filtering out of classes that
            // do not have the proper attribute, but we must check the
            // attribute on the methods
            Type classType = provider.GetFromName(className);
            if (classType == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            string methodName = pathParts[2];
            // For now, only a single method of a given name
            // can be RPC transpiled in order to be consistent
            // with the fact that only one method can be transpiled
            // directly
            MethodInfo method = classType.GetMethods().FirstOrDefault(
                m =>
                    m.GetCustomAttributes(
                        typeof(TranspileRPCAttribute)
                    ).ToList().Count > 0
                    && m.Name == methodName
            );

            if (method == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            string requestBody = new StreamReader(context.Request.Body).ReadToEnd();
            RPCMessage message = JsonConvert.DeserializeObject<RPCMessage>(
                requestBody
            );
            object thisObject = JsonConvert.DeserializeObject(
                JsonConvert.SerializeObject(message.ThisObject), classType);
            object[] args = matcher.DeriveArguments(message.Values.Select(
                value => JsonConvert.SerializeObject(value)).ToArray(), method);

            object returnValue;
            try {
                returnValue = await InvokeAndAwaitMethod(method, thisObject, args);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
                return;
            }

            // We need to send back both the new this and the return value in case
            // the object mutated its own state
            context.Response.StatusCode = 200;
            RPCReturn body = new RPCReturn
            {
                ThisObject = thisObject,
                ReturnValue = returnValue
            };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(body));
        }

        /// <summary>
        /// Invokes a method and if that method is asynchronous, waits on the completion
        /// of the method and returns the result.
        /// </summary>
        private async Task<object> InvokeAndAwaitMethod(MethodInfo method, object thisObject, object[] args)
        {
            object returnValue = method.Invoke(thisObject, args);

            if (returnValue is Task task)
            {
                await task.ConfigureAwait(false);
                PropertyInfo resultProperty = task.GetType().GetProperty("Result");
                // In the case of a function with a void return 
                if (resultProperty != null)
                {
                    returnValue = resultProperty.GetValue(task);
                }
             }

            return returnValue;
        }
    }

    internal class RPCMessage
    {
        public object ThisObject { get; set; }
        public object[] Values { get; set; }
    }

    internal class RPCReturn
    {
        public object ThisObject { get; set; }
        public object ReturnValue { get; set; }
    }
}
