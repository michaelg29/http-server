using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HttpServer.WebServer
{
    public class ArgType
    {
        private static IList<ArgType> mappings = new List<ArgType>
        {
            NewArgType<string>(s => s, "str"),
            NewArgType<Guid>(s => (object)Guid.Parse(s)),
            NewArgType<double>(s => (object)double.Parse(s)),
            NewArgType<int>(s => (object)int.Parse(s), "int")
        };

        private static ArgType NewArgType<T>(Func<string, object> parser, params string[] altNames)
        {
            return new ArgType(typeof(T), parser, altNames);
        }

        public static void RegisterType<T>(Func<string, object> parser, params string[] altNames)
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                if (mappings[i].Type == typeof(T))
                {
                    mappings.RemoveAt(i);
                }
            }

            mappings.Add(NewArgType<T>(parser, altNames));
        }

        public static ArgType GetArgType(Type argType)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.Type == argType)
                {
                    return mapping;
                }
            }

            return null;
        }

        public static ArgType GetArgType(string argTypeStr)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.AltNames.Contains(argTypeStr))
                {
                    return mapping;
                }
            }

            return null;
        }

        public static bool TryParse(string valStr, out object val, out Type type)
        {
            // priority at top
            for (int i = mappings.Count - 1; i >= 0; i--)
            {
                var mapping = mappings[i];
                try
                {
                    val = mapping.Parser(valStr);
                    type = mapping.Type;
                    return true;
                }
                catch
                {
                    continue;
                }
            }

            val = null;
            type = null;
            return false;
        }

        public static bool TryParse<T>(string valStr, out T val)
        {
            try
            {
                val = GetArgType(typeof(T)).Parse<T>(valStr);
                return true;
            }
            catch
            {
                val = default;
                return false;
            }
        }

        public T Parse<T>(string valStr)
        {
            return (T)Parser(valStr);
        }

        public Type Type { get; private set; }
        public Func<string, object> Parser { get; private set; }
        public HashSet<string> AltNames { get; private set; }

        private ArgType(Type type, Func<string, object> Parser, IList<string> altNames = null)
        {
            this.Type = type;
            this.Parser = Parser;

            this.AltNames = new HashSet<string>();
            foreach (string name in altNames)
            {
                this.AltNames.Add(name);
            }
            this.AltNames.Add(type.Name);
        }
    }
}
