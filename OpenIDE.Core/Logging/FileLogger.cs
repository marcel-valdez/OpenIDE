using System;
using System.IO;
using System.Reflection;

namespace OpenIDE.Core.Logging
{
	public class FileLogger : ILogger
	{
		private string _file;

		public FileLogger()
		{
			_file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OpenIDE.CodeEngine.log");
		}
		public void Write(string message)
		{
			write(message);
		}

		public void Write(string message, params object[] args)
		{
			write(string.Format(message, args));
		}

		public void Write(Exception ex)
		{
			write(getException(ex));	
		}

		private string getException(Exception ex)
		{
			var message = ex.ToString();
			if (ex.InnerException != null)
				message += Environment.NewLine + getException(ex.InnerException);
			return message;
		}

		private void write(string message)
		{
			using (var writer = new StreamWriter(_file, true))
			{
				writer.WriteLine(message);	
			}
		}
	}
}
