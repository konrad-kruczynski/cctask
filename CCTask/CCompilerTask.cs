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
using System.Threading;
using System.Security.Cryptography;
using System.Text;

namespace CCTask
{
	public class CCompilerTask : Task
	{
		[Required]
		public ITaskItem[] Sources { get; set; }

		public ITaskItem[] SourceDirectories { get; set; }

		public string Output { get; set; }

		[Output]
		public ITaskItem[] Outputs { get; set; }

		public string Flags  { get; set; }
		public string CFlags { get; set; }
		public string LFlags { get; set; }

		public bool Link { get; set; }

		public bool PutObjectFilesWhereSources { get; set; }

		public CCompilerTask()
		{
		}

		public override bool Execute()
		{
			Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically
			var objectFiles = new List<string>();

			var buildPath = Path.GetDirectoryName(Output);
			buildPath = buildPath == string.Empty ? Directory.GetCurrentDirectory() : Path.GetFullPath(buildPath);
			buildDirectory = Path.Combine(buildPath, BuildDirectoryName + "_" + Path.GetFileName(Output));
			Directory.CreateDirectory(buildDirectory);


			// compilation
			if(SourceDirectories == null) 
			{
				SourceDirectories = new ITaskItem[0];
			}
			var allSources = Sources.Select(x => x.ItemSpec).Union(SourceDirectories.SelectMany(x => Directory.GetFiles(x.ItemSpec).Where(y => Path.GetExtension(y) == ".c")));
			allSources = allSources.Select(x => Path.GetFullPath(x));
			var compiler = CompilerProvider.Instance.CCompiler;
			var compilationResult = System.Threading.Tasks.Parallel.ForEach(allSources, (source, loopState) =>
			{
				var objectFile = CToO(source);
				lock(objectFiles)
				{
					objectFiles.Add(objectFile);
				}
				if(!compiler.Compile(source, objectFile, Flags ?? string.Empty, CFlags ?? string.Empty, SourceHasChanged))
				{
					loopState.Break();
				}
			});
			if(compilationResult.LowestBreakIteration != null)
			{
				return false;
			}

			Outputs = objectFiles.Select(x => new TaskItem(x)).ToArray();

			if (Link)
			{
				// linking
				var linker = CompilerProvider.Instance.CLinker;
				var result = linker.Link(objectFiles, Path.Combine(buildPath, Output), LFlags ?? string.Empty, SourceHasChanged);
				return result;
			}


				return true;
			}
		}

		private string buildDirectory;
	}
}

