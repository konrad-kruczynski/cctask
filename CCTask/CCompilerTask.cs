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

		[Required]
		public string Output { get; set; }

		public string CFlags { get; set; }
		public string LFlags { get; set; }

		public bool PutObjectFilesWhereSources { get; set; }

		public CCompilerTask()
		{
			hasherSource = new ThreadLocal<MD5>(() => MD5.Create());
			hashDb = new Dictionary<string, string>();
		}

		public override bool Execute()
		{
			Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically
			var objectFiles = new List<string>();

			buildDirectory = Path.Combine(Path.GetDirectoryName(Output), BuildDirectoryName + "_" + Path.GetFileName(Output));
			Directory.CreateDirectory(buildDirectory);
			hashDbFile = Path.Combine(buildDirectory, HashDbFilename);

			LoadHashes();

			// compilation
			var compiler = CompilerProvider.Instance.CCompiler;
			var result = System.Threading.Tasks.Parallel.ForEach(Sources, (source, loopState) =>
			{
				var objectFile = CToO(source.ItemSpec);
				lock(objectFiles)
				{
					objectFiles.Add(objectFile);
				}
				if(!compiler.Compile(source.ItemSpec, objectFile, CFlags ?? string.Empty))
				{
					loopState.Break();
				}
			});
			if(result.LowestBreakIteration != null)
			{
				return false;
			}

			// linking
			var linker = CompilerProvider.Instance.CLinker;
			return linker.Link(objectFiles, Output, LFlags ?? string.Empty);
		}

		private void LoadHashes()
		{
			foreach(var line in File.ReadLines(hashDbFile))
			{
				var fileAndHash = line.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				hashDb.Add(fileAndHash[0], fileAndHash[1]);
			}
		}

		private void SaveHashes()
		{
			File.WriteAllLines(hashDbFile, hashDb.Select(x => string.Format("{0};{1}", x.Key, x.Value)));
		}

		private string CToO(string source)
		{
			if(PutObjectFilesWhereSources)
			{
				if(Path.GetExtension(source) == ".c")
				{
					return Path.GetDirectoryName(source) + Path.GetFileNameWithoutExtension(source) + ".o";
				}
				return source + ".o";
			}
			var hash = CalculateMD5(source);
			return Path.Combine(buildDirectory, hash + ".o");
		}

		private bool HasSourceChanged(string sourcePath, string outputPath)
		{
			string hash;
			using(var stream = File.OpenRead(sourcePath))
			{
				var hasher = hasherSource.Value;
				hash = BitConverter.ToString(hasher.ComputeHash(stream));
			}

			var result = false;
			if(!hashDb.ContainsKey(sourcePath))
			{
				result = true;
			}
			else
			{
				result = hashDb[sourcePath] != hash;
			}
			if(result)
			{
				hashDb[sourcePath] = hash;
			}
			return result;
		}

		private string CalculateMD5(string s)
		{
			var md5 = hasherSource.Value;
			return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(s))).ToLower().Replace("-", "");
		}

		private string buildDirectory;
		private string hashDbFile;
		private readonly Dictionary<string, string> hashDb;
		private readonly ThreadLocal<MD5> hasherSource;
		private const string BuildDirectoryName = "buildcache";
		private const string HashDbFilename = "hashes";
	}
}

