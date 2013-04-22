using System;
using System.Linq;
using System.Collections.Generic;
using OpenIDE.Core.CodeEngineIntegration;
using OpenIDE.Core.Language;

namespace OpenIDE.Arguments.Handlers
{
	class CodeModelQueryHandler : ICommandHandler
	{
		private List<CodeEngineQueryHandler> _handlers = new List<CodeEngineQueryHandler>();
		private ICodeEngineLocator _codeEngineFactory;


		public CommandHandlerParameter Usage {
			get {
				var usage = new CommandHandlerParameter(
					"All",
					CommandType.FileCommand,
					Command,
					"Handles queries against the code model cache");
				foreach (var handler in _handlers)
					usage.Add(handler.Usage);
				return usage;
			}
		}

		public CodeModelQueryHandler(ICodeEngineLocator locator)
		{
			_codeEngineFactory = locator;
			_handlers.Add(new CodeEngineGetProjectsHandler(_codeEngineFactory));
			_handlers.Add(new CodeEngineGetFilesHandler(_codeEngineFactory));
			_handlers.Add(new CodeEngineGetCodeRefsHandler(_codeEngineFactory));
			_handlers.Add(new CodeEngineGetSignatureRefsHandler(_codeEngineFactory));
			_handlers.Add(new CodeEngineFindSignatureHandler(_codeEngineFactory));
		}

		public string Command { get { return "codemodel"; } }
		
		public void Execute (string[] arguments)
		{
			if (arguments.Length == 0)
				return;
			var handler = _handlers.FirstOrDefault(x => x.Command == arguments[0]);
			if (handler == null)
				return;
			handler.Execute(getArguments(arguments));
		}

		private string[] getArguments(string[] args)
		{
			var arguments = new List<string>();
			for (int i = 1; i < args.Length; i++)
				arguments.Add(args[i]);
			return arguments.ToArray();
		}
	}

	class CodeEngineGetProjectsHandler : CodeEngineQueryHandler 
	{
		protected override string _commandDescription {
			get { return  "Queries for projects"; }
		}
		protected override string _queryDescription {
			get { return  "Format: file=MyProj*. Supported properties: file"; }
		}
		protected override string _command { get { return  "get-projects"; } }

		protected override void run(Instance instance, string args) {
			Console.WriteLine(instance.GetProjects(args));
		}
		
		public CodeEngineGetProjectsHandler(ICodeEngineLocator codeEngineFactory)
		{
			_codeEngineFactory = codeEngineFactory;
		}
	}

	class CodeEngineGetFilesHandler : CodeEngineQueryHandler 
	{
		protected override string _commandDescription {
			get { return  "Queries for files"; }
		}
		protected override string _queryDescription {
			get { return  "Format: file=*.cs. Supported properties: project,file"; }
		}
		protected override string _command { get { return  "get-files"; } }

		protected override void run(Instance instance, string args) {
			Console.WriteLine(instance.GetFiles(args));
		}
		
		public CodeEngineGetFilesHandler(ICodeEngineLocator codeEngineFactory)
		{
			_codeEngineFactory = codeEngineFactory;
		}
	}

	class CodeEngineGetCodeRefsHandler : CodeEngineQueryHandler 
	{
		protected override string _commandDescription {
			get { return  "Queries for signatures"; }
		}
		protected override string _queryDescription {
			get { return  "Format: type=class,name=MyCls*. Supported properties: language, type, file, parent, name, signature, custom"; }
		}
		protected override string _command { get { return  "get-signatures"; } }

		protected override void run(Instance instance, string args) {
			Console.WriteLine(instance.GetCodeRefs(args));
		}
		
		public CodeEngineGetCodeRefsHandler(ICodeEngineLocator codeEngineFactory)
		{
			_codeEngineFactory = codeEngineFactory;
		}
	}
	
	class CodeEngineGetSignatureRefsHandler : CodeEngineQueryHandler
	{
		protected override string _commandDescription {
			get { return  "Queries for signature references"; }
		}
		protected override string _queryDescription {
			get { return  "Format: file=*.cs,signature=My.Signature. Supported properties: file,signature"; }
		}
		protected override string _command { get { return  "get-signature-refs"; } }

		protected override void run(Instance instance, string args) {
			Console.WriteLine(instance.GetSignatureRefs(args));
		}
		
		public CodeEngineGetSignatureRefsHandler(ICodeEngineLocator codeEngineFactory)
		{
			_codeEngineFactory = codeEngineFactory;
		}
	}
	
	class CodeEngineFindSignatureHandler : CodeEngineQueryHandler
	{
		protected override string _commandDescription {
			get { return  "Type search"; }
		}
		protected override string _queryDescription {
			get { return  "Search string"; }
		}
		protected override string _command { get { return  "find-types"; } }

		protected override void run(Instance instance, string args) {
			Console.WriteLine(instance.FindTypes(args));
		}
		
		public CodeEngineFindSignatureHandler(ICodeEngineLocator codeEngineFactory)
		{
			_codeEngineFactory = codeEngineFactory;
		}
	}
	
	abstract class CodeEngineQueryHandler : ICommandHandler
	{
		protected abstract string _commandDescription { get; }
		protected abstract string _queryDescription { get; }
		protected abstract string _command { get; }
		protected abstract void run(Instance instance, string args);

		protected ICodeEngineLocator _codeEngineFactory;

		public CommandHandlerParameter Usage {
			get {
				var usage = new CommandHandlerParameter(
					"All",
					CommandType.FileCommand,
					Command,
					_commandDescription);
				usage.Add("[QUERY]", _queryDescription);
				return usage;
			}
		}

		public string Command { get { return _command; } }
		
		public void Execute (string[] arguments)
		{
			var instance = _codeEngineFactory.GetInstance(Environment.CurrentDirectory);
			if (instance == null)
				return;
			var args = "";
			if (arguments.Length == 1)
				args = arguments[0];
			run(instance, args);
		}
	}
}
