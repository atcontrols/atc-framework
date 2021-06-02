using Crestron.SimplSharpPro;
using System;

namespace ATC.Framework.Communications
{
    public class ComPortTransport : Transport
    {
        #region Class variables
        ComPort port;
        ComPort.ComPortSpec spec;
        #endregion

        #region Constructor
        public ComPortTransport(ComPort port, ComPort.ComPortSpec spec)
            : base()
        {
            this.port = port;
            this.spec = spec;
        }
        #endregion

        #region Public methods
        public override bool Connect()
        {
            return PortRegister(true);
        }

        public override bool Disconnect()
        {
            return PortRegister(false);
        }

        public override bool Send(string s)
        {
            try
            {
                if (ConnectionState == ConnectionState.Connected)
                {
                    Trace(String.Format("Send() sending: \"{0}\"", s.Trim()));
                    port.Send(s);
                    return true;
                }
                else
                {
                    TraceError("Send() cannot send as port not registered.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                TraceException("Send() exception caught.", ex);
                return false;
            }
        }
        #endregion

        #region Internal methods
        bool PortRegister(bool value)
        {
            try
            {
                if (value) // register port
                {
                    // attempt to register
                    eDeviceRegistrationUnRegistrationResponse response = port.Register();

                    if (response == eDeviceRegistrationUnRegistrationResponse.Success)
                    {
                        Trace("PortRegister() port registered successfully.");

                        port.SetComPortSpec(spec);
                        port.SerialDataReceived += new ComPortDataReceivedEvent(PortSerialDataReceived);

                        RaiseConnectionStateEvent(ConnectionState.Connected, "Port registered successfully.");

                        return true;
                    }
                    else
                    {
                        TraceError("ComPortTransport() port failed to register: " + response);
                        return false;
                    }
                }
                else // unregister port
                {
                    if (port.Registered)
                    {
                        eDeviceRegistrationUnRegistrationResponse response = port.UnRegister();
                        if (response == eDeviceRegistrationUnRegistrationResponse.Success)
                        {
                            Trace("PortRegister() port unregistered successfully.");
                            RaiseConnectionStateEvent(ConnectionState.NotConnected, "Port unregistered successfully.");

                            // unsubscribe from events
                            port.SerialDataReceived -= PortSerialDataReceived;

                            return true;
                        }
                        else
                        {
                            TraceError("PortRegister() port failed to unregister: " + response);
                            return false;
                        }
                    }
                    else
                    {
                        TraceError("PortRegister() cannot unregister as port is not currently registered.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException("PortRegister() exception caught.", ex);
                return false;
            }
        }
        #endregion

        #region Port event callbacks
        void PortSerialDataReceived(ComPort port, ComPortSerialDataEventArgs args)
        {
            Trace(String.Format("PortSerialDataReceived() received string: \"{0}\"", args.SerialData.Trim()));
            RaiseResponseReceivedEvent(args.SerialData);
        }
        #endregion

        #region Object cleanup
        /// <summary>
        /// Free up any unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PortRegister(false);
            }
        }

        /// <summary>
        /// Object destructor
        /// </summary>
        ~ComPortTransport()
        {
            Trace("~ComPortTransport() object destructor called.");
            Dispose(false);
        }
        #endregion
    }
}