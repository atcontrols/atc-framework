using ATC.Framework.Debugging;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using System;
using System.Collections.Generic;

namespace ATC.Framework.UserInterface
{
    public interface IUserInterfaceComponent : IFeedbackRenderer
    {
        IUserInterfaceManager Manager { get; }
        uint JoinOffset { get; set; }
    }

    public abstract class UserInterfaceComponent : SystemComponent, IUserInterfaceComponent
    {
        #region Fields

        private IUserInterfaceManager _manager;
        private uint _joinOffset;

        private readonly List<DigitalJoin> digitals = new List<DigitalJoin>();
        private readonly List<AnalogJoin> analogs = new List<AnalogJoin>();
        private readonly List<SerialJoin> serials = new List<SerialJoin>();
        private readonly List<uint> smartObjectIds = new List<uint>();

        private CTimer holdTimer;

        #endregion

        #region Properties

        /// <summary>
        /// The UserInterfaceManager that manages this component.
        /// </summary>
        public IUserInterfaceManager Manager
        {
            get
            {
                if (_manager == null)
                    throw new NullReferenceException("The user interface manager has not been set. This property should be accessed after the Initialize method is called.");

                return _manager;
            }
            internal set
            {
                _manager = value;
            }
        }

        /// <summary>
        /// The amount to offset all specified joins by.
        /// </summary>
        public uint JoinOffset
        {
            get { return _joinOffset; }
            set
            {
                foreach (var join in digitals)
                    join.Offset = value;

                foreach (var join in analogs)
                    join.Offset = value;

                foreach (var join in serials)
                    join.Offset = value;

                _joinOffset = value;
            }
        }

        /// <summary>
        /// True if the button held timer is currently active.
        /// </summary>
        private bool IsHoldTimerActive
        {
            get { return holdTimer != null; }
        }

        #endregion

        #region Join listener methods

        /// <summary>
        /// Adds the specified digital join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        protected void AddJoinListener(DigitalJoin join)
        {
            AddJoinListener(join, 0, 0, null);
        }

        /// <summary>
        /// Adds the specified digital join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        /// <param name="handler">Handler method to process join.</param>
        protected void AddJoinListener(DigitalJoin join, DigitalJoinHandler handler)
        {
            AddJoinListener(join, 0, 0, handler);
        }

        /// <summary>
        /// Adds the specified digital join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        /// <param name="holdTime">How long (in milliseconds) should the button be held for.</param>
        /// <param name="repeatTime">How often (in milliseconds) should the event repeat while the button is held.</param>
        protected void AddJoinListener(DigitalJoin join, long holdTime)
        {
            AddJoinListener(join, holdTime, 0, null);
        }

        /// <summary>
        /// Adds the specified digital join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        /// <param name="holdTime">How long (in milliseconds) should the button be held for.</param>
        /// <param name="repeatTime">How often (in milliseconds) should the event repeat while the button is held.</param>
        protected void AddJoinListener(DigitalJoin join, long holdTime, long repeatTime)
        {
            AddJoinListener(join, holdTime, repeatTime, null);
        }

        /// <summary>
        /// Adds the specified digital join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        /// <param name="holdTime">How long (in milliseconds) should the button be held for.</param>
        /// <param name="repeatTime">How often (in milliseconds) should the event repeat while the button is held.</param>
        /// <param name="handler">Handler method to process join.</param>
        protected void AddJoinListener(DigitalJoin join, long holdTime, long repeatTime, DigitalJoinHandler handler)
        {
            if (join == null)
                throw new ArgumentNullException("join");

            // set join internal properties
            join.Offset = JoinOffset;
            join.Handler = handler;

            // set hold parameters
            if (holdTime > 0)
            {
                join.HoldParams = new DigitalJoinHoldParameters()
                {
                    HoldTime = holdTime,
                    RepeatTime = repeatTime,
                };
            }

            if (digitals.Exists(j => j.Number == join.Number && j.Offset == JoinOffset))
                TraceError(string.Format("AddJoinListener() join number: {0} already exists in digital joins list.", join.Number));
            else
            {
                Trace("AddJoinListener() Adding join: " + join);
                digitals.Add(join);
            }
        }

        /// <summary>
        /// Adds the specified analog join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        protected void AddJoinListener(AnalogJoin join)
        {
            AddJoinListener(join, null);
        }

        /// <summary>
        /// Adds the specified analog join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        /// <param name="handler">Handler method to process join.</param>
        protected void AddJoinListener(AnalogJoin join, AnalogJoinHander handler)
        {
            // set join internal properties
            join.Offset = JoinOffset;
            join.Handler = handler;

            if (analogs.Exists(j => j.Number == join.Number))
                TraceError(string.Format("AddJoinListener() join number: {0} already exists in analog joins list.", join.Number));
            else
            {
                Trace("AddJoinListener() Adding analog join: " + join.Number);
                analogs.Add(join);
            }
        }

        /// <summary>
        /// Adds the specified serial join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        protected void AddJoinListener(SerialJoin join)
        {
            AddJoinListener(join, null);
        }

        /// <summary>
        /// Adds the specified serial join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="join">The join to add.</param>
        /// <param name="handler">Handler method to process join.</param>
        protected void AddJoinListener(SerialJoin join, SerialJoinHandler handler)
        {
            // set join internal properties
            join.Offset = JoinOffset;
            join.Handler = handler;

            if (serials.Exists(x => x.Number == join.Number))
                TraceError(string.Format("AddJoinListener() join number: {0} already exists in serial joins list.", join.Number));
            else
            {
                Trace("AddJoinListener() Adding serial join: " + join.Number);
                serials.Add(join);
            }
        }

        /// <summary>
        /// Adds the specfied join to a list of joins that this component should listen for.
        /// </summary>
        /// <param name="joinNumber">The raw join number</param>
        /// <param name="joinType">The type of join to add (digital, analog, serial)</param>
        protected void AddJoinListener(uint joinNumber, JoinType joinType)
        {
            switch (joinType)
            {
                case JoinType.Digital: AddJoinListener(new DigitalJoin(joinNumber)); break;
                case JoinType.Analog: AddJoinListener(new AnalogJoin(joinNumber)); break;
                case JoinType.Serial: AddJoinListener(new SerialJoin(joinNumber)); break;
            }
        }

        /// <summary>
        /// Add a collection of joins to be listened for (using the default handler)
        /// </summary>
        /// <param name="joins">A enumerable collection of join numbers.</param>
        /// <param name="joinType">The type of join</param>
        protected void AddJoinListener(IEnumerable<uint> joins, JoinType joinType)
        {
            foreach (var join in joins)
            {
                switch (joinType)
                {
                    case JoinType.Digital: AddJoinListener(new DigitalJoin(join)); break;
                    case JoinType.Analog: AddJoinListener(new AnalogJoin(join)); break;
                    case JoinType.Serial: AddJoinListener(new SerialJoin(join)); break;
                }
            }
        }

        /// <summary>
        /// Adds the specified SmartObject ID to a list of IDs that the compononent should listen for.
        /// </summary>
        /// <param name="id"></param>
        protected void AddSmartObjectListener(uint id)
        {
            if (smartObjectIds.Contains(id))
            {
                TraceError("AddSmartObjectId() SmartObject list already contains ID: " + id);
            }
            else
            {
                Trace("AddJoin() Adding SmartObject with ID: " + id);
                smartObjectIds.Add(id);
            }
        }

        #endregion

        #region Manager invoked methods

        /// <summary>
        /// Invoke the Initialize method on the component.
        /// </summary>
        internal void InvokeInitialize()
        {
            Initialize();
        }

        internal void OnlineEventHandler(OnlineOfflineEventArgs args)
        {
            OnlineStateHandler(args.DeviceOnLine);
        }

        internal bool SigEventHandler(SigEventArgs args)
        {
            // calculate base join number
            uint baseJoinNumber = args.Sig.Number - JoinOffset; // base join number (join offset subtracted)

            // check that we're handling positive integer joins only
            if ((int)args.Sig.Number - JoinOffset < 0)
                return false; // negative join number detected (probably due to join offset)

            Trace(string.Format("SigEventHandler() validating signal type: {0}, number: {1}, join offset: {2}, base join number: {3}, ", args.Sig.Type, args.Sig.Number, JoinOffset, baseJoinNumber), TraceLevel.Extended);

            switch (args.Sig.Type)
            {
                case eSigType.Bool: // digital join

                    // check that the join number is handled by this component
                    DigitalJoin digitalJoin = digitals.Find(j => j.Number == baseJoinNumber && j.Offset == JoinOffset);
                    if (digitalJoin == null)
                    {
                        Trace(string.Format("SigEventHandler() digital join: {0} not handled by this component.", baseJoinNumber), TraceLevel.Extended);
                        return false;
                    }

                    // set the join's value
                    digitalJoin.Value = args.Sig.BoolValue ? DigitalJoinState.Pressed : DigitalJoinState.Released;

                    // dispatch to handler method
                    if (digitalJoin.Handler != null)
                        digitalJoin.Handler(digitalJoin); // custom handler
                    else
                        DigitalJoinHandler(digitalJoin); // default handler

                    // manage button held logic (if required)
                    if (digitalJoin.Value == DigitalJoinState.Pressed && digitalJoin.HoldParams != null)
                        CreateHoldTimer(digitalJoin);
                    else if (digitalJoin.Value == DigitalJoinState.Released && IsHoldTimerActive)
                        DestroyHoldTimer();

                    return true;

                case eSigType.UShort: // analog join

                    // check that the join number is handled by this component
                    AnalogJoin analogJoin = analogs.Find(j => j.Number == baseJoinNumber && j.Offset == JoinOffset);
                    if (analogJoin == null)
                    {
                        Trace(string.Format("SigEventHandler() analog join: {0} not handled by this component.", baseJoinNumber), TraceLevel.Extended);
                        return false;
                    }

                    // set the join's value
                    analogJoin.Value = args.Sig.UShortValue;

                    // dispatch to handler method
                    if (analogJoin.Handler != null)
                        analogJoin.Handler(analogJoin); // custom handler
                    else
                        AnalogJoinHandler(analogJoin); // default handler

                    return true;

                case eSigType.String: // serial join

                    // check that the join number is handled by this component
                    SerialJoin serialJoin = serials.Find(j => j.Number == baseJoinNumber && j.Offset == JoinOffset);
                    if (serialJoin == null)
                    {
                        Trace(string.Format("SigEventHandler() serial join: {0} not handled by this component.", baseJoinNumber), TraceLevel.Extended);
                        return false;
                    }

                    // set the join's value
                    serialJoin.Value = args.Sig.StringValue;

                    // dispatch to handler method
                    if (serialJoin.Handler != null)
                        serialJoin.Handler(serialJoin); // custom handler
                    else
                        SerialJoinHandler(serialJoin); // default handler

                    return true;

                default:
                    TraceWarning("SigEventHandler() unhandled signal type: " + args.Sig.Type);
                    return false;
            }
        }

        internal bool SmartObjectEventHandler(SmartObjectEventArgs args)
        {
            Trace(string.Format("SmartObjectEventHandler() validating event. ID: {0}, event type: {1}, number: {2}, name: \"{3}\"", args.SmartObjectArgs.ID, args.Event, args.Sig.Number, args.Sig.Name), TraceLevel.Extended);

            if (smartObjectIds.Contains(args.SmartObjectArgs.ID))
            {
                switch (args.Event)
                {
                    case eSigEvent.BoolChange:

                        // ignore "Is Moving" event
                        if (args.Sig.Name == "Is Moving")
                            return true;

                        DigitalJoinState state = args.Sig.BoolValue ? DigitalJoinState.Pressed : DigitalJoinState.Released;
                        uint number = SmartObjectMethods.GetButtonIndex(args.Sig.Name) > 0 ? (uint)SmartObjectMethods.GetButtonIndex(args.Sig.Name) : args.Sig.Number;
                        SmartObjectBoolHandler(args.SmartObjectArgs.ID, number, args.Sig.Name, state);
                        // todo: add button held handling
                        break;

                    case eSigEvent.UShortChange:
                        SmartObjectUShortHandler(args.SmartObjectArgs.ID, args.Sig.Number, args.Sig.Name, args.Sig.UShortValue);
                        break;

                    case eSigEvent.StringChange:
                        SmartObjectStringHandler(args.SmartObjectArgs.ID, args.Sig.Number, args.Sig.Name, args.Sig.StringValue);
                        break;

                    default:
                        TraceWarning("SmartObjectEventHandler() unhandled event type: " + args.Event);
                        break;
                }

                return true;
            }
            else
            {
                Trace(string.Format("SmartObjectEventHandler() smart object ID: {0} not handled by this component.", args.SmartObjectArgs.ID), TraceLevel.Extended);
                return false;
            }
        }

        #endregion

        #region Virtual methods

        protected virtual void Initialize() { }
        protected virtual void OnlineStateHandler(bool panelOnline) { }
        protected virtual void DigitalJoinHandler(DigitalJoin join) { }
        protected virtual void AnalogJoinHandler(AnalogJoin join) { }
        protected virtual void SerialJoinHandler(SerialJoin join) { }
        protected virtual void SmartObjectBoolHandler(uint smartObjectId, uint number, string name, DigitalJoinState state) { }
        protected virtual void SmartObjectUShortHandler(uint smartObjectId, uint number, string name, ushort value) { }
        protected virtual void SmartObjectStringHandler(uint smartObjectId, uint number, string name, string value) { }
        public virtual void Render() { }
        public virtual void Reset() { }

        #endregion

        #region Join methods

        /// <summary>
        /// Sets the specified digital join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(DigitalJoin join, bool value)
        {
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Sets the specified digital join number (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="joinNumber">The raw join number to use</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(uint joinNumber, bool value)
        {
            var join = new DigitalJoin(joinNumber);
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Gets the value of the specified digital join (adding any join offset) from the panel.
        /// </summary>
        /// <param name="join">Join to get (do not include offset)</param>
        /// <returns>The value of the join set on the panel</returns>
        protected bool SetJoin(DigitalJoin join)
        {
            return Manager.GetJoin(join, JoinOffset);
        }

        /// <summary>
        /// Pulses (on then off) the specified join.
        /// </summary>
        /// <param name="join">The digital join to pulse.</param>
        protected void PulseJoin(DigitalJoin join)
        {
            Manager.PulseJoin(join, JoinOffset);
        }

        /// <summary>
        /// Sets the specified analog join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(AnalogJoin join, ushort value)
        {
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Sets the specified analog join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="joinNumber">The raw join number to use</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(uint joinNumber, ushort value)
        {
            var join = new AnalogJoin(joinNumber);
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Gets the value of the specified analog join (adding any join offset) from the panel.
        /// </summary>
        /// <param name="join">Join number to get (do not include offset)</param>
        /// <returns>The value of the join set on the panel</returns>
        protected ushort GetJoin(AnalogJoin join)
        {
            return Manager.GetJoin(join, JoinOffset);
        }

        /// <summary>
        /// Sets the specified analog join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(AnalogJoin join, int value)
        {
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Sets the specified analog join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="joinNumber">The raw join number to use</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(uint joinNumber, int value)
        {
            var join = new AnalogJoin(joinNumber);
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Sets the specified serial join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(SerialJoin join, string value)
        {
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Sets the specified serial join (adding any join offset) to the specified value.
        /// </summary>
        /// <param name="join">Join number to set (do not include offset)</param>
        /// <param name="value">The value to set</param>
        protected void SetJoin(uint joinNumber, string value)
        {
            var join = new SerialJoin(joinNumber);
            Manager.SetJoin(join, value, JoinOffset);
        }

        /// <summary>
        /// Gets the value of the specified serial join (adding any join offset) from the panel.
        /// </summary>
        /// <param name="join">Join number to get (do not include offset)</param>
        /// <returns>The value of the join set on the panel</returns>
        protected string GetJoin(SerialJoin join)
        {
            return Manager.GetJoin(join, JoinOffset);
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
        protected bool SetItemSelected(uint smartObjectId, int index, bool value)
        {
            return Manager.SetItemSelected(smartObjectId, index, value);
        }

        /// <summary>
        /// Sets the visible status of the specified smart object item to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemEnabled(uint smartObjectId, int index, bool value)
        {
            return Manager.SetItemEnabled(smartObjectId, index, value);
        }

        /// <summary>
        /// Sets the visible status of the specified smart object item to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemVisible(uint smartObjectId, int index, bool value)
        {
            return Manager.SetItemVisible(smartObjectId, index, value);
        }

        /// <summary>
        /// Set the number of items for a specified SmartObject.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="numberOfItems">The number of items to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemCount(uint smartObjectId, int numberOfItems)
        {
            return Manager.SetItemCount(smartObjectId, numberOfItems);
        }

        /// <summary>
        /// Sets the text feedback of the specified smart object item to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The string value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemText(uint smartObjectId, int index, string value)
        {
            return Manager.SetItemText(smartObjectId, index, value);
        }

        /// <summary>
        /// Sets the digital input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemValue(uint smartObjectId, string name, bool value)
        {
            return Manager.SetItemValue(smartObjectId, name, value);
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemValue(uint smartObjectId, string name, ushort value)
        {
            return Manager.SetItemValue(smartObjectId, name, value);
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemValue(uint smartObjectId, int index, ushort value)
        {
            return Manager.SetItemValue(smartObjectId, index, value);
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemValue(uint smartObjectId, string name, int value)
        {
            return Manager.SetItemValue(smartObjectId, name, value);
        }

        /// <summary>
        /// Sets the analog input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="value">The analog value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemValue(uint smartObjectId, int index, int value)
        {
            return Manager.SetItemValue(smartObjectId, index, value);
        }

        /// <summary>
        /// Sets the serial input of the smart object to the value specified.
        /// </summary>
        /// <param name="smartObjectId">The ID of the SmartObject.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="value">The serial value to set.</param>
        /// <returns>True on success / false on fail.</returns>
        protected bool SetItemValue(uint smartObjectId, string name, string value)
        {
            return Manager.SetItemValue(smartObjectId, name, value);
        }

        #endregion

        #region Button hold methods

        private void CreateHoldTimer(DigitalJoin join)
        {
            try
            {
                if (join.HoldParams == null)
                {
                    TraceError("CreateHoldTimer() join hold parameters are null.");
                    return;
                }

                if (IsHoldTimerActive)
                    DestroyHoldTimer();

                if (join.HoldParams.RepeatTime > 0)
                {
                    holdTimer = new CTimer(HoldTimerCallback, join, join.HoldParams.HoldTime, join.HoldParams.RepeatTime);
                    Trace(string.Format("CreateHoldTimer() created timer. Join: {0}, Hold Time: {1}, Repeat Time: {2}", join, join.HoldParams.HoldTime, join.HoldParams.RepeatTime), TraceLevel.Extended);
                }
                else
                {
                    holdTimer = new CTimer(HoldTimerCallback, join, join.HoldParams.HoldTime);
                    Trace(string.Format("CreateHoldTimer() created timer. Join: {0}, Hold Time: {1}", join, join.HoldParams.HoldTime), TraceLevel.Extended);
                }
            }
            catch (Exception ex)
            {
                TraceException("CreateHoldTimer() exception caught.", ex);
            }
        }

        private void DestroyHoldTimer()
        {
            try
            {
                if (IsHoldTimerActive)
                {
                    holdTimer.Stop();
                    holdTimer.Dispose();
                    holdTimer = null;
                    Trace("DestroyHoldTimer() cleaned up button held timer.", TraceLevel.Extended);
                }
                else
                    TraceWarning("DestroyHoldTimer() called but timer is not active.", TraceLevel.Extended);
            }
            catch (Exception ex)
            {
                TraceException("DestroyHoldTimer() exception caught.", ex);
            }
        }

        private void HoldTimerCallback(object o)
        {
            try
            {
                DigitalJoin join = (DigitalJoin)o;
                join.Value = DigitalJoinState.Held;

                if (join.Handler != null)
                    join.Handler(join);
                else
                    DigitalJoinHandler(join);
            }
            catch (Exception ex)
            {
                TraceException("HoldTimerCallback() exception caught.", ex);
            }
        }

        #endregion
    }
}
