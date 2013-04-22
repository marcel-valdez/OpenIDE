using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using OpenIDE.Core.FileSystem;
using OpenIDE.Core.Language;

namespace OpenIDE.Arguments.Handlers
{
	class DeleteScriptHandler : ICommandHandler
	{
		private string _token;

		public CommandHandlerParameter Usage {
			get {
					var usage = new CommandHandlerParameter(
						"All",
						CommandType.FileCommand,
						Command,
						"Delete a script");
					usage.Add("SCRIPT-NAME", "Script name local are picked over global");
					return usage;
			}
		}
	
		public string Command { get { return "rm"; } }

		public DeleteScriptHandler(string token) {
			_token = token;
		}

		public void Execute(string[] arguments)
		{
			var scripts = new List<Script>();
			scripts.AddRange(new ScriptLocator(_token, Environment.CurrentDirectory).GetLocalScripts());
			new ScriptLocator(_token, Environment.CurrentDirectory)
				.GetGlobalScripts()
				.Where(x => scripts.Count(y => x.Name.Equals(y.Name)) == 0).ToList()
				.ForEach(x => scripts.Add(x));
			var script = scripts.FirstOrDefault(x => x.Name.Equals(arguments[0]));
			if (script == null || arguments.Length < 1)
				return;
			if (File.Exists(script.File))
				File.Delete(script.File);
		}
	}
}
