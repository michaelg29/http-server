using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.WebServer
{
    /// <summary>
    /// Class to identify and parse arguments
    /// </summary>
    public class ArgType
    {
        /// <summary>
        /// Set of predefined types with their parsers
        /// </summary>
        private static IList<ArgType> argTypes = new List<ArgType>
        {
            new ArgType(typeof(string), s => s, "str"),
            new ArgType(typeof(Guid), s => Guid.Parse(s)),
            new ArgType(typeof(double), s => double.Parse(s), "float"),
            new ArgType(typeof(int), s => int.Parse(s), "int"),
            new ArgType(typeof(bool), s => bool.Parse(s), "bool")
        };

        /// <summary>
        /// Add a new type to the list
        /// </summary>
        /// <typeparam name="T">Type to parse</typeparam>
        /// <param name="parser">Parser callback</param>
        /// <param name="altNames">Alternate names to identify the type by</param>
        public static void RegisterType<T>(Func<string, object> parser, params string[] altNames)
        {
            // find existing type, otherwise add
            ArgType existingArgType = argTypes.Where(at => at.Type == typeof(T)).SingleOrDefault();
            if (existingArgType != null)
            {
                existingArgType.Parser = parser;
                existingArgType.AltNames.Concat(altNames);
            }
            else
            {
                argTypes.Add(new ArgType(typeof(T), parser, altNames));
            }
        }

        /// <summary>
        /// Parse a string into an argument
        /// </summary>
        /// <param name="valStr">String to parse</param>
        /// <param name="val">Parsed argument value</param>
        /// <param name="type">Parsed argument type</param>
        /// <returns>If the string was successfully parsed</returns>
        public static bool TryParse(string valStr, out object val, out Type type)
        {
            // priority at top of list
            for (int i = argTypes.Count - 1; i >= 0; i--)
            {
                var argType = argTypes[i];
                try
                {
                    val = argType.Parser(valStr);
                    type = argType.Type;
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

        /// <summary>
        /// Parse a string into an argument
        /// </summary>
        /// <typeparam name="T">Specific type to parse</typeparam>
        /// <param name="valStr">String to parse</param>
        /// <param name="val">Output value</param>
        /// <returns>If the string was successfully parsed into the specified type</returns>
        public static bool TryParse<T>(string valStr, out T val)
        {
            val = default;
            if (TryParse(typeof(T), valStr, out object objVal))
            {
                val = (T)objVal;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parse a string into an argument
        /// </summary>
        /// <param name="type">Specific type to parse</typeparam>
        /// <param name="valStr">String to parse</param>
        /// <param name="val">Output value</param>
        /// <returns>If the string was successfully parsed into the specified type</returns>
        public static bool TryParse(Type type, string valStr, out object val)
        {
            try
            {
                val = GetArgType(type).Parser(valStr);
                return true;
            }
            catch
            {
                val = default;
                return false;
            }
        }

        /// <summary>
        /// Get instance of ArgType corresponding to the type
        /// </summary>
        /// <param name="argType">Type</param>
        /// <returns>Corresponding instance, null if does not exist</returns>
        public static ArgType GetArgType(Type argType)
        {
            return argTypes
                .Where(a => a.Type == argType)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get instance of ArgType corresponding to the string name
        /// </summary>
        /// <param name="argTypeStr">String name</param>
        /// <returns>Corresponding instance, null if does not exist</returns>
        public static ArgType GetArgType(string argTypeStr)
        {
            argTypeStr = argTypeStr.ToLower();
            return argTypes
                .Where(a => a.AltNames.Contains(argTypeStr))
                .FirstOrDefault();
        }

        /// <summary>
        /// Type of the argument
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Parser function to deserialize from a string
        /// </summary>
        public Func<string, object> Parser { get; private set; }

        /// <summary>
        /// Identifying names
        /// </summary>
        public HashSet<string> AltNames { get; private set; }

        /// <summary>
        /// Create an ArgType
        /// </summary>
        /// <param name="type">Type of argument</param>
        /// <param name="parser">Parser function</param>
        /// <param name="altNames">Identifying names</param>
        private ArgType(Type type, Func<string, object> parser, params string[] altNames)
        {
            Type = type;
            Parser = parser;

            AltNames = new HashSet<string>();
            foreach (string altName in altNames)
            {
                AltNames.Add(altName.ToLower());
            }
            AltNames.Add(type.Name.ToLower());
        }
    }
}
