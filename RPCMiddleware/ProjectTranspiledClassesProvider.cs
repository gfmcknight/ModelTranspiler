using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using TranspilerDirectives;

namespace RPCMiddleware
{
    public class ProjectTranspiledClassesProvider : ITranpiledClassesProvider
    {
        private Assembly assembly;
        private Dictionary<string, Type> typesBySimpleName;

        /// <summary>
        /// An abstraction for retrieving types from a project by short name.
        /// </summary>
        /// <param name="sentinelType">A single type from the project for which this class provides.</param>
        public ProjectTranspiledClassesProvider(Type sentinelType)
        {
            assembly = Assembly.GetAssembly(sentinelType);
        }

        public Type GetFromName(string name)
        {
            if (typesBySimpleName == null)
            {
                LoadAllClasses();
            }

            return typesBySimpleName[name];
        }

        public void LoadAllClasses()
        {
            IEnumerable<Type> transpiledTypes = assembly.GetExportedTypes()
                .Where(type => type.GetCustomAttribute<TranspileAttribute>() != null);

            typesBySimpleName = new Dictionary<string, Type>();

            foreach (Type type in transpiledTypes)
            {
                typesBySimpleName.Add(type.Name, type);
            }
        }
    }
}
