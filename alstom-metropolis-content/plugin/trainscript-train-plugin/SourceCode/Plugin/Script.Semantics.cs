using System;
using System.Collections.Generic;
using System.IO;

namespace Plugin {
	internal partial class Script {
		
		/// <summary>Parses a script file semantically.</summary>
		/// <param name="script">The script to which the semantics will be applied.</param>
		/// <param name="element">The no-name top-level element.</param>
		/// <param name="title">The title of the script file.</param>
		/// <returns>The run-once event that encapsulates all top-level actions.</returns>
		private static Tag ParseSemantics(Script script, Element element, string title) {
			if (element.Name == null) {
				// --- top-level ---
				if (element.Parameters.Count != 0) {
					throw new InvalidDataException("Top-level parameters are invalid in file " + title);
				}
				Event e = new RunOnceEvent(script);
				foreach (Element subelement in element.Elements) {
					Tag tag = ParseSemantics(script, subelement, title);
					if (tag is Condition) {
						if (element.Parameters.Count != 0) {
							throw new InvalidDataException("Top-level conditions are invalid in file " + title);
						}
					} else if (tag is Action) {
						e.Actions.Add((Action)tag);
					}
				}
				return e;
			} else if (element.Name == "event") {
				// --- event ---
				string name;
				if (element.Parameters.TryGetValue("name", out name)) {
					Event e;
					if (
						name == "key down" || name == "keydown" ||
						name == "key press" || name == "key pressed" || name == "keypress" || name == "keypressed" ||
						name == "key up" || name == "keyup"
					) {
						// --- key down / key press / key up ---
						OpenBveApi.Runtime.VirtualKeys key = OpenBveApi.Runtime.VirtualKeys.S;
						bool keyDefined = false;
						foreach (KeyValuePair<string, string> pair in element.Parameters) {
							switch (pair.Key) {
								case "name":
									break;
								case "type":
									key = GetKey(pair.Value);
									keyDefined = true;
									break;
								default:
									throw new InvalidDataException("Unsupported parameter '" + pair.Key + "' in tag [" + element.Name + "] in file " + title);
							}
						}
						if (!keyDefined) {
							throw new InvalidDataException("Missing parameter 'type' in tag [" + element.Name + "] in file " + title);
						}
						if (name == "key down" || name == "keydown") {
							e = new KeyDownEvent(script, key);
						} else if (name == "key press" || name == "key pressed" || name == "keypress" || name == "keypressed") {
							e = new KeyPressEvent(script, key);
						} else if (name == "key up" || name == "keyup") {
							e = new KeyUpEvent(script, key);
						} else {
							throw new InvalidOperationException();
						}
					} else if (name == "beacon") {
						// --- beacon ---
						RangeCollection type = RangeCollection.All;
						RangeCollection signal = RangeCollection.All;
						foreach (KeyValuePair<string, string> pair in element.Parameters) {
							switch (pair.Key) {
								case "name":
									break;
								case "type":
									type = GetRangeCollection(pair.Value);
									break;
								case "signal":
									signal = GetRangeCollection(pair.Value);
									break;
								default:
									throw new InvalidDataException("Unsupported parameter '" + pair.Key + "' in tag [" + element.Name + "] in file " + title);
							}
						}
						e = new BeaconEvent(script, type, signal);
					} else if (name == "count down" || name == "countdown") {
						// --- countdown ---
						double value = 0.0;
						foreach (KeyValuePair<string, string> pair in element.Parameters) {
							switch (pair.Key) {
								case "name":
									break;
								case "interval":
									value = GetTime(pair.Value);
									break;
								default:
									throw new InvalidDataException("Unsupported parameter '" + pair.Key + "' in tag [" + element.Name + "] in file " + title);
							}
						}
						e = new CountdownEvent(script, value);
					} else {
						throw new InvalidDataException("Unsupported event '" + name + "' in file " + title);
					}
					foreach (Element subelement in element.Elements) {
						Tag tag = ParseSemantics(script, subelement, title);
						if (tag is Condition) {
							e.Conditions.Add((Condition)tag);
						} else if (tag is Action) {
							e.Actions.Add((Action)tag);
						}
					}
					return e;
				} else {
					// --- unsupported ---
					throw new InvalidDataException("Missing parameter 'name' in tag [event] in file " + title);
				}
			} else if (element.Name == "panel") {
				// --- panel ---
				if (element.Elements.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept subtags in file " + title);
				}
				int index = 0;
				int[] values = new int[] { 1 };
				double interval = 0.0;
				foreach (KeyValuePair<string, string> pair in element.Parameters) {
					switch (pair.Key) {
						case "index":
							index = GetAbstractInteger(pair.Value);
							break;
						case "value":
						case "values":
							values = GetAbstractIntegers(pair.Value);
							break;
						case "interval":
							interval = GetTime(pair.Value);
							break;
						default:
							throw new InvalidDataException("Unsupported parameter '" + pair.Key + "' in tag [" + element.Name + "] in file " + title);
					}
				}
				if (values.Length > 1 & interval > 0.0) {
					return new PanelAction(script, index, values, interval);
				} else {
					return new PanelAction(script, index, values[0]);
				}
			} else if (element.Name == "sound") {
				// --- sound ---
				if (element.Elements.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept subtags in file " + title);
				}
				int index = 0;
				double volume = 1.0;
				double pitch = 1.0;
				bool looped = false;
				foreach (KeyValuePair<string, string> pair in element.Parameters) {
					switch (pair.Key) {
						case "index":
							index = GetAbstractInteger(pair.Value);
							break;
						case "volume":
							volume = GetRatio(pair.Value);
							break;
						case "pitch":
							pitch = GetRatio(pair.Value);
							break;
						case "looped":
						case "looping":
							looped = GetBoolean(pair.Value);
							break;
						default:
							throw new InvalidDataException("Unsupported parameter '" + pair.Key + "' in tag [" + element.Name + "] in file " + title);
					}
				}
				return new SoundAction(script, index, volume, pitch, looped);
			} else if (element.Name == "stop_sound") {
				// --- stop sound ---
				if (element.Elements.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept subtags in file " + title);
				}
				int index = 0;
				foreach (KeyValuePair<string, string> pair in element.Parameters) {
					switch (pair.Key) {
						case "index":
							index = GetAbstractInteger(pair.Value);
							break;
						default:
							throw new InvalidDataException("Unsupported parameter '" + pair.Key + "' in tag [" + element.Name + "] in file " + title);
					}
				}
				return new StopSoundAction(script, index);
			} else if (element.Name == "handles") {
				// --- handles ---
				if (element.Elements.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept subtags in file " + title);
				}
				int? reverser = null;
				int? powerNotch = null;
				int? brakeNotch = null;
				double? brakePercentage = null;
				foreach (KeyValuePair<string, string> pair in element.Parameters) {
					switch (pair.Key) {
						case "reverser":
							reverser = GetReverser(pair.Value, script);
							break;
						case "power":
							powerNotch = GetPower(pair.Value, script);
							break;
						case "brakes":
							double temp;
							brakeNotch = GetBrakes(pair.Value, script, out temp);
							brakePercentage = temp;
							break;
						default:
							throw new InvalidDataException("Unsupported parameter '" + pair.Key + "' in tag [" + element.Name + "] in file " + title);
					}
				}
				return new HandlesAction(script, reverser, powerNotch, brakeNotch, brakePercentage);
			} else if (element.Name == "release_handles") {
				// --- release handles ---
				if (element.Elements.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept subtags in file " + title);
				}
				if (element.Parameters.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept parameters in file " + title);
				}
				return new ReleaseHandlesAction(script);
			} else if (element.Name == "abort_countdown") {
				// --- abort handles ---
				if (element.Elements.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept subtags in file " + title);
				}
				if (element.Parameters.Count != 0) {
					throw new InvalidDataException("Tag [" + element.Name + "] does not accept parameters in file " + title);
				}
				return new AbortCountdownAction(script);
			} else {
				// --- unsupported ---
				throw new InvalidDataException("Unsupported tag [" + element.Name + "] in file " + title);
			}
		}
		
	}
}