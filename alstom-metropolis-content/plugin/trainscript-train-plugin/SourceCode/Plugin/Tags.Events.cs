using System;
using System.Collections.Generic;
using OpenBveApi.Runtime;

namespace Plugin {
	
	internal class RunOnceEvent : Event {
		internal RunOnceEvent(Script script) {
			this.Script = script;
			this.Conditions = new List<Condition>();
			this.Actions = new List<Action>();
		}
	}
	
	internal class KeyDownEvent : Event {
		internal VirtualKeys Key;
		internal KeyDownEvent(Script script, VirtualKeys key) {
			this.Script = script;
			this.Conditions = new List<Condition>();
			this.Actions = new List<Action>();
			this.Key = key;
		}
	}

	internal class KeyPressEvent : Event {
		internal VirtualKeys Key;
		internal KeyPressEvent(Script script, VirtualKeys key) {
			this.Script = script;
			this.Conditions = new List<Condition>();
			this.Actions = new List<Action>();
			this.Key = key;
		}
	}

	internal class KeyUpEvent : Event {
		internal VirtualKeys Key;
		internal KeyUpEvent(Script script, VirtualKeys key) {
			this.Script = script;
			this.Conditions = new List<Condition>();
			this.Actions = new List<Action>();
			this.Key = key;
		}
	}
	
	internal class BeaconEvent : Event {
		internal RangeCollection Type;
		internal RangeCollection Signal;
		internal BeaconEvent(Script script, RangeCollection type, RangeCollection signal) {
			this.Script = script;
			this.Conditions = new List<Condition>();
			this.Actions = new List<Action>();
			this.Type = type;
			this.Signal = signal;
		}
	}
	
	internal class CountdownEvent : Event {
		internal double Current;
		internal double Maximum;
		internal CountdownEvent(Script script, double maximum) {
			this.Script = script;
			this.Conditions = new List<Condition>();
			this.Actions = new List<Action>();
			this.Current = 0.0;
			this.Maximum = maximum;
		}
		internal override void Perform() {
			if (!this.Script.Events.Contains(this)) {
				this.Current = 0.0;
				this.Script.Events.Add(this);
			}
		}
	}

}