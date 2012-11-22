using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using OpenIDE.CodeEngine.Core.Caching;
using OpenIDE.CodeEngine.Core.Handlers;
using OpenIDE.CodeEngine.Core.Endpoints;
using OpenIDE.CodeEngine.Core.Endpoints.Tcp;
using OpenIDE.CodeEngine.Core.ChangeTrackers;
using OpenIDE.Core.Commands;
using OpenIDE.Core.Logging;
using OpenIDE.CodeEngine.Core.EditorEngine;
using OpenIDE.Core.Caching;
using OpenIDE.Core.Language;
using OpenIDE.Core.Windowing;


namespace OpenIDE.CodeEngine.Core.Bootstrapping
{
	public class Bootstrapper
	{
		private static string _path;
		private static PluginFileTracker _tracker;
		private static List<IHandler> _handlers = new List<IHandler>();
		private static CommandEndpoint _endpoint;
		private static EventEndpoint _eventEndpoint;
		private static TypeCache _cache;
        private static PluginLocator _pluginLocator;
        private static CrawlHandler _crawlHandler;

		public static CommandEndpoint GetEndpoint(string path, string[] enabledLanguages)
		{
			_path = path;
            _cache = new TypeCache();
			_crawlHandler = new CrawlHandler(_cache, (s) => Logger.Write(s));
			_pluginLocator = new PluginLocator(
				enabledLanguages,
				Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)),
				(msg) => {});
			initPlugins(_pluginLocator, _crawlHandler);

			_eventEndpoint = new EventEndpoint(_path, _pluginLocator);
			_eventEndpoint.Start();

			_tracker = new PluginFileTracker();
			_tracker.Start(
				_path,
				_cache,
				_cache,
				_pluginLocator,
				_eventEndpoint);

            _endpoint = new CommandEndpoint(_path, _cache, _eventEndpoint);
			_endpoint.AddHandler(messageHandler);
			
			_handlers.AddRange(new IHandler[] {
					new GetProjectsHandler(_endpoint, _cache),
					new GetFilesHandler(_endpoint, _cache),
					new GetCodeRefsHandler(_endpoint, _cache),
					new GetSignatureRefsHandler(_endpoint, _cache),
					new GoToDefinitionHandler(_endpoint, _cache, _pluginLocator),
					new FindTypeHandler(_endpoint, _cache),
					new SnippetEditHandler(_endpoint, _cache, _path),
					new SnippetDeleteHandler(_cache, _path),

                    // Make sure this handler is the last one since the command can be file extension or language name
                    new LanguageCommandHandler(_endpoint, _cache, _pluginLocator)
				});
			return _endpoint;
		}

		public static ICacheBuilder GetCacheBuilder()
		{
			return _cache;
		}

		public static void Shutdown()
		{
            shutdownPlugins(_pluginLocator, _crawlHandler);
			_tracker.Dispose();
			_eventEndpoint.Stop();
		}

		private static void messageHandler(MessageArgs message, ITypeCache cache, Editor editor)
		{
			var msg = CommandMessage.New(message.Message);
			_handlers
				.Where(x => x.Handles(msg)).ToList()
				.ForEach(x => x.Handle(message.ClientID, msg));
		}

        private static void shutdownPlugins(PluginLocator locator, CrawlHandler handler)
		{
			locator.Locate().ToList()
				.ForEach(x => 
					{
						try {
                            x.Shutdown();
						} catch (Exception ex) {
							Logger.Write(ex.ToString());
						}
					});
		}
		
		private static void initPlugins(PluginLocator locator, CrawlHandler handler)
		{
			new Thread(() =>
				{
					var plugins = locator.Locate();
					foreach (var plugin in plugins) {
						try {
							handler.SetLanguage(plugin.GetLanguage());
                            plugin.Initialize(_path);
							plugin.Crawl(new string[] { _path }, (line) => handler.Handle(line));
						} catch (Exception ex) {
							Logger.Write(ex.ToString());
						}
					}
				}).Start();
		}
	}
}
