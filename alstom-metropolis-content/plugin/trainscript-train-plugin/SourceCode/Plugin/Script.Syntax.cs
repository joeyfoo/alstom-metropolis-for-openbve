using System;
using System.IO;

namespace Plugin {
	internal partial class Script {
		
		/// <summary>Parses a script file syntactially.</summary>
		/// <param name="lines">The individual lines of the script file.</param>
		/// <param name="title">The title of the script file.</param>
		/// <returns>The no-name top-level element.</returns>
		private static Element ParseSyntax(string[] lines, string title) {
			Element element = new Element(null, null);
			for (int i = 0; i < lines.Length; i++) {
				string line = lines[i];
				int semicolon = line.IndexOf(';');
				if (semicolon >= 0) {
					line = line.Substring(0, semicolon).Trim();
				} else {
					line = line.Trim();
				}
				if (line.Length != 0) {
					if (line[0] == '[' && line[line.Length - 1] == ']') {
						if (line[1] == '/') {
							string name = line.Substring(2, line.Length - 3).Trim().ToLowerInvariant();
							if (element.Parent == null) {
								throw new InvalidDataException("Closing tag [/" + name + "] does not have a matching opening tag at line " + (i + 1).ToString() + " in file " + title);
							} else if (element.Name != name) {
								throw new InvalidDataException("Closing tag [/" + name + "] does not match opening tag [" + element.Name + "] on line " + (i + 1).ToString() + " in file " + title);
							}
							element = element.Parent;
						} else if (line[line.Length - 2] == '/') {
							string name = line.Substring(1, line.Length - 3).Trim().ToLowerInvariant();
							element.Elements.Add(new Element(element, name));
						} else {
							string name = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
							Element newElement = new Element(element, name);
							element.Elements.Add(newElement);
							element = newElement;
						}
					} else {
						int equals = line.IndexOf('=');
						if (equals >= 0) {
							string key = line.Substring(0, equals).TrimEnd().ToLowerInvariant();
							string value = line.Substring(equals + 1).TrimStart();
							element.Parameters[key] = value;
						} else {
							throw new InvalidDataException("Syntax error on line " + (i + 1).ToString() + " in file " + title);
						}
					}
				}
			}
			if (element.Parent != null) {
				throw new InvalidDataException("Missing closing tag [/" + element.Parent.Name + "] in file " + title);
			}
			return element;
		}
		
	}
}