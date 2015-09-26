using System;
using System.Collections.Generic;

namespace Plugin {
	internal partial class Script {
		
		// --- members ---
		
		internal Plugin Plugin;
		
		internal List<Event> Events;
		
		internal List<HandlesAction> Handles = new List<HandlesAction>();
		
		
		// --- constructors ---
		
		internal Script(Plugin plugin) {
			this.Plugin = plugin;
			this.Events = new List<Event>();
		}
		
	}
}