using System;
using System.Collections.Generic;
using OpenBveApi.Runtime;
using Plugin;

namespace Plugin {
	
	/// <summary>Represents an abstract tag.</summary>
	internal abstract class Tag {
		internal Script Script;
	}
	
	/// <summary>Represents an abstract condition.</summary>
	internal abstract class Condition : Tag {
		internal abstract bool Evaluate();
	}
	
	/// <summary>Represents an abstract action.</summary>
	internal abstract class Action : Tag {
		internal abstract void Perform();
	}
	
	/// <summary>Represents an abstract event.</summary>
	/// <remarks>When an event is performed, the event is added to the script's event handler.</remarks>
	/// <remarks>When an event is triggered, its actions are performed if its conditions are met.</remarks>
	internal class Event : Action {
		// --- members ---
		internal List<Condition> Conditions;
		internal List<Action> Actions;
		// --- functions ---
		/// <summary>Adds this event to the script's event handler.</summary>
		internal override void Perform() {
			if (!this.Script.Events.Contains(this)) {
				this.Script.Events.Add(this);
			}
		}
		/// <summary>Performs the event's actions if the event's conditions are met.</summary>
		internal void Trigger() {
			foreach (Condition condition in this.Conditions) {
				if (!condition.Evaluate()) return;
			}
			foreach (Action action in this.Actions) {
				action.Perform();
			}
		}
	}
	
	internal struct RangeCollection {
		internal Range[] Elements;
		internal RangeCollection(Range[] elements) {
			this.Elements = elements;
		}
		internal bool IsMember(int value) {
			for (int i = 0; i < this.Elements.Length; i++) {
				if (this.Elements[i].IsMember(value)) return true;
			}
			return false;
		}
		internal static readonly RangeCollection All = new RangeCollection(new Range[] { new Range(int.MinValue, int.MaxValue) });
	}
	
	internal struct Range {
		internal int Start;
		internal int End;
		internal Range(int start, int end) {
			this.Start = start;
			this.End = end;
		}
		internal bool IsMember(int value) {
			return this.Start <= value & value <= this.End;
		}
	}
	
}