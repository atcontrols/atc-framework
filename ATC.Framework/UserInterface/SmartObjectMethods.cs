using ATC.Framework.Debugging;
using Crestron.SimplSharpPro;
using System;
using System.Text.RegularExpressions;

namespace ATC.Framework.UserInterface
{
    internal static class SmartObjectMethods
    {
        #region SetValue methods
        public static bool SetValue(SmartObject so, string name, bool value)
        {
            try
            {
                if (so.BooleanInput.Contains(name))
                {
                    so.BooleanInput[name].BoolValue = value;
                    Trace(String.Format("SmartObjectMethods.SetValue() set object: {0}, boolean input \"{1}\" to: {2}.", so.ID, name, value));
                    return true;
                }
                else
                {
                    Trace(String.Format("SmartObjectMethods.SetValue() smart object: {0} does not contain digital signal: \"{1}\"", so.ID, name));
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetValue() exception caught." + ex.Message);
                return false;
            }
        }

        public static bool SetValue(SmartObject so, string name, ushort value)
        {
            try
            {
                if (so.UShortInput.Contains(name))
                {
                    so.UShortInput[name].UShortValue = value;
                    Trace(String.Format("SmartObjectMethods.SetValue() set object: {0}, ushort input \"{1}\" to: {2}.", so.ID, name, value));
                    return true;
                }
                else
                {
                    Trace(String.Format("SmartObjectMethods.SetValue() smart object: {0} does not contain analog signal: \"{1}\"", so.ID, name));
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetValue() exception caught." + ex.Message);
                return false;
            }
        }

        public static bool SetValue(SmartObject so, string name, string value)
        {
            try
            {
                if (so.StringInput.Contains(name))
                {
                    so.StringInput[name].StringValue = value;
                    Trace(String.Format("SmartObjectMethods.SetValue() set object: {0}, string input \"{1}\" to: {2}.", so.ID, name, value));
                    return true;
                }
                else
                {
                    Trace(String.Format("SmartObjectMethods.SetValue() smart object: {0} does not contain serial signal: \"{1}\"", so.ID, name));
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetValue() exception caught." + ex.Message);
                return false;
            }
        }

        public static bool SetValue(SmartObject so, string name, int value)
        {
            return SetValue(so, name, (ushort)value);
        }

        public static bool SetValue(SmartObject so, int index, ushort value)
        {
            return SetValue(so, "an_fb" + index, value);
        }

        public static bool SetValue(SmartObject so, int index, int value)
        {
            return SetValue(so, index, (ushort)value);
        }
        #endregion

        #region Convenience methods
        public static bool SetSelected(SmartObject so, int index, bool value)
        {
            try
            {
                Trace(String.Format("SmartObjectMethods.SetSelected() setting object index: {0}, item index: {1}, to value: {2}", so.ID, index, value));

                if (SetValue(so, String.Format("Item {0} Selected", index), value))
                    return true;
                else if (SetValue(so, String.Format("fb{0}", index), value))
                    return true;
                else if (SetValue(so, String.Format("Tab Button {0} Select", index), value))
                    return true;
                else
                {
                    Trace("SmartObjectMethods.SetSelected() couldn't find a matching name.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetSelected() exception caught." + ex.Message);
                return false;
            }
        }

        public static bool SetEnabled(SmartObject so, int index, bool value)
        {
            try
            {
                Trace(String.Format("SmartObjectMethods.SetEnabled() setting object index: {0}, item index: {1}, to value: {2}", so.ID, index, value));

                if (SetValue(so, String.Format("Item {0} Enabled", index), value))
                    return true;
                else
                {
                    Trace("SmartObjectMethods.SetEnabled() couldn't find a matching name.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetEnabled() exception caught." + ex.Message);
                return false;
            }
        }

        public static bool SetVisible(SmartObject so, int index, bool value)
        {
            try
            {
                Trace(String.Format("SmartObjectMethods.SetVisible() setting object index: {0}, item index: {1}, to value: {2}", so.ID, index, value));

                if (SetValue(so, String.Format("Item {0} Visible", index), value))
                    return true;
                else
                {
                    Trace("SmartObjectMethods.SetVisible() couldn't find a matching name.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetItemCount() exception caught." + ex.Message);
                return false;
            }
        }

        public static bool SetText(SmartObject so, int index, string value)
        {
            try
            {
                Trace(String.Format("SmartObjectMethods.SetText() setting object index: {0}, item index: {1}, to value: \"{2}\"", so.ID, index, value));

                if (SetValue(so, String.Format("Set Item {0} Text", index), value))
                    return true;
                else if (SetValue(so, "text-o" + index, value))
                    return true;
                else
                {
                    Trace("SmartObjectMethods.SetText() couldn't find a matching name.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetItemCount() exception caught." + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Attempt to set the number of items for a given SmartObject
        /// </summary>
        /// <param name="so">The SmartObject to set</param>
        /// <param name="number">The number of items to set</param>
        /// <returns>True on success, false on fail</returns>
        public static bool SetNumberOfItems(SmartObject so, int number)
        {
            try
            {
                Trace(String.Format("SmartObjectMethods.SetNumberOfItems() setting object index: {0}, to value: {1}", so.ID, number));

                if (SetValue(so, "Set Number of Items", number))
                    return true;
                else
                {
                    Trace("SmartObjectMethods.SetNumberOfItems() set number of items not found for this SmartObject.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.SetNumberOfItems() exception caught." + ex.Message);
                return false;
            }
        }
        #endregion

        /// <summary>
        /// Attempts to extract a button index from a given SmartGraphics button name.
        /// </summary>
        /// <param name="name">The SG button name to process.</param>
        /// <returns>Positive integer on success.</returns>
        public static int GetButtonIndex(string name)
        {
            try
            {
                if (name.Contains("Clicked")) // ignore clicked event
                    return -1;
                else if (name.StartsWith("Item") && name.EndsWith("Pressed")) // example: "Item 1 Pressed"
                {
                    string[] words = name.Split(' '); // split button string into words
                    return Int32.Parse(words[1]); // get index from middle word
                }
                else if (name.StartsWith("Tab Button") && name.EndsWith("Press")) // example: "Tab Button 3 Press"
                {
                    string[] words = name.Split(' ');
                    return Int32.Parse(words[2]);
                }
                else if (name.StartsWith("press")) // example: press6
                {
                    return Int32.Parse(name.Substring(5));
                }
                else if (Regex.IsMatch(name, @"^\d")) // check if first character is a digit
                {
                    return Int32.Parse(name);
                }
                else
                {
                    Trace(String.Format("PanelMethods.GetButtonIndex() didn't match any known pattern: \"{0}\"", name));
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Trace("SmartObjectMethods.GetButtonIndex() exception caught." + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Prints out all available signal names to the console.
        /// </summary>
        /// <param name="so"></param>
        public static void PrintSigNames(SmartObject so)
        {
            Tracer.PrintLine("SmartObject Object ID {0}, on {1}", so.ID, so.Device.ToString());
            foreach (Sig sig in so.BooleanInput)
                Tracer.PrintLine("BooleanInput Signal name: {0}", sig.Name);
            foreach (Sig sig in so.BooleanOutput)
                Tracer.PrintLine("BooleanOutput Signal name: {0}", sig.Name);
            foreach (Sig sig in so.StringInput)
                Tracer.PrintLine("StringInput Signal name: {0}", sig.Name);
            foreach (Sig sig in so.StringOutput)
                Tracer.PrintLine("StringOutput Signal name: {0}", sig.Name);
            foreach (Sig sig in so.UShortInput)
                Tracer.PrintLine("UShortInput Signal name: {0}", sig.Name);
            foreach (Sig sig in so.UShortOutput)
                Tracer.PrintLine("UShortOutput Signal name: {0}", sig.Name);
        }

        #region Tracing
        public static bool TraceEnabled { get; set; }

        private static void Trace(string message)
        {
            if (TraceEnabled)
                Tracer.PrintLine(message);
        }
        #endregion
    }
}
