using System;
using NUnit.Framework;
using OpenIDENet.CodeEngine.Core.Crawlers;
using System.Linq;
using System.IO;
using System.Reflection;
namespace OpenIDENet.CodeEngine.Core.Tests.Crawlers
{
	[TestFixture]
	public class CSharpCommentParserTests
	{
		private CSharpFileParser _parser;
		private Fake_CacheBuilder _cache;
		
		[SetUp]
		public void Setup()
		{
			_cache = new Fake_CacheBuilder();
			_parser = new CSharpFileParser(_cache);
			_parser.ParseFile("file1", () => { return getContent(); });
		}
		
		[Test]
		public void Should_not_parse_content_of_multiline_comments()
		{
			Assert.That(_cache.Classes.FirstOrDefault(x => x.Name.Equals("CSharpComments")), Is.Null);
		}
		
		[Test]
		public void Should_look_behind_comments()
		{
			Assert.That(_cache.Classes.FirstOrDefault(x => x.Name.Equals("InComment")), Is.Null);
			Assert.That(_cache.Classes.FirstOrDefault(x => x.Name.Equals("BehindComment")), Is.Not.Null);
		}
		
		[Test]
		public void Should_look_in_front_of_comments()
		{
			Assert.That(_cache.Classes.FirstOrDefault(x => x.Name.Equals("InFronOfCOmment")), Is.Not.Null);
			Assert.That(_cache.Classes.FirstOrDefault(x => x.Name.Equals("ClassBehind")), Is.Null);
		}
		
		private string getContent()
		{
			return File.ReadAllText(Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestResources"), "CSharpComments.txt"));
		}
	}
}
