using System;
using System.Collections.Generic;
using OpenBveApi.Runtime;

namespace Plugin {
	/// <summary>The interface to be implemented by the plugin.</summary>
	public partial class Plugin : IRuntime {
		
		// --- classes ---
		
		internal class Blink {
			internal int Index;
			internal int[] Values;
			internal double Interval;
			internal double Counter;
			internal Blink(int index, int[] values, double interval) {
				this.Index = index;
				this.Values = values;
				this.Interval = interval;
				this.Counter = 0.0;
			}
		}
		
		internal class Sound {
			internal int Index;
			internal double Volume;
			internal double Pitch;
			internal bool Looped;
			internal SoundHandle Handle;
			internal Sound(int index, double volume, double pitch, bool looped) {
				this.Index = index;
				this.Volume = volume;
				this.Pitch = pitch;
				this.Looped = looped;
				this.Handle = null;
			}
		}
		
		
		// --- keys-related ---
		
		private bool[] Keys = new bool[16];
		
		
		// --- panel-related ---
		
		internal int[] Panel = null;
		
		internal List<Blink> Blinks = new List<Blink>();
		
		
		// --- sound-related ---
		
		internal List<Sound> Sounds = null;
		
		private PlaySoundDelegate PlaySound = null;

		
		// --- administration-related ---
		
		internal VehicleSpecs Specs = null;
		
		private double MaximumBrakeCylinderPressure = 1.0;
		
		private string ScriptsDirectory = null;
		
		private Script[] Scripts = null;
		
		private double ScriptsReloadCountdown = 0.0;
		
		private string DebugMessage = null;
		
		
		// --- functions ---
		
		private void LoadScripts() {
			for (int i = 0; i < this.Panel.Length; i++) {
				this.Panel[i] = 0;
			}
			this.Blinks = new List<Blink>();
			foreach (Sound sound in this.Sounds) {
				sound.Handle.Stop();
			}
			this.Sounds = new List<Sound>();
			#if !DEBUG
			try {
				#endif
				this.Scripts = Script.FromDirectory(this.ScriptsDirectory, this);
				this.DebugMessage = "OK";
				#if !DEBUG
			} catch (Exception ex) {
				this.Scripts = new Script[] { };
				for (int i = 0; i < this.Panel.Length; i++) {
					this.Panel[i] = 1;
				}
				this.DebugMessage = ex.Message;
			}
			#endif
		}
		
		
		// --- api functions ---
		
		/// <summary>Is called when the plugin is loaded.</summary>
		/// <param name="properties">The properties supplied to the plugin on loading.</param>
		/// <returns>Whether the plugin was loaded successfully.</returns>
		public bool Load(LoadProperties properties) {
			Panel = new int[2048];
			Sounds = new List<Sound>();
			PlaySound = properties.PlaySound;
			properties.Panel = this.Panel;
			properties.AISupport = AISupport.Basic;
			this.ScriptsDirectory = OpenBveApi.Path.CombineDirectory(properties.TrainFolder, "Scripts");
			try {
				string file = OpenBveApi.Path.CombineFile(properties.TrainFolder, "train.dat");
				string[] lines = System.IO.File.ReadAllLines(file);
				for (int i = 0; i < lines.Length; i++) {
					if (lines[i].Equals("#PRESSURE", StringComparison.OrdinalIgnoreCase)) {
						string text = lines[i + 1];
						int semicolon = text.IndexOf(';');
						if (semicolon >= 0) {
							text = text.Substring(0, semicolon).Trim();
						} else {
							text = text.Trim();
						}
						this.MaximumBrakeCylinderPressure = 1000.0 * double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
						break;
					}
				}
			} catch {
				this.MaximumBrakeCylinderPressure = 490000.0;
			}
			return true;
		}
		
		/// <summary>Is called when the plugin is unloaded.</summary>
		public void Unload() {
		}
		
		/// <summary>Is called after loading to inform the plugin about the specifications of the train.</summary>
		/// <param name="specs">The specifications of the train.</param>
		public void SetVehicleSpecs(VehicleSpecs specs) {
			this.Specs = specs;
			LoadScripts();
		}
		
		/// <summary>Is called when the plugin should initialize or reinitialize.</summary>
		/// <param name="mode">The mode of initialization.</param>
		public void Initialize(InitializationModes mode) {
		}
		
		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data passed to the plugin.</param>
		public void Elapse(ElapseData data) {
			if (data.ElapsedTime.Seconds < 0.0 | data.ElapsedTime.Seconds > 1.0) {
				return;
			}
			// --- reload scripts ---
			const double maximum = 3.0;
			if (this.Keys[(int)VirtualKeys.B1] & this.Keys[(int)VirtualKeys.B2]) {
				if (this.ScriptsReloadCountdown < maximum) {
					this.ScriptsReloadCountdown += data.ElapsedTime.Seconds;
					if (this.ScriptsReloadCountdown >= maximum) {
						LoadScripts();
					} else {
						this.DebugMessage = "Reloading in " + (maximum - ScriptsReloadCountdown).ToString("0.0") + " seconds...";
					}
				}
			} else {
				if (this.ScriptsReloadCountdown != 0.0) {
					if (this.ScriptsReloadCountdown < maximum) {
						this.DebugMessage = "OK";
					}
					this.ScriptsReloadCountdown = 0.0;
				}
			}
			// --- blink ---
			foreach (Blink blink in this.Blinks) {
				blink.Counter += data.ElapsedTime.Seconds;
				blink.Counter -= blink.Interval * Math.Floor(blink.Counter / blink.Interval);
				int index = (int)Math.Floor(blink.Counter / blink.Interval * (double)blink.Values.Length);
				this.Panel[blink.Index] = blink.Values[index];
			}
			// --- sound ---
			for (int i = this.Sounds.Count - 1; i >= 0; i--) {
				bool remove = false;
				if (this.Sounds[i].Handle == null) {
					this.Sounds[i].Handle = this.PlaySound.Invoke(this.Sounds[i].Index, this.Sounds[i].Volume, this.Sounds[i].Pitch, this.Sounds[i].Looped);
					if (this.Sounds[i].Handle == null) {
						remove = true;
					}
				} else if (this.Sounds[i].Handle.Stopped) {
					remove = true;
				}
				if (remove) {
					this.Sounds.RemoveAt(i);
					i--;
				}
			}
			// --- handles ---
			bool reverserOverriden = false;
			bool powerOverriden = false;
			bool brakesOverridden = false;
			double brakesPercentage = 0.0;
			foreach (Script script in this.Scripts) {
				foreach (HandlesAction handles in script.Handles) {
					if (handles.Reverser.HasValue) {
						if (reverserOverriden) {
							if (Math.Sign(handles.Reverser.Value) != Math.Sign(data.Handles.Reverser)) {
								data.Handles.Reverser = 0;
							}
						} else {
							data.Handles.Reverser = handles.Reverser.Value;
							reverserOverriden = true;
						}
					}
					if (handles.PowerNotch.HasValue) {
						if (powerOverriden) {
							if (handles.PowerNotch.Value < data.Handles.PowerNotch) {
								data.Handles.PowerNotch = handles.PowerNotch.Value;
							}
						} else {
							data.Handles.PowerNotch = handles.PowerNotch.Value;
							powerOverriden = true;
						}
					}
					if (this.Specs.BrakeType == BrakeTypes.AutomaticAirBrake) {
						if (handles.BrakePercentage.HasValue) {
							if (handles.BrakePercentage > brakesPercentage) {
								brakesPercentage = handles.BrakePercentage.Value;
							}
							brakesOverridden = true;
						}
					} else {
						if (handles.BrakeNotch.HasValue) {
							if (brakesOverridden) {
								if (handles.BrakeNotch.Value > data.Handles.BrakeNotch) {
									data.Handles.BrakeNotch = handles.BrakeNotch.Value;
								}
							} else {
								data.Handles.BrakeNotch = handles.BrakeNotch.Value;
								brakesOverridden = true;
							}
						}
					}
				}
			}
			if (this.Specs.BrakeType == BrakeTypes.AutomaticAirBrake && brakesOverridden) {
				if (brakesPercentage <= 0.0) {
					data.Handles.BrakeNotch = 0;
				} else if (brakesPercentage < 1.0) {
					double pressure = brakesPercentage * this.MaximumBrakeCylinderPressure;
					if (data.Vehicle.BcPressure > 1.15 * pressure) {
						data.Handles.BrakeNotch = 0;
					} else if (data.Vehicle.BcPressure > 0.85 * pressure) {
						data.Handles.BrakeNotch = 1;
					} else {
						data.Handles.BrakeNotch = 2;
					}
				} else if (brakesPercentage == 1.0) {
					if (data.Vehicle.BcPressure > 0.95 * this.MaximumBrakeCylinderPressure) {
						data.Handles.BrakeNotch = 1;
					} else {
						data.Handles.BrakeNotch = 2;
					}
				} else {
					data.Handles.BrakeNotch = 3;
				}
			}
			// --- countdowns ---
			foreach (Script script in this.Scripts) {
				for (int i = 0; i < script.Events.Count; i++) {
					CountdownEvent f = script.Events[i] as CountdownEvent;
					if (f != null) {
						f.Current += data.ElapsedTime.Seconds;
						if (f.Current >= f.Maximum) {
							script.Events.RemoveAt(i);
							f.Current = 0.0;
							f.Trigger();
							i--;
						}
					}
				}
			}
			// --- debug ---
			data.DebugMessage = this.DebugMessage;
		}
		
		/// <summary>Is called when the driver changes the reverser.</summary>
		/// <param name="reverser">The new reverser position.</param>
		public void SetReverser(int reverser) {
		}
		
		/// <summary>Is called when the driver changes the power notch.</summary>
		/// <param name="powerNotch">The new power notch.</param>
		public void SetPower(int powerNotch) {
		}
		
		/// <summary>Is called when the driver changes the brake notch.</summary>
		/// <param name="brakeNotch">The new brake notch.</param>
		public void SetBrake(int brakeNotch) {
		}
		
		/// <summary>Is called when a virtual key is pressed.</summary>
		/// <param name="key">The virtual key that was pressed.</param>
		public void KeyDown(VirtualKeys key) {
			int index = (int)key;
			if (index >= 0 & index < this.Keys.Length) {
				if (!this.Keys[index]) {
					foreach (Script script in this.Scripts) {
						for (int i = 0; i < script.Events.Count; i++) {
							KeyDownEvent f = script.Events[i] as KeyDownEvent;
							if (f != null && f.Key == key) {
								f.Trigger();
							}
						}
					}
					this.Keys[index] = true;
				}
				foreach (Script script in this.Scripts) {
					for (int i = 0; i < script.Events.Count; i++) {
						KeyPressEvent f = script.Events[i] as KeyPressEvent;
						if (f != null && f.Key == key) {
							f.Trigger();
						}
					}
				}
			}
		}
		
		/// <summary>Is called when a virtual key is released.</summary>
		/// <param name="key">The virtual key that was released.</param>
		public void KeyUp(VirtualKeys key) {
			int index = (int)key;
			if (index >= 0 & index < this.Keys.Length) {
				if (this.Keys[index]) {
					foreach (Script script in this.Scripts) {
						for (int i = 0; i < script.Events.Count; i++) {
							KeyUpEvent f = script.Events[i] as KeyUpEvent;
							if (f != null && f.Key == key) {
								f.Trigger();
							}
						}
					}
					this.Keys[index] = false;
				}
			}
		}
		
		/// <summary>Is called when a horn is played or when the music horn is stopped.</summary>
		/// <param name="type">The type of horn.</param>
		public void HornBlow(HornTypes type) {
		}
		
		/// <summary>Is called when the state of the doors changes.</summary>
		/// <param name="oldState">The old state of the doors.</param>
		/// <param name="newState">The new state of the doors.</param>
		public void DoorChange(DoorStates oldState, DoorStates newState) {
		}
		
		/// <summary>Is called when the aspect in the current or in any of the upcoming sections changes, or when passing section boundaries.</summary>
		/// <param name="data">Signal information per section. In the array, index 0 is the current section, index 1 the upcoming section, and so on.</param>
		/// <remarks>The signal array is guaranteed to have at least one element. When accessing elements other than index 0, you must check the bounds of the array first.</remarks>
		public void SetSignal(SignalData[] signal) {
		}
		
		/// <summary>Is called when the train passes a beacon.</summary>
		/// <param name="beacon">The beacon data.</param>
		public void SetBeacon(BeaconData beacon) {
			foreach (Script script in this.Scripts) {
				for (int i = 0; i < script.Events.Count; i++) {
					BeaconEvent f = script.Events[i] as BeaconEvent;
					if (f != null && f.Type.IsMember(beacon.Type) && f.Signal.IsMember(beacon.Signal.Aspect)) {
						f.Trigger();
					}
				}
			}
		}
		
		/// <summary>Is called when the plugin should perform the AI.</summary>
		/// <param name="data">The AI data.</param>
		public void PerformAI(AIData data) {
		}
		
	}
}