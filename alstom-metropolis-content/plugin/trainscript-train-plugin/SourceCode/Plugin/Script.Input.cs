using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Plugin {
	internal partial class Script {
		
		// --- integers ---
		
		private static int GetAbstractInteger(string expression) {
			return int.Parse(expression, CultureInfo.InvariantCulture);
		}
		
		private static int[] GetAbstractIntegers(string expression) {
			string[] parts = Split(expression);
			int[] values = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++) {
				values[i] = GetAbstractInteger(parts[i]);
			}
			return values;
		}
		
		private static int GetReverser(string expression, Script script) {
			switch (expression.ToLowerInvariant()) {
				case "b":
				case "backward":
					return -1;
				case "n":
				case "null":
				case "neutral":
					return 0;
				case "f":
				case "forward":
					return 1;
				default:
					throw new InvalidDataException("Invalid reverser '" + expression + "' encountered.");
			}
		}

		private static int GetPower(string expression, Script script) {
			switch (expression.ToLowerInvariant()) {
				case "n":
				case "null":
				case "neutral":
					return 0;
				default:
					int value;
					if (expression.EndsWith("%")) {
						double ratio = 0.01 * double.Parse(expression.Substring(0, expression.Length - 1).TrimEnd(), CultureInfo.InvariantCulture);
						value = (int)Math.Round(ratio * (double)script.Plugin.Specs.PowerNotches);
					} else if (expression.StartsWith("P", StringComparison.OrdinalIgnoreCase)) {
						value = int.Parse(expression.Substring(1), CultureInfo.InvariantCulture);
					} else {
						throw new InvalidDataException("Invalid power '" + expression + "' encountered.");
					}
					if (value < 0) {
						value = 0;
					} else if (value > script.Plugin.Specs.PowerNotches) {
						value = script.Plugin.Specs.PowerNotches;
					}
					return value;
			}
		}

		private static int GetBrakes(string expression, Script script, out double brakePercentage) {
			switch (expression.ToLowerInvariant()) {
				case "n":
				case "null":
				case "neutral":
					brakePercentage = 0.0;
					return 0;
				case "hld":
				case "hold":
				case "hold brake":
				case "hold brakes":
					brakePercentage = 0.0;
					return script.Plugin.Specs.HasHoldBrake ? 1 : 0;
				case "emg":
				case "emergency":
				case "emergency brake":
				case "emergency brakes":
					brakePercentage = 2.0;
					return script.Plugin.Specs.BrakeNotches + 1;
				default:
					if (script.Plugin.Specs.BrakeType == OpenBveApi.Runtime.BrakeTypes.AutomaticAirBrake) {
						if (expression.EndsWith("%")) {
							double ratio = 0.01 * double.Parse(expression.Substring(0, expression.Length - 1).TrimEnd(), CultureInfo.InvariantCulture);
							brakePercentage = ratio <= 0.0 ? 0.0 : ratio < 1.0 ? ratio : 1.0;
							return 0;
						} else if (expression.StartsWith("B", StringComparison.OrdinalIgnoreCase)) {
							brakePercentage = 1.0;
							return 0;
						} else {
							throw new InvalidDataException("Invalid brakes '" + expression + "' encountered.");
						}
					} else {
						int value;
						if (expression.EndsWith("%")) {
							double ratio = 0.01 * double.Parse(expression.Substring(0, expression.Length - 1).TrimEnd(), CultureInfo.InvariantCulture);
							int notches = script.Plugin.Specs.HasHoldBrake ? script.Plugin.Specs.BrakeNotches - 1 : script.Plugin.Specs.BrakeNotches;
							value = (int)Math.Round(ratio * (double)notches);
						} else if (expression.StartsWith("B", StringComparison.OrdinalIgnoreCase)) {
							value = int.Parse(expression.Substring(1), CultureInfo.InvariantCulture);
						} else {
							throw new InvalidDataException("Invalid brakes '" + expression + "' encountered.");
						}
						if (value < 0) {
							value = 0;
						} else if (script.Plugin.Specs.HasHoldBrake) {
							value++;
						}
						if (value > script.Plugin.Specs.BrakeNotches) {
							value = script.Plugin.Specs.BrakeNotches;
						}
						brakePercentage = 0.0;
						return value;
					}
			}
		}
		
		
		// --- integer ranges ---
		
		private static Range GetRange(string expression) {
			int to = expression.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
			if (to >= 0) {
				int start = int.Parse(expression.Substring(0, to).TrimEnd(), CultureInfo.InvariantCulture);
				int end = int.Parse(expression.Substring(to + 4).TrimStart(), CultureInfo.InvariantCulture);
				return new Range(start, end);
			} else {
				to = expression.IndexOf('-');
				if (to >= 0) {
					int start = int.Parse(expression.Substring(0, to).TrimEnd(), CultureInfo.InvariantCulture);
					int end = int.Parse(expression.Substring(to + 1).TrimStart(), CultureInfo.InvariantCulture);
					return new Range(start, end);
				} else {
					int value = int.Parse(expression, CultureInfo.InvariantCulture);
					return new Range(value, value);
				}
			}
		}
		
		private static RangeCollection GetRangeCollection(string expression) {
			string[] parts = Split(expression);
			Range[] ranges = new Range[parts.Length];
			for (int i = 0; i < parts.Length; i++) {
				ranges[i] = GetRange(parts[i]);
			}
			return new RangeCollection(ranges);
		}
		
		
		// --- doubles ---
		
		private static double GetAbstractDouble(string expression) {
			return double.Parse(expression, CultureInfo.InvariantCulture);
		}
		
		private static double GetRatio(string expression) {
			if (expression[expression.Length - 1] == '%') {
				return 0.01 * double.Parse(expression.Substring(0, expression.Length - 1).TrimEnd(), CultureInfo.InvariantCulture);
			} else {
				return double.Parse(expression, CultureInfo.InvariantCulture);
			}
		}
		
		private static double GetTime(string expression) {
			if (expression.EndsWith("milliseconds", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 12).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("millisecond", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 11).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("ms", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 2).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("seconds", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 7).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("second", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 6).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("s", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 1).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("minutes", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 7).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("minute", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 6).TrimEnd(), CultureInfo.InvariantCulture);
			} else if (expression.EndsWith("min", StringComparison.OrdinalIgnoreCase)) {
				return double.Parse(expression.Substring(0, expression.Length - 3).TrimEnd(), CultureInfo.InvariantCulture);
			} else {
				throw new InvalidDataException("Invalid time '" + expression + "' encountered.");
			}
		}
		
		
		// --- booleans ---
		private static bool GetBoolean(string expression) {
			switch (expression.ToLowerInvariant()) {
				case "false":
				case "no":
				case "off":
					return false;
				case "true":
				case "yes":
				case "on":
					return true;
				default:
					throw new InvalidDataException("Invalid boolean '" + expression + "' encountered.");
			}
		}
		
		
		// --- strings ---
		
		private static string[] Split(string expression) {
			string[] parts = expression.Split(',');
			for (int i = 0; i < parts.Length; i++) {
				parts[i] = parts[i].Trim();
			}
			return parts;
		}
		
		
		// --- keys ---
		
		private static OpenBveApi.Runtime.VirtualKeys GetKey(string expression) {
			switch (expression.ToUpperInvariant()) {
					case "S": return OpenBveApi.Runtime.VirtualKeys.S;
					case "A1": return OpenBveApi.Runtime.VirtualKeys.A1;
					case "A2": return OpenBveApi.Runtime.VirtualKeys.A2;
					case "B1": return OpenBveApi.Runtime.VirtualKeys.B1;
					case "B2": return OpenBveApi.Runtime.VirtualKeys.B2;
					case "C1": return OpenBveApi.Runtime.VirtualKeys.C1;
					case "C2": return OpenBveApi.Runtime.VirtualKeys.C2;
					case "D2": return OpenBveApi.Runtime.VirtualKeys.D;
					case "D3": return OpenBveApi.Runtime.VirtualKeys.E;
					case "D4": return OpenBveApi.Runtime.VirtualKeys.F;
					case "D5": return OpenBveApi.Runtime.VirtualKeys.G;
					case "D6": return OpenBveApi.Runtime.VirtualKeys.H;
					case "D7": return OpenBveApi.Runtime.VirtualKeys.I;
					case "D8": return OpenBveApi.Runtime.VirtualKeys.J;
					case "D9": return OpenBveApi.Runtime.VirtualKeys.K;
					case "D0": return OpenBveApi.Runtime.VirtualKeys.L;
				default:
					throw new InvalidDataException("Invalid key '" + expression + "' encountered.");
			}
		}
		
	}
}