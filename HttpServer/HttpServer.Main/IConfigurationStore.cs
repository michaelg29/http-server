using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer.Main
{
    /// <summary>
    /// Interface for configuration variables
    /// </summary>
    public interface IConfigurationStore
    {
        /// <summary>
        /// Register a configuration variable
        /// </summary>
        /// <param name="name">Name of variable</param>
        /// <param name="value">Value of variable</param>
        void RegisterVariable<T>(string name, T value);

        /// <summary>
        /// Retrieve a configuration variable
        /// </summary>
        /// <typeparam name="T">Type to retrieve</typeparam>
        /// <param name="name">Name of variable</param>
        /// <param name="variable">Output variable</param>
        /// <returns>If the variable was found</returns>
        bool TryGetVariable<T>(string name, out T variable);

        /// <summary>
        /// Retrieve a configuration variable
        /// </summary>
        /// <typeparam name="T">Type to retrieve</typeparam>
        /// <param name="name">Name of variable</param>
        /// <param name="defaultVal">Default value if not found</param>
        /// <returns>The found variable or default value</returns>
        T GetVariable<T>(string name, T defaultVal = default);
    }
}
