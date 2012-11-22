using System;
using System.IO;
using System.Reflection;
using System.Linq;
using CSharp.FileSystem;
using System.Xml;
using System.Text;
using System.Diagnostics;
using CSharp.Projects;
using CSharp.Files;
using CSharp.Responses;
using CoreExtensions;

namespace CSharp.Commands
{
	public class NewHandler : ICommandHandler
	{
		private string _keyPath;
		private IProjectHandler _project;
		private IResolveFileTypes _fileTypeResolver;
		private Func<string, ProviderSettings> _getTypesProviderByLocation;
		// Check explanation by OverrideTemplatePicker
		private Func<string, string, INewTemplate> _pickTemplate;
		
		public string Usage {
			get {
				try {
					var usage = 
						Command+"|\"Uses the new template to create what ever specified by the template\"";
					getTemplates("CSharp").ToList()
						.ForEach(x => 
							{
								var command = getUsage(x);
								if (command != null)
									usage += " " + listUsages(command);
							});
					return usage + " end ";
				} catch {
					return null;
				}
			}
		}

		private string listUsages(BaseCommandHandlerParameter command)
		{
			var usage = command.Name + "|\"" + command.Description + "\"";
			command.Parameters.ToList()
				.ForEach(x => usage += " " + listUsages(x));
			return usage + " end ";
		}

		private BaseCommandHandlerParameter getUsage(string template)
		{
			var name = Path.GetFileNameWithoutExtension(template);
			var definition = new NewTemplate(template, null, _keyPath).GetUsageDefinition();
			var parser = new TemplateDefinitionParser();
			var usage = parser.Parse(name, definition);
			if (usage == null)
				return null;
			var fileParam = new BaseCommandHandlerParameter("FILE", "Path to the file to be create");
			usage.Parameters.ToList()
				.ForEach(x => fileParam.Add(x));
			usage = new BaseCommandHandlerParameter(usage.Name, usage.Description);
			usage.Add(fileParam);
			return usage;
		}
		
		public string Command { get { return "new"; } }
		
		public NewHandler(IResolveFileTypes fileTypeResolver, Func<string, ProviderSettings> provider, string keyPath)
		{
			_keyPath = keyPath;
			_getTypesProviderByLocation = provider;
			_pickTemplate = pickTemplate;
			_fileTypeResolver = fileTypeResolver;
			_project = new ProjectHandler();
		}
		
		// Yeah.. abstraction for not having to dick arround with templates and other file access
		// stuff in tests.
		// Shut it or make something better! ;)
		public void OverrideProjectHandler(IProjectHandler handler)
		{
			_project = handler;
		}
		public void OverrideTemplatePicker(Func<string, string, INewTemplate> picker)
		{
			_pickTemplate = picker;
		}
		
		public void Execute(IResponseWriter writer, string[] arguments)
		{
			if (arguments.Length < 2)
			{
				writer.Write("error|Invalid number of arguments. " +
					"Usage: new {template name} {item name} {template arguments}");
				return;
			}
			
			var className = getFileName(arguments[1]);
			var location = getLocation(arguments[1]);
			if (!_project.Read(location, _getTypesProviderByLocation))
				return;
			
			var template = _pickTemplate(arguments[0], _project.Type);
			if (template == null)
			{
				writer.Write("error|No template with the name {0} exists.", arguments[0]);
				return;
			}
			var ns = getNamespace(location, _project.Fullpath, _project.DefaultNamespace);
			template.Run(location, className, ns, _project.Fullpath, _project.Type, getArguments(arguments));
			if (template.File == null)
				return;
			
			_project.AppendFile(template.File);
			_project.Write();
			
			writer.Write("comment|Created class {0}.{1}", ns, className);
			writer.Write("comment|Full path {0}", template.File.Fullpath);
			
			gotoFile(writer, template.File.Fullpath, template.Line, template.Column, location);
		}
		
		private INewTemplate pickTemplate(string templateName, string type)
		{
			var template = getTemplates(type)
				.FirstOrDefault(x => x.Contains(Path.DirectorySeparatorChar + templateName + "."));
			if (template == null)
				return null;
			return new NewTemplate(template, _fileTypeResolver, _keyPath);
		}
		
		private string[] getTemplates(string type)
		{
			var templateDir = 
				Path.Combine(
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
					"new");
			return Directory.GetFiles(templateDir)
				.Where(x => !x.EndsWith(".swp") && !x.EndsWith("~")).ToArray();
		}

		private string[] getArguments(string[] args)
		{
			if (args.Length == 1)
				return new string[] {};
			string[] newArgs = new string[args.Length - 1];
			for (int i = 1; i < args.Length; i++)
				newArgs[i - 1] = args[i];
			return newArgs;
		}
		
		private string getFileName(string classname)
		{
			return Path.GetFileNameWithoutExtension(classname);
		}
		
		private string getLocation(string className)
		{
			var dir = Path.GetDirectoryName(className).Trim();
			if (dir.Length == 0)
				return _keyPath;
			if (Directory.Exists(Path.Combine(_keyPath, dir)))
				return Path.Combine(_keyPath, dir);
			if (Directory.Exists(dir))
				return dir;
			
			if (!Path.IsPathRooted(dir))
				dir = Path.Combine(_keyPath, dir);
			Directory.CreateDirectory(dir);

			return dir;
		}
		
		private string getNamespace(string location, string project, string defaultNamespace)
		{
			var projectLocation = Path.GetDirectoryName(project);
			var relativePath = PathExtensions.GetRelativePath(projectLocation, location);
			if (relativePath.Length == 0 || relativePath.Equals(location))
				return defaultNamespace;
			return string.Format("{0}.{1}",
				defaultNamespace,
				relativePath.Replace(Path.DirectorySeparatorChar.ToString(), "."));
		}
		
		private void gotoFile(IResponseWriter writer, string file, int line, int column, string location)
		{
			writer.Write(
				string.Format("editor goto \"{0}|{1}|{2}\"",
					file, line, column));
			writer.Write("editor setfocus");
		}
	}
	
	public interface INewTemplate
	{
		IFile File { get; }
		int Line { get; }
		int Column { get; }
		
		void Run(
			string location,
			string itemName,
			string nameSpace,
			string projectPath,
			string language,
			string[] arguments);
	}

	class NewTemplate : INewTemplate
	{
		private string _keyPath;
		private IResolveFileTypes _fileTypeResolver;
		private string _file;
		
		public IFile File { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }
		
		public NewTemplate(string file, IResolveFileTypes fileTypeResolver, string keyPath)
		{
			_fileTypeResolver = fileTypeResolver;
			_file = file;
			Line = 0;
			Column = 0;
			_keyPath = keyPath;
		}

		public string GetUsageDefinition()
		{
			return run("get_definition");
		}
		
		public void Run(
			string location,
			string itemName,
			string nameSpace,
			string projectPath,
			string projectType,
			string[] arguments)
		{
			try
			{
				var filename = 
					Path.Combine(location, string.Format("{0}{1}", itemName, run("get_file_extension")));
				if (System.IO.File.Exists(filename))
				{
					Console.WriteLine("error|File already exists {0}", filename);
					File = null;
					return;
				}
				File = _fileTypeResolver.Resolve(filename);
				if (File == null)
					return;
				
				var xml = getXml(location, projectPath, projectType, arguments);
				var tempFile = Path.GetTempFileName();
				System.IO.File.WriteAllText(tempFile, xml);
				var content = run(string.Format("\"{0}\" \"{1}\" \"{2}\"", itemName, nameSpace, tempFile));
				System.IO.File.Delete(tempFile);
				System.IO.File.WriteAllText(filename, content);
				getPositionInfo();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				File = null;
			}
		}
		
		private string getXml(
			string filename,
			string projectPath,
			string projectType,
			string[] arguments)
		{
			var sb = new StringBuilder();
			using (var writer = XmlWriter.Create(sb))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("parameters");
					writer.WriteElementString("fullpath", filename);
					writer.WriteStartElement("project");
						writer.WriteElementString("fullpath", projectPath);
						writer.WriteElementString("type", projectType.ToString().ToLower());
					writer.WriteEndElement();
					writer.WriteStartElement("custom_parameters");
						arguments.ToList().ForEach(x => writer.WriteElementString("parameter", x));
					writer.WriteEndElement();
				writer.WriteEndElement();
			}
			return sb.ToString();
		}
		
		private void getPositionInfo()
		{
			try
			{
				var positionString = run("get_position")
					.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
				Line = int.Parse(positionString[0]);
				Column = int.Parse(positionString[1]);
			}
			catch
			{
				Line = 0;
				Column = 0;
			}
		}
		
		private string run(string arguments)
		{
            var proc = new Process();
            var sb = new StringBuilder();
			proc.Query(
	        	_file,
	        	arguments,
	        	false,
	        	Environment.CurrentDirectory,
	        	(error, s) => {
	        			if (error) {
	        				sb.AppendLine("error|" + s);
	        				return;
	        			}
	        			sb.AppendLine(s);
	        		});
			var output = sb.ToString();
			if (output.Length > Environment.NewLine.Length)
				return output.Substring(0, output.Length - Environment.NewLine.Length);
            return output;
		}
	}
}

