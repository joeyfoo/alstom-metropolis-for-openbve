using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Plugin {
	internal partial class Script {
		
		// --- classes ---
		
		/// <summary>Represents a tag in the textual script file.</summary>
		private class Element {
			internal Element Parent;
			internal string Name;
			internal Dictionary<string, string> Parameters;
			internal List<Element> Elements;
			internal Element(Element parent, string name) {
				this.Parent = parent;
				this.Name = name;
				this.Parameters = new Dictionary<string, string>();
				this.Elements = new List<Element>();
			}
		}
		
		
		// --- functions ---
		
		/// <summary>Loads all scripts from the specified directory.</summary>
		/// <param name="directory">The directory.</param>
		/// <param name="plugin">The plugin.</param>
		/// <returns>The scripts.</returns>
		internal static Script[] FromDirectory(string directory, Plugin plugin) {
			List<Script> scripts = new List<Script>();
			string[] files = Directory.GetFiles(directory);
			foreach (string file in files) {
				if (file.EndsWith(".script", StringComparison.OrdinalIgnoreCase)) {
					scripts.Add(FromFile(file, plugin));
				}
			}
			return scripts.ToArray();
		}
		
		/// <summary>Loads a script from the specified file.</summary>
		/// <param name="file">The file.</param>
		/// <param name="plugin">The plugin.</param>
		/// <returns>The script.</returns>
		internal static Script FromFile(string file, Plugin plugin) {
			string title = Path.GetFileName(file);
			string[] lines = File.ReadAllLines(file, Encoding.UTF8);
			Element element = ParseSyntax(lines, title);
			Script script = new Script(plugin);
			Event e = (Event)ParseSemantics(script, element, title);
			e.Trigger();
			return script;
		}
		


		
	}
}