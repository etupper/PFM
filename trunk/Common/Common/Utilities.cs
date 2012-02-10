using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace Common
{

    public static class Utilities
    {
        public static void DisposeHandlers(Control targetControl) {
			EventInfo[] events = targetControl.GetType ().GetEvents ();
			if (events.Length > 0) {
				foreach (EventInfo info in events) {
					MethodInfo raiseMethod = info.GetRaiseMethod ();
					if (raiseMethod != null) {
						Delegate handler = Delegate.CreateDelegate (typeof(EventHandler), targetControl, raiseMethod.Name, false);
						info.RemoveEventHandler (targetControl, handler);
					}
				}
			}
			Control.ControlCollection controls = targetControl.Controls;
			if (controls.Count > 0) {
				foreach (Control control in controls) {
					DisposeHandlers (control);
				}
			}
		}
	}
}

