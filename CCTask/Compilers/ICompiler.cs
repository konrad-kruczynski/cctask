using System;

namespace CCTask.Compilers
{
	public interface ICompiler
	{
		bool Compile(string source, string output);
	}
}

