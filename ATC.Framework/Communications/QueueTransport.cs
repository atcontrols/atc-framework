using Crestron.SimplSharp;
using System;
using System.Collections.Generic;

namespace ATC.Framework.Communications
{
    public class QueueTransport : Transport, IDisposable
    {
        #region Class variables
        private readonly Queue<String> queue = new Queue<String>();
        CTimer timer, autoDisconnectTimer;
        Transport transport;
        long timerDelay;
        bool awaitingResponse;
        #endregion

        #region Constants
        public const long DelayIntervalDefault = 100;
        public const long ResponseTimeoutDefault = 5000;
        public const long AutoDisconnectDelayDefault = 2000;
        #endregion

        #region Properties
        /// <summary>
        /// Returns true of object is initialized correctly.
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// Automatically connect if not connected and a string is requested to be sent.
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// Automatically disconnect after a delay if no items remain in the queue.
        /// </summary>
        public bool AutoDisconnect { get; set; }

        /// <summary>
        /// How long in milliseconds to wait until disconnecting after last response received.
        /// </summary>
        public long AutoDisconnectDelay { get; set; }

        public QueueTransportMode Mode { get; private set; }

        /// <summary>
        /// Number of elements in the queue.
        /// </summary>
        public int QueueDepth
        {
            get
            {
                if (queue == null)
                    return 0;
                else
                    return queue.Count;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Defaults to DelayInterval mode with default delay interval.
        /// </summary>
        /// <param name="transport">The transport to use internally.</param>
        public QueueTransport(Transport transport)
        {
            Initialized = Initialize(QueueTransportMode.DelayInterval, transport, DelayIntervalDefault);
        }

        /// <summary>
        /// Specify a mode and transport type.
        /// </summary>
        /// <param name="mode">Mode of operation for the QueueTransport to use.</param>
        /// <param name="transport">The transport to use internally.</param>
        public QueueTransport(QueueTransportMode mode, Transport transport)
        {
            switch (mode)
            {
                case QueueTransportMode.DelayInterval:
                    Initialized = Initialize(mode, transport, DelayIntervalDefault);
                    break;
                case QueueTransportMode.AdvanceOnResponse:
                    Initialized = Initialize(mode, transport, ResponseTimeoutDefault);
                    break;
                case QueueTransportMode.ManualAdvance:
                    Initialized = Initialize(mode, transport, 0);
                    break;
            }
        }

        /// <summary>
        /// Specify a mode, transport and time for the internal timer.
        /// </summary>
        /// <param name="mode">Mode of operation for the QueueTransport to use.</param>
        /// <param name="transport">The transport to use internally.</param>
        /// <param name="timerDelay">The time parameter to use for either delay interval or response timeout.</param>
        public QueueTransport(QueueTransportMode mode, Transport transport, long timerDelay)
        {
            Initialized = Initialize(mode, transport, timerDelay);
        }

        bool Initialize(QueueTransportMode mode, Transport transport, long timerDelay)
        {
            if (transport != null)
            {
                Mode = mode;
                this.timerDelay = timerDelay;
                AutoDisconnectDelay = AutoDisconnectDelayDefault;

                SetInternalTransport(transport);

                return true;
            }
            else
            {
                TraceError("Initialize() transport cannot be null.");

                return false;
            }
        }
        #endregion

        /// <summary>
        /// Specify the internal transport for this QueueTransport object to use.
        /// </summary>
        /// <param name="newTransport">Transport object to use.</param>
        public void SetInternalTransport(Transport newTransport)
        {
            // dispose of any existing internal transport
            if (transport != null)
            {
                transport.ConnectionStateCallback -= TransportConnectionStatusCallback;
                transport.ResponseReceivedCallback -= TransportReceiveDataCallback;
                transport.Dispose();
                transport = null;
            }

            // reset queue
            QueueReset();

            // update internal transport
            if (newTransport != null)
            {
                transport = newTransport;
                transport.ConnectionStateCallback += new EventHandler<ConnectionStateEventArgs>(TransportConnectionStatusCallback);
                transport.ResponseReceivedCallback += new EventHandler<ResponseReceivedEventArgs>(TransportReceiveDataCallback);
            }
            else
                transport = null;
        }

        #region Interface methods
        /// <summary>
        /// Adds specified string to the queue.
        /// </summary>
        /// <param name="command">The string to add.</param>
        public override bool Send(string s)
        {
            try
            {
                // add string to queue
                queue.Enqueue(s);
                Trace(String.Format("EnqueueCommand() added: \"{0}\" to queue. Size is now: {1}", s.Trim(), QueueDepth));

                // perform correct action depending on mode
                switch (Mode)
                {
                    case QueueTransportMode.DelayInterval:
                    case QueueTransportMode.AdvanceOnResponse:
                        if (!TimerActive)
                            ProcessQueue();
                        return true;
                    case QueueTransportMode.ManualAdvance:
                        return true;
                    default:
                        TraceError("QueueAdd() unhandled mode: " + Mode);
                        return false;
                }
            }
            catch (Exception ex)
            {
                TraceException("EnqueueCommand() exception caught.", ex);
                return false;
            }
        }

        /// <summary>
        /// Instruct the transport to connect.
        /// </summary>
        /// <returns></returns>
        public override bool Connect()
        {
            try
            {
                return transport.Connect();
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                return false;
            }
        }

        /// <summary>
        /// Instruct the transport to disconnect
        /// </summary>
        /// <returns></returns>
        public override bool Disconnect()
        {
            try
            {
                return transport.Disconnect();
            }
            catch (Exception ex)
            {
                TraceException("Connect() exception caught.", ex);
                return false;
            }
        }
        #endregion

        #region Queue methods
        /// <summary>
        /// Manually advance the queue (to be used in ManualAdvance mode).
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        public bool AdvanceQueue()
        {
            if (Mode != QueueTransportMode.ManualAdvance)
            {
                TraceError("QueueAdvance() should only be called in ManualAdvance mode.");
                return false;
            }
            else
            {
                if (QueueDepth >= 1)
                {
                    return ProcessQueue();
                }
                else
                {
                    TraceError("QueueAdvance() there is nothing in the queue.");
                    return false;
                }
            }
        }

        /// <summary>
        /// Read first item in the queue.
        /// </summary>
        /// <returns>The first item in the queue.</returns>
        public string GetFirstItem()
        {
            if (queue.Count > 0)
                return queue.Peek();

            return null;
        }

        /// <summary>
        /// Remove the first item from the queu.
        /// </summary>
        /// <returns>The first item in the queue.</returns>
        public string RemoveFirstItem()
        {
            if (queue.Count > 0)
                return queue.Dequeue();

            return null;
        }

        /// <summary>
        /// Attempt to send the first item in the queue.
        /// </summary>
        /// <returns>True on success, false on fail.</returns>
        private bool ProcessQueue()
        {
            try
            {
                if (QueueDepth == 0) // queue is null or empty (nothign to do)
                {
                    if (TimerActive)
                    {
                        Trace("QueueProcess() queue is empty. Stopping timer.");
                        TimerReset();
                        return true;
                    }
                    else
                    {
                        Trace("QueueProcess() queue is empty. No action needed.");
                        return false;
                    }
                }
                else if (transport.ConnectionState == ConnectionState.NotConnected) // transport is not currently connected
                {
                    if (AutoConnect)
                    {
                        Trace("QueueProcess() transport is not currently connected. Instructing transport to connect.");
                        return transport.Connect();
                    }
                    else
                    {
                        Trace("QueueProcess() transport is not currently connected. Enable AutoConnect to automatically connect.");
                        return false;
                    }
                }
                else if (transport.ConnectionState == ConnectionState.Connecting) // transport is busy
                {
                    Trace("QueueProcess() transport is attemping connection. No action taken.");
                    return false;
                }
                else // all good, try send the first item
                {
                    if (Mode == QueueTransportMode.AdvanceOnResponse)
                    {
                        awaitingResponse = true;
                        TimerCreate(timerDelay);
                    }

                    string s = Mode == QueueTransportMode.ManualAdvance ?
                        queue.Peek() : queue.Dequeue(); // dont remove first item in manual advance mode
                    bool success = transport.Send(s);

                    if (success)
                        Trace(String.Format("QueueProcess() sent: \"{0}\" successfully. Queue size: {1}.", s.Trim(), QueueDepth));
                    else
                        TraceError(String.Format("QueueProcess() could not send: \"{0}\". Queue size: {1}.", s.Trim(), QueueDepth));

                    return success;
                }
            }
            catch (Exception ex)
            {
                TraceException("QueueProcess() exception caught.", ex);
                return false;
            }
        }

        /// <summary>
        /// Resets objects internal queue.
        /// </summary>
        public void QueueReset()
        {
            TimerReset();
            queue.Clear();
        }
        #endregion

        #region Timer methods
        bool TimerActive { get { return timer != null; } }

        void TimerCreate(long dueTime)
        {
            if (!TimerActive)
            {
                timer = new CTimer(TimerCallback, timerDelay);
                Trace(String.Format("TimerCreate() timer created with dueTime: {0}", dueTime));
            }
            else
                TraceError("TimerCreate() cannot create new timer as one already exists.");
        }

        void TimerCreate(long dueTime, long repeatPeriod)
        {
            if (!TimerActive)
            {
                timer = new CTimer(TimerCallback, null, dueTime, repeatPeriod);
                Trace(String.Format("TimerCreate() timer created with dueTime: {0} and repeatPeriod: {1}", dueTime, repeatPeriod));
            }
            else
                TraceError("TimerCreate() cannot create new timer as one already exists.");
        }

        void TimerCallback(object o)
        {
            switch (Mode)
            {
                case QueueTransportMode.DelayInterval:
                    ProcessQueue();
                    break;
                case QueueTransportMode.AdvanceOnResponse:
                    if (awaitingResponse)
                    {
                        TraceWarning("TimerCallback() timeout reached while waiting for a response.");
                        awaitingResponse = false;
                    }
                    else
                        TraceError("TimerCallback() timeout reached, but not waiting for a response.");
                    TimerReset();
                    ProcessQueue();
                    break;
                default:
                    TraceError("TimerCallback() unhandled mode: " + Mode);
                    break;
            }
        }

        void TimerReset()
        {
            if (TimerActive)
            {
                Trace("TimerReset() timer destroyed.");
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }
        #endregion

        #region Transport callbacks
        void TransportConnectionStatusCallback(object sender, ConnectionStateEventArgs args)
        {
            Trace("TransportConnectionStatusCallback() connection state: " + args.State);

            switch (args.State)
            {
                case ConnectionState.Connected:

                    switch (Mode)
                    {
                        case QueueTransportMode.DelayInterval:
                            if (!TimerActive)
                                TimerCreate(0, timerDelay); // start timer for delay interval
                            break;
                        case QueueTransportMode.AdvanceOnResponse:
                        case QueueTransportMode.ManualAdvance:
                            if (AutoConnect)
                                ProcessQueue(); // send first item in queue automatically in autoconnect mode
                            break;

                    }

                    AutoDisconnectHandler();
                    break;

                case ConnectionState.NotConnected:
                    QueueReset(); // reset queue on disconnect
                    break;
            }

            RaiseConnectionStateEvent(args);
        }

        void TransportReceiveDataCallback(object sender, ResponseReceivedEventArgs args)
        {
            Trace("TransportReceiveDataCallback() received response. Length: " + args.Response.Length);

            RaiseResponseReceivedEvent(args);

            // advance in queue if in correct mode
            if (Mode == QueueTransportMode.AdvanceOnResponse)
            {
                awaitingResponse = false;
                TimerReset();
                ProcessQueue();
            }

            AutoDisconnectHandler();
        }
        #endregion

        #region Auto disconnect
        /// <summary>
        /// Starts auto disconnect timer
        /// </summary>
        void AutoDisconnectHandler()
        {
            // handle automatic disconnection
            if (AutoDisconnect)
            {
                // stop active timer
                if (autoDisconnectTimer != null)
                {
                    autoDisconnectTimer.Stop();
                    autoDisconnectTimer.Dispose();
                    autoDisconnectTimer = null;
                }

                // create new timer
                autoDisconnectTimer = new CTimer(AutoDisconnectCallback, AutoDisconnectDelay);
            }
        }

        void AutoDisconnectCallback(object o)
        {
            if (AutoDisconnect)
            {
                if (transport.ConnectionState == ConnectionState.Connected)
                {
                    Trace("AutoDisconnectCallback() automatically disconnecting.");
                    transport.Disconnect();

                    autoDisconnectTimer.Dispose();
                    autoDisconnectTimer = null;
                }
                else
                    TraceError("AutoDisconnectCallback() called but transport is not connected. Current state: " + transport.ConnectionState);
            }
            else
                TraceError("AutoDisconnectCallback() called but AutoDisconnect is not enabled.");
        }
        #endregion

        #region Object cleanup
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SetInternalTransport(null);
            }
        }

        /// <summary>
        /// Object destructor
        /// </summary>
        ~QueueTransport()
        {
            Trace("~QueueTransport() object destructor called.");
            Dispose(false);
        }
        #endregion
    }

    public enum QueueTransportMode
    {
        /// <summary>
        /// Wait a specified amount of time between consecutive sends.
        /// </summary>
        DelayInterval,

        /// <summary>
        /// Wait for a response (up to the specified timeout) before sending the next string.
        /// </summary>
        AdvanceOnResponse,

        /// <summary>
        /// Queue must be manually advanced by using the QueueProcess() method.
        /// </summary>
        ManualAdvance,
    }
}
