using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestSamples;

namespace RoundTripper
{
    class Program
    {
        static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            string assemblyName = typeof(Class1).Assembly.FullName;

            bool creatingFile = args.Length > 1 && args[1] == "create";
            List<string> failedRoundTrips = new List<string>();

            using (FileStream fileStream = File.OpenRead(Path.Join(args[0], "roundtrip-classes.txt")))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (creatingFile)
                        {
                            Console.WriteLine(line);
                            CreateDefaultAndWrite(line, args[0], assemblyName);
                        }
                        else
                        {
                            if (!CheckDefault(line, args[0], assemblyName))
                            {
                                failedRoundTrips.Add(line);
                            }
                        }
                    }
                }
            }

            if (creatingFile)
            {
                return;
            }

            if (failedRoundTrips.Count != 0)
            {
                Console.WriteLine("FAILED the following round-trips:");
                foreach (string className in failedRoundTrips)
                {
                    Console.WriteLine(className);
                }
            }
            else
            {
                Console.WriteLine("All roundtripping OK");
            }
        }

        private static void CreateDefaultAndWrite(string line, string root, string assemblyName)
        {
            Type type = Type.GetType(line + ", " + assemblyName);
            string filename = Path.Join(root, "roundtrip", line + ".json");
            using (FileStream fileStream = File.Open(filename, FileMode.OpenOrCreate)) {
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    writer.Write(JsonConvert.SerializeObject(GetDefaultObject(type)));
                }
            }
        }

        private static bool CheckDefault(string line, string root, string assemblyName)
        {
            Type type = Type.GetType(line + ", " + assemblyName);
            
            // The file that was created by the JS roundtripper has the fromjs suffix
            // so that both can be observed when debugging
            string filename = Path.Join(root, "roundtrip", line + "-fromjs.json");
            object roundTrippedObject;

            using (FileStream fileStream = File.OpenRead(filename))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string json = reader.ReadToEnd();
                    roundTrippedObject = JsonConvert.DeserializeObject(json, type);
                }
            }

            return IsSame(GetDefaultObject(type), roundTrippedObject);
        }

        private static object GetDefaultObject(Type type)
        {
            MethodInfo info = type.GetMethod("DefaultValue");
            // Assume the method is static and takes no arguments
            object defaultValue = info.Invoke(null, Array.Empty<object>());
            return defaultValue;
        }

        private static bool IsSame(object a, object b)
        {
            if (a == null)
            {
                return b == null;
            }

            if (a.GetType() != b.GetType())
            {
                return false;
            }

            PropertyInfo[] properties = a.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (!IsSameValue(property.GetValue(a), property.GetValue(b)))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsSameValue(object val1, object val2)
        {
            switch (val1)
            {
                case int i:
                    return i == (int)val2;
                case bool b:
                    return b == (bool)val2;
                case string s:
                    return s == (string)val2;
                case DateTime dt:
                    return dt == (DateTime)val2;
                case double d:
                    return d == (double)val2;
                case Guid g:
                    return g == (Guid)val2;
                case JObject j1:
                    JObject j2 = val2 as JObject;
                    return JToken.DeepEquals(j1, j2);
                case IDictionary d1:
                    IDictionary d2 = val2 as IDictionary;
                    if (d1.Count != d2.Count)
                    {
                        return false;
                    }
                    foreach (object key in d1.Keys)
                    {
                        if (!d2.Contains(key) || !IsSameValue(d1[key], d2[key]))
                        {
                            return false;
                        }
                    }
                    return true;
                case IList l1:
                    IList l2 = val2 as IList;
                    if (l1.Count != l2.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < l1.Count; i++)
                    {
                        if (!IsSameValue(l1[i], l2[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                default:
                    return IsSame(val1, val2); // Assume we have a same object
            }
        }
    }
}
