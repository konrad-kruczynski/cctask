/**
 * CCTask
 * 
 * Copyright 2012 Konrad Kruczyński <konrad.kruczynski@gmail.com>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:

 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */ 
using System;
using System.Linq;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO;
using System.Collections.Generic;

namespace CCTask
{
	public class CCompilerTask : Task
	{
		[Required]
		public ITaskItem[] Sources { get; set; }

		[Required]
		public string Output { get; set; }

		public string CFlags { get; set; }
		public string LFlags { get; set; }

		public override bool Execute()
		{
			Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically
			var objectFiles = new List<string>();

			// compilation
			var compiler = CompilerProvider.Instance.CCompiler;
			foreach(var source in Sources)
			{
				var objectFile = CToO(source.ItemSpec);
				objectFiles.Add(objectFile);
				if(!Utilities.SourceHasChanged(source.ItemSpec, objectFile))
				{
					continue;
				}
				if(!compiler.Compile(source.ItemSpec, objectFile, CFlags ?? string.Empty))
				{
					return false;
				}
			}

			// linking
			if(!objectFiles.Any(x => Utilities.SourceHasChanged(x, Output)))
			{
				// everything is up to date
				return true;
			}
			var linker = CompilerProvider.Instance.CLinker;
			return linker.Link(objectFiles, Output, LFlags ?? string.Empty);
		}

		private static string CToO(string source)
		{
			if(Path.GetExtension(source) == ".c")
			{
				return Path.GetDirectoryName(source) + Path.GetFileNameWithoutExtension(source) + ".o";
			}
			return source + ".o";
		}
	}
}

