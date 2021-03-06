using System;
namespace OpenIDE
{
	public class InvalidArgumentsException : Exception
	{
		public string[] Arguments { get; private set; }
		
		public InvalidArgumentsException(string[] args) :
			base("Invalid command line arguments")
		{
			Arguments = args;
		}
	}
}

