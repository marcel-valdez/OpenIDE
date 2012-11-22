using System;
using System.Linq;
using OpenIDE.Messaging;
using OpenIDE.Core.FileSystem;
using OpenIDE.Bootstrapping;
using OpenIDE.Arguments;
using OpenIDE.Core.Language;
using OpenIDE.CommandBuilding;
using OpenIDE.Core.CommandBuilding;
namespace oi
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Bootstrapper.Initialize();
			args = Bootstrapper.Settings.Parse(args);
			if (args.Length == 0)
			{
				printUsage();
				return;
			}
			var execute = Bootstrapper.GetDispatcher();
			var parser = new CommandStringParser();
			execute.For(
				parser.GetCommand(args),
				parser.GetArguments(args));
		}

		private static void printUsage()
		{
			Console.WriteLine("OpenIDE.Net v0.1");
			Console.WriteLine("OpenIDE.Net is a cross language system that provides simple IDE features around your favorite text exitor.");
			Console.WriteLine("(http://www.openide.net, http://github.com/ContinuousTests/OpenIDE)");
			Console.WriteLine();
			var level = 1;
			var handlers = Bootstrapper.GetCommandHandlers();
			handlers.ToList()
				.ForEach(x =>
					{
						var command = x.Usage;
						if (command == null)
							return;

						if (command.Name != Bootstrapper.Settings.DefaultLanguage)
							printCommand(command);
						else
						{
							Console.WriteLine("");
							Console.WriteLine("\tDirect commands because of default language {0}", command.Name);
							level--;
						}

						command.Parameters.ToList()
							.ForEach(y =>  printParameter(y, ref level));

						if ((command.Name == Bootstrapper.Settings.DefaultLanguage))
							level++;
					});
		}
		
		private static void printCommand(CommandHandlerParameter command)
		{
			Console.WriteLine("");
			Console.WriteLine("\t{2} : {0} ({1})",
				command.GetDescription(Environment.NewLine + "\t"),
				command.Language,
				command.Name);
		}

		private static void printParameter(BaseCommandHandlerParameter parameter, ref int level)
		{
			level++;
			var name = parameter.Name;
			if (!parameter.Required)
				name = "[" + name + "]";
			Console.WriteLine("{0}{1} : {2}", "".PadLeft(level, '\t'), name, parameter.Description);
			foreach (var child in parameter.Parameters)
				printParameter(child, ref level);
			level--;
		}
	}
}

