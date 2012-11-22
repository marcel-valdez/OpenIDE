using System.IO;
using System.Text;
using System.Diagnostics;
using OpenIDE.Core.Language;
using OpenIDE.CodeEngine.Core.Endpoints.Tcp;
using OpenIDE.CodeEngine.Core.ReactiveScripts;
namespace OpenIDE.CodeEngine.Core.Endpoints
{
	public class EventEndpoint
	{
		private string _keyPath;
		private TcpServer _server;
		private string _instanceFile;
		private ReactiveScriptEngine _reactiveEngine;
		
		public EventEndpoint(string keyPath, PluginLocator locator)
		{
			_keyPath = keyPath;
			_server = new TcpServer();
			_server.IncomingMessage += Handle_serverIncomingMessage;
			_server.Start();
			_reactiveEngine = new ReactiveScriptEngine(_keyPath, locator);
		}
 
		void Handle_serverIncomingMessage (object sender, MessageArgs e)
		{
			handle(e);
		}

		void handle(MessageArgs command)
		{
            _reactiveEngine.Handle(command.Message);
		}
		
		public void Send(string message)
		{
			_server.Send(message);
			_reactiveEngine.Handle(message);
		}
		
		public void Start()
		{
			_server.Start();
			writeInstanceInfo(_keyPath);
		}
		
		public void Stop()
		{
			if (File.Exists(_instanceFile))
				File.Delete(_instanceFile);
		}
		
		private void writeInstanceInfo(string key)
		{
			var path = Path.Combine(Path.GetTempPath(), "OpenIDE.Events");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			_instanceFile = Path.Combine(path, string.Format("{0}.pid", Process.GetCurrentProcess().Id));
			var sb = new StringBuilder();
			sb.AppendLine(key);
			sb.AppendLine(_server.Port.ToString());
			File.WriteAllText(_instanceFile, sb.ToString());
		}
	}
}

