using ATC.Framework.Debugging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ATC.Framework
{
    public interface ISystemComponent
    {
        string ComponentName { get; set; }
        bool TraceEnabled { get; set; }
        TraceLevel TraceLevel { get; set; }
    }

    public abstract class SystemComponent : ISystemComponent, IDisposable
    {
        #region Fields

        private readonly Tracer tracer = new Tracer();
        private readonly static Dictionary<string, ISystemComponent> components = new Dictionary<string, ISystemComponent>();

        #endregion

        #region Properties
        [JsonIgnore]
        public virtual bool TraceEnabled
        {
            get { return tracer.Enabled; }
            set { tracer.Enabled = value; }
        }

        [JsonIgnore]
        public virtual string ComponentName
        {
            get => tracer.Name;
            set
            {
                RemoveComponent(tracer.Name); // remove old reference to component

                // update references
                tracer.Name = value;
                AddComponent(this);
            }
        }

        [JsonIgnore]
        public virtual TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Set to true once the Dispose method has been called.
        /// </summary>
        [JsonIgnore]
        public bool Disposed { get; private set; }

        #endregion

        #region Constructor

        public SystemComponent()
        {
            string name = GetType().Name; // default name is just class name

            int count = 0;
            while (components.ContainsKey(name)) // generate unique name            
                name = string.Format("{0}-{1}", GetType().Name, count++);

            ComponentName = name;
        }

        #endregion

        #region Object cleanup

        /// <summary>
        /// Free up any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
                RemoveComponent(ComponentName);

            Disposed = true;
        }

        ~SystemComponent() => Dispose(false);

        #endregion

        #region Trace methods

        protected void Trace(string message)
        {
            tracer.Trace(message);
        }

        /// <summary>
        /// Outputs the specified message to the console (if specified level is greater than or equal to component TraceLevel)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        protected void Trace(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.Trace(message);
        }

        /// <summary>
        /// Outputs a special line to the console.
        /// </summary>
        /// <param name="lineType">The special type of line to output.</param>
        protected void TraceLine(LineType lineType)
        {
            tracer.TraceLine(lineType);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceInfo(string message)
        {
            tracer.TraceInfo(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The level at which to log.</param>
        protected void TraceInfo(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.TraceInfo(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceWarning(string message)
        {
            tracer.TraceWarning(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceWarning(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.TraceWarning(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceError(string message)
        {
            tracer.TraceError(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        protected void TraceError(string message, TraceLevel level)
        {
            if (TraceLevel >= level)
                tracer.TraceError(message);
        }

        /// <summary>
        /// Output the specified message to console and also write to error log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">The exception to log.</param>
        protected void TraceException(string message, Exception ex)
        {
            tracer.TraceException(message, ex);
        }

        protected void TraceException(Exception ex, string methodName, string message = null)
        {
            tracer.TraceException(ex, methodName, message);
        }

        #endregion

        #region Class static methods

        /// <summary>
        /// Add a component to the local cache.
        /// </summary>
        /// <param name="component">The component to add.</param>
        internal static void AddComponent(ISystemComponent component)
        {
            components[component.ComponentName] = component;
#warning Does not check for duplicates
            //components.Add(component.ComponentName, component);
            Tracer.PrintLine($"Added {component.ComponentName} to system component collection. Total count is: {components.Count}");
        }

        /// <summary>
        /// Get a reference to a component by its name.
        /// </summary>
        /// <param name="name">The name of the component</param>
        /// <returns>A reference to the component</returns>
        public static ISystemComponent GetComponent(string name)
        {
            return components[name];
        }

        /// <summary>
        /// Get a reference to a component by its type. This will only return the first component found of this type.
        /// </summary>
        /// <typeparam name="T">The type of system component</typeparam>
        /// <returns>A reference to the component (or null if not found)</returns>
        public static T GetComponent<T>() where T : ISystemComponent
        {
            ISystemComponent component = components.Values.FirstOrDefault(c => c.GetType() == typeof(T));

            // print warning if component couldn't be found
            if (component == null)
                Tracer.PrintLine(string.Format("GetComponent() couldn't find any component of type: {0}", typeof(T).Name));

            return (T)component;
        }

        /// <summary>
        /// Remove a component from the local cache.
        /// </summary>
        /// <param name="name">The name of the component to remove.</param>
        private static void RemoveComponent(string name)
        {
            if (name == null)
                return;

            bool result = components.Remove(name);
            string message = result ?
                $"Removed {name} from system component collection. Total count is: {components.Count}" :
                $"Couln't find {name} in component collection - cannot remove.";

            Tracer.PrintLine(message);
        }

        #endregion
    }
}
