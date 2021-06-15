using ATC.Framework.Debugging;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using System;
using System.Collections.Generic;

namespace ATC.Framework.UserInterface
{
    public interface IUserInterfaceManager : ISystemComponent, IFeedbackRenderer
    {
        bool IsRegistered { get; }
        bool IsOnline { get; }

        bool Register(string sgdPath);
        bool Register(ISmartObject sgdPanel);

        // component methods
        T CreateComponent<T>() where T : UserInterfaceComponent, new();
        T CreateComponent<T>(uint joinOffset) where T : UserInterfaceComponent, new();
        void AddComponent(UserInterfaceComponent component);
        T GetComponent<T>() where T : IUserInterfaceComponent;

        // digital join methods
        void SetJoin(DigitalJoin join, bool value, uint offset);
        bool GetJoin(DigitalJoin join, uint offset);
        void PulseJoin(DigitalJoin join, uint offset);

        // analog join methods
        void SetJoin(AnalogJoin join, ushort value, uint offset);
        void SetJoin(AnalogJoin join, int value, uint offset);
        ushort GetJoin(AnalogJoin join, uint offset);

        // serial join methods
        void SetJoin(SerialJoin join, string value, uint offset);
        string GetJoin(SerialJoin join, uint offset);

        // smart object methods
        bool SetItemSelected(uint smartObjectId, int index, bool value);
        bool SetItemEnabled(uint smartObjectId, int index, bool value);
        bool SetItemVisible(uint smartObjectId, int index, bool value);
        bool SetItemCount(uint smartObjectId, int numberOfItems);
        bool SetItemText(uint smartObjectId, int index, string value);
        bool SetItemValue(uint smartObjectId, string name, bool value);
        bool SetItemValue(uint smartObjectId, string name, ushort value);
        bool SetItemValue(uint smartObjectId, int index, ushort value);
        bool SetItemValue(uint smartObjectId, string name, int value);
        bool SetItemValue(uint smartObjectId, int index, int value);
        bool SetItemValue(uint smartObjectId, string name, string value);
    }

    public class UserInterfaceManager<TPanel> : SystemComponent, IUserInterfaceManager
        where TPanel : BasicTriListWithSmartObject
    {
        #region Fields

        private readonly TPanel _panel;
        private readonly List<UserInterfaceComponent> components = new List<UserInterfaceComponent>();

        #endregion

        #region Constants

        private const uint ReservedJoinStart = 16000;

        #endregion

        #region Properties

        /// <summary>
        /// Panel is successfully registered with control system.
        /// </summary>
        public bool IsRegistered
        {
            get { return Panel != null && Panel.Registered; }
        }

        /// <summary>
        /// Returns true if panel is online.
        /// </summary>
        public bool IsOnline
        {
            get { return Panel != null && Panel.IsOnline; }
        }

        public TPanel Panel
        {
            get { return _panel; }
        }

        #endregion

        #region Constructor

        public UserInterfaceManager(TPanel panel)
        {
            _panel = panel;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Attempt to register the panel to the control system.
        /// </summary>
        /// <param name="sgdPath">The path to the SmartGraphics definitions file to use</param>
        /// <returns>True on succesful registration.</returns>
        public virtual bool Register(string sgdPath)
        {
            return Register(sgdPath, null);
        }

        public virtual bool Register(ISmartObject otherPanel)
        {
            return Register(null, otherPanel);
        }

        private bool Register(string sgdPath, ISmartObject otherPanel)
        {
            // register panel
            var response = Panel.Register();
            if (response != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                TraceError("Register() panel failed to register: " + response);
                Panel.Dispose();
                return false;
            }

            // load SmartGraphics objects
            if (!string.IsNullOrEmpty(sgdPath))
            {
                if (File.Exists(sgdPath))
                {
                    Panel.LoadSmartObjects(sgdPath);
                    Trace(string.Format("Register() loaded {0} SmartObjects from file.", Panel.SmartObjects.Count));
                }
                else
                    TraceError(string.Format("Register() SGD file: {0} does not exist.", sgdPath));
            }
            else if (otherPanel != null)
            {
                Panel.LoadSmartObjects(otherPanel);
                Trace(string.Format("Register() loaded {0} SmartObjects from specified panel.", Panel.SmartObjects.Count));
            }
            else
            {
                TraceWarning("Register() not attempting to load any SmartObjects.");
            }

            // add event handlers for smart objects
            Panel.OnlineStatusChange += new OnlineStatusChangeEventHandler(OnlineEventHandler);
            Panel.SigChange += new SigEventHandler(SigChangeHandler);
            foreach (KeyValuePair<uint, SmartObject> pair in Panel.SmartObjects)
                pair.Value.SigChange += new SmartObjectSigChangeEventHandler(SmartObjectHandler);

            // initialize each component
            foreach (var c in components)
                c.InvokeInitialize();

            return true;
        }

        /// <summary>
        /// Create a component and add it to by managed by this object.
        /// </summary>
        /// <typeparam name="T">The type of component to create.</typeparam>
        /// <returns></returns>
        public T CreateComponent<T>()
            where T : UserInterfaceComponent, new()
        {
            return CreateComponent<T>(0);
        }

        /// <summary>
        /// Create a component and add it to by managed by this object.
        /// </summary>
        /// <typeparam name="T">The type of component to create.</typeparam>
        /// <param name="joinOffset">The join offset to apply to the component.</param>
        /// <returns></returns>
        public T CreateComponent<T>(uint joinOffset)
            where T : UserInterfaceComponent, new()
        {
            string typeName = typeof(T).Name;

            T component = new T()
            {
                ComponentName = string.Format("{0}-{1}", ComponentName, typeName),
                TraceEnabled = TraceEnabled,
                JoinOffset = joinOffset,
                Manager = this,
            };

            components.Add(component);

            return component;
        }

        /// <summary>
        /// Add a component to be managed by this object.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public void AddComponent(UserInterfaceComponent component)
        {
            try
            {
                if (components != null)
                {
                    if (components.Contains(component))
                    {
                        TraceWarning("AddComponent() components list already contains specified component. No action taken.");
                        return;
                    }

                    components.Add(component); // add component to list of managed components
                    component.Manager = this; // set the manager for the component to be this object
                    Trace(string.Format("AddComponent() added {0} to list. Count is now: {1}", component.ComponentName, components.Count));
                }
                else
                {
                    TraceError("AddComponent() components list is null. Cannot add component.");
                }
            }
            catch (Exception ex)
            {
                TraceException("AddComponent() exception caught.", ex);
            }
        }

        /// <summary>
        /// Return the first user interface component that matches the specified type.
        /// </summary>
        /// <typeparam name="T">The type to match.</typeparam>
        /// <returns>A reference to the component if found, null if not found.</returns>
        public new T GetComponent<T>()
            where T : IUserInterfaceComponent
        {
            IUserInterfaceComponent component = components.Find(c => c.GetType() == typeof(T));

            // print warning if component couldn't be found
            if (component == null)
                TraceWarning(string.Format("GetComponent() component of type: {0} was not found.", typeof(T).Name));

            return (T)component;
        }

        /// <summary>
        /// Render panel feedback in all components
        /// </summary>
        public virtual void Render()
        {
            try
            {
                // render list of components (if any defined)
                Trace(string.Format("Render() rendering {0} components.", components.Count), TraceLevel.Extended);
                foreach (var c in components)
                    c.Render();
            }
            catch (Exception ex)
            {
                TraceException("Render() exception caught.", ex);
            }
        }

        /// <summary>
        /// Reset all component variables to default values.
        /// </summary>
        public virtual void Reset()
        {
            try
            {
                Trace(string.Format("Reset() resetting {0} components.", components.Count), TraceLevel.Extended);
                foreach (var c in components)
                    c.Reset();
            }
            catch (Exception ex)
            {
                TraceException("Render() exception caught.", ex);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                // remove event handlers
                Panel.OnlineStatusChange -= OnlineEventHandler;
                Panel.SigChange -= SigChangeHandler;
                foreach (KeyValuePair<uint, SmartObject> pair in Panel.SmartObjects)
                    pair.Value.SigChange -= SmartObjectHandler;

                var response = Panel.UnRegister();
                if (response == eDeviceRegistrationUnRegistrationResponse.Success)
                    Trace("Dispose() succesfully unregistered panel.");
                else
                    TraceError("Dispose() error occurred while trying to unregister: " + response);

                Panel.Dispose();
            }
        }

        #region Panel event handlers

        protected virtual void OnlineEventHandler(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            try
            {
                // check if panel has been unregistered
                if (Disposed && !args.DeviceOnLine)
                {
                    Trace("OnlineEventHandler() panel has been unregistered.");
                    return;
                }

                Trace("OnlineEventHandler() called. Online: " + args.DeviceOnLine);

                // validate components list
                if (components == null || components.Count == 0)
                {
                    TraceError("OnlineEventHandler() components list is null or empty.");
                    return;
                }

                // iterate through all components
                foreach (UserInterfaceComponent c in components)
                    c.OnlineEventHandler(args);

                // perform full update
                if (args.DeviceOnLine)
                    Render();
            }
            catch (Exception ex)
            {
                TraceException("OnlineEventHandler() exception caught.", ex);
            }
        }

        protected virtual void SigChangeHandler(BasicTriList device, SigEventArgs args)
        {
            try
            {
                Trace(string.Format("SigChangeHandler() handling signal type: {0}, number: {1}", args.Sig.Type, args.Sig.Number), TraceLevel.Extended);

                // do nothing if its a reserved join
                if (args.Sig.Number > ReservedJoinStart)
                    return;

                bool eventHandled = false;

                // iterate through all components
                foreach (UserInterfaceComponent c in components)
                {
                    // attempt to handle event with each component
                    if (c.SigEventHandler(args) == true)
                    {
                        Trace(string.Format("SigChangeHandler() signal type: {0}, number: {1}, handled by: {2}", args.Sig.Type, args.Sig.Number, c.ComponentName), TraceLevel.Extended);
                        eventHandled = true;
                    }
                }

                // exit method if event has been handled
                if (eventHandled)
                    return;

                // output warning if signal hasn't been handled by any of the components
                string message;
                switch (args.Sig.Type)
                {
                    case eSigType.Bool:
                        message = string.Format("SigChangeHandler() unhandled digital signal. Number: {0}, Value: {1}", args.Sig.Number, args.Sig.BoolValue);
                        break;
                    case eSigType.UShort:
                        message = string.Format("SigChangeHandler() unhandled analog signal. Number: {0}, Value: {1}", args.Sig.Number, args.Sig.UShortValue);
                        break;
                    case eSigType.String:
                        message = string.Format("SigChangeHandler() unhandled serial signal. Number: {0}, Value: {1}", args.Sig.Number, args.Sig.StringValue);
                        break;
                    default:
                        message = "SigChangeHandler() unhandled signal type: " + args.Sig.Type;
                        break;
                }
                TraceWarning(message);
            }
            catch (Exception ex)
            {
                TraceException("SigChangeHandler() exception caught.", ex);
            }
        }

        protected virtual void SmartObjectHandler(GenericBase device, SmartObjectEventArgs args)
        {
            try
            {
                Trace(string.Format("SmartObjectHandler() handling ID: {0}, event type: {1}, number: {2}, name: \"{3}\"", args.SmartObjectArgs.ID, args.Event, args.Sig.Number, args.Sig.Name), TraceLevel.Extended);

                bool eventHandled = false;

                // iterate through all components
                foreach (UserInterfaceComponent c in components)
                {
                    // if component handles smartobject event then return
                    if (c.SmartObjectEventHandler(args) == true)
                    {
                        Trace(string.Format("SmartObjectHandler() object ID: {0}, handled by: {1}", args.SmartObjectArgs.ID, c.ComponentName), TraceLevel.Extended);
                        eventHandled = true;
                    }
                }

                // exit method if event has been handled
                if (eventHandled) return;

                // output warning if signal hasn't been handled by any of the components            
                string value;
                switch (args.Event)
                {
                    case eSigEvent.BoolChange: value = args.Sig.BoolValue.ToString(); break;
                    case eSigEvent.UShortChange: value = args.Sig.UShortValue.ToString(); break;
                    case eSigEvent.StringChange: value = args.Sig.StringValue; break;
                    default:
                        TraceWarning("SmartObjectHandler() unhandled event type: " + args.Event);
                        return;
                }
                TraceWarning(string.Format("SmartObjectHandler() unhandled SmartObject event. ID: {0}, event type: {1}, number: {2}, name: \"{3}\", value: {4}", args.SmartObjectArgs.ID, args.Event, args.Sig.Number, args.Sig.Name, value));
            }
            catch (Exception ex)
            {
                TraceException("SmartObjectHandler() exception caught.", ex);
            }
        }

        #endregion

        #region Panel join methods

        /// <summary>
        /// Sets the specified digital join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set.</param>
        /// <param name="value">The value to set</param>
        /// <param name="offset">The join offset to add.</param>
        public void SetJoin(DigitalJoin join, bool value, uint offset)
        {
            var offsetJoin = new DigitalJoin(join.Number + offset);
            TraceInfo(string.Format("SetJoin() setting join: {0} to value: {1}", offsetJoin, value), TraceLevel.Extended);
            DigitalJoin.SetJoin(Panel, offsetJoin, value);
        }

        /// <summary>
        /// Gets the value of the specified digital join from the panel.
        /// </summary>
        /// <param name="join">Join number to get</param>
        /// <param name="component">The component to apply the join offset from.</param>
        /// <param name="offset">The join offset to add.</param>
        /// <returns>The value of the join set on the panel</returns>
        public bool GetJoin(DigitalJoin join, uint offset)
        {
            var offsetJoin = new DigitalJoin(join.Number + offset);
            return DigitalJoin.GetJoin(Panel, offsetJoin);
        }

        /// <summary>
        /// Pulses (on then off) the specified digital join.
        /// </summary>
        /// <param name="join">The digital join to pulse.</param>
        /// <param name="offset">The join offset to add.</param>
        public void PulseJoin(DigitalJoin join, uint offset)
        {
            var offsetJoin = new DigitalJoin(join.Number + offset);
            DigitalJoin.PulseJoin(Panel, offsetJoin);
        }

        /// <summary>
        /// Sets the specified analog join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        /// <param name="offset">The join offset to add.</param>
        public void SetJoin(AnalogJoin join, ushort value, uint offset)
        {
            var offsetJoin = new AnalogJoin(join.Number + offset);
            TraceInfo(string.Format("SetJoin() setting join: {0} to value: {1}", offsetJoin, value), TraceLevel.Extended);
            AnalogJoin.SetJoin(Panel, offsetJoin, value);
        }

        /// <summary>
        /// Sets the specified analog join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        /// <param name="offset">The join offset to add.</param>
        public void SetJoin(AnalogJoin join, int value, uint offset)
        {
            var offsetJoin = new AnalogJoin(join.Number + offset);
            TraceInfo(string.Format("SetJoin() setting join: {0} to value: {1}", offsetJoin, value), TraceLevel.Extended);
            AnalogJoin.SetJoin(Panel, offsetJoin, (ushort)value);
        }

        /// <summary>
        /// Gets the value of the specified analog join (adding any join offset) from the panel.
        /// </summary>
        /// <param name="join">Join number to get (do not include offset)</param>
        /// <param name="offset">The join offset to add.</param>
        /// <returns>The value of the join set on the panel</returns>
        public ushort GetJoin(AnalogJoin join, uint offset)
        {
            var offsetJoin = new AnalogJoin(join.Number + offset);
            return AnalogJoin.GetJoin(Panel, offsetJoin);
        }

        /// <summary>
        /// Sets the specified serial join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        /// <param name="offset">The join offset to add.</param>
        public void SetJoin(SerialJoin join, string value, uint offset)
        {
            var offsetJoin = new SerialJoin(join.Number + offset);
            TraceInfo(string.Format("SetJoin() setting join: {0} to value: {1}", offsetJoin, value), TraceLevel.Extended);
            SerialJoin.SetJoin(Panel, offsetJoin, value);
        }

        /// <summary>
        /// Gets the value of the specified serial join (adding any join offset) from the panel.
        /// </summary>
        /// <param name="join">Join number to get (do not include offset)</param>
        /// <param name="offset">The join offset to add.</param>
        /// <returns>The value of the join set on the panel</returns>
        public string GetJoin(SerialJoin join, uint offset)
        {
            var offsetJoin = new SerialJoin(join.Number + offset);
            return SerialJoin.GetJoin(Panel, offsetJoin);
        }

        #endregion

        #region SmartObject methods

        /// <summary>
        /// Sets the selected feedback of the specified smart object item to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemSelected(uint smartObjectId, int index, bool value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetSelected(so, index, value);
            }
            else
            {
                TraceError("SetSmartObjectItemSelected() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the enabled status of the specified smart object item to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemEnabled(uint smartObjectId, int index, bool value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetEnabled(so, index, value);
            }
            else
            {
                TraceError("SetItemEnabled() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the visible status of the specified smart object item to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemVisible(uint smartObjectId, int index, bool value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetVisible(so, index, value);
            }
            else
            {
                TraceError("SetItemVisible() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Set the number of items for a specified SmartObject.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="numberOfItems">The number of items to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemCount(uint smartObjectId, int numberOfItems)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetNumberOfItems(so, numberOfItems);
            }
            else
            {
                TraceError("SetItemVisible() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the text feedback of the specified smart object item to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The string value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemText(uint smartObjectId, int index, string value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetText(so, index, value);
            }
            else
            {
                TraceError("SetItemText() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the digital input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemValue(uint smartObjectId, string name, bool value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetValue(so, name, value);
            }
            else
            {
                TraceError("SetItemValue() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemValue(uint smartObjectId, string name, ushort value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetValue(so, name, value);
            }
            else
            {
                TraceError("SetItemValue() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemValue(uint smartObjectId, int index, ushort value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                string name = "an_fb" + index;
                return SmartObjectMethods.SetValue(so, name, value);
            }
            else
            {
                TraceError("SetItemValue() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemValue(uint smartObjectId, string name, int value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetValue(so, name, value);
            }
            else
            {
                TraceError("SetItemValue() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemValue(uint smartObjectId, int index, int value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                string name = "an_fb" + index;
                return SmartObjectMethods.SetValue(so, name, value);
            }
            else
            {
                TraceError("SetItemValue() invalid SmartObject ID.");
                return false;
            }
        }

        /// <summary>
        /// Sets the serial input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The serial value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        public bool SetItemValue(uint smartObjectId, string name, string value)
        {
            if (Panel.SmartObjects.Contains(smartObjectId))
            {
                SmartObject so = Panel.SmartObjects[smartObjectId];
                return SmartObjectMethods.SetValue(so, name, value);
            }
            else
            {
                TraceError("SetItemValue() invalid SmartObject ID.");
                return false;
            }
        }

        #endregion
    }
}
