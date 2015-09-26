using System;
using System.Collections.Generic;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>This action sets a panel indicator.</summary>
	internal class PanelAction : Action {
		internal int Index;
		internal int[] Values;
		internal double Interval;
		internal PanelAction(Script script, int index, int value) {
			this.Script = script;
			this.Index = index;
			this.Values = new int[] { value };
			this.Interval = 0.0;
		}
		internal PanelAction(Script script, int index, int[] values, double interval) {
			this.Script = script;
			this.Index = index;
			this.Values = values;
			this.Interval = interval;
		}
		internal override void Perform() {
			if (this.Index >= 0 & this.Index < this.Script.Plugin.Panel.Length) {
				Plugin.Blink find = this.Script.Plugin.Blinks.Find((Plugin.Blink blink) => { return blink.Index == this.Index; });
				if (find != null) {
					if (this.Values.Length == 1) {
						this.Script.Plugin.Blinks.Remove(find);
						this.Script.Plugin.Panel[this.Index] = this.Values[0];
					} else {
						find.Values = this.Values;
						find.Interval = this.Interval;
					}
				} else if (this.Values.Length == 1) {
					this.Script.Plugin.Panel[this.Index] = this.Values[0];
				} else {
					this.Script.Plugin.Blinks.Add(new Plugin.Blink(this.Index, this.Values, this.Interval));
				}
			}
		}
	}
	
	/// <summary>This action plays a sound.</summary>
	internal class SoundAction : Action {
		internal int Index;
		internal double Volume;
		internal double Pitch;
		internal bool Looped;
		internal SoundAction(Script script, int index, double volume, double pitch, bool looped) {
			this.Script = script;
			this.Index = index;
			this.Volume = volume;
			this.Pitch = pitch;
			this.Looped = looped;
		}
		internal override void Perform() {
			if (!this.Script.Plugin.Sounds.Exists((Plugin.Sound sound) => { return sound.Index == this.Index; })) {
				this.Script.Plugin.Sounds.Add(new Plugin.Sound(this.Index, this.Volume, this.Pitch, this.Looped));
			}
		}
	}
	
	/// <summary>This action stops a sound.</summary>
	internal class StopSoundAction : Action {
		internal int Index;
		internal StopSoundAction(Script script, int index) {
			this.Script = script;
			this.Index = index;
		}
		internal override void Perform() {
			Plugin.Sound find = this.Script.Plugin.Sounds.Find((Plugin.Sound sound) => { return sound.Index == this.Index; });
			if (find != null) {
				find.Handle.Stop();
				this.Script.Plugin.Sounds.Remove(find);
			}
		}
	}
	
	/// <summary>This action overrides the handles.</summary>
	internal class HandlesAction : Action {
		internal int? Reverser;
		internal int? PowerNotch;
		internal int? BrakeNotch;
		internal double? BrakePercentage;
		internal HandlesAction(Script script, int? reverser, int? powerNotch, int? brakeNotch, double? brakePercentage) {
			this.Script = script;
			this.Reverser = reverser;
			this.PowerNotch = powerNotch;
			this.BrakeNotch = brakeNotch;
			this.BrakePercentage = brakePercentage;
		}
		internal override void Perform() {
			if (!this.Script.Handles.Contains(this)) {
				this.Script.Handles.Add(this);
			}
		}
	}
	
	/// <summary>This action releases the handles.</summary>
	internal class ReleaseHandlesAction : Action {
		internal ReleaseHandlesAction(Script script) {
			this.Script = script;
		}
		internal override void Perform() {
			if (this.Script.Handles.Count != 0) {
				this.Script.Handles = new List<HandlesAction>();
			}
		}
	}
	
	internal class AbortCountdownAction : Action {
		internal AbortCountdownAction(Script script) {
			this.Script = script;
		}
		internal override void Perform() {
			this.Script.Events.RemoveAll((Event e) => { return e is CountdownEvent; });
		}
	}
	
}