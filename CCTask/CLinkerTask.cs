using System;
using Microsoft.Build.Utilities;
using System.IO;
using Microsoft.Build.Framework;
using System.Linq;

namespace CCTask
{
	public class CLinkerTask : Task
	{
		[Required]
		public ITaskItem[] ObjectFiles { get; set; }

		public ITaskItem[] Flags { get; set; }

		[Required]
		public string Output { get; set; }

		public override bool Execute()
		{
			Logger.Instance = new XBuildLogProvider(Log); // TODO: maybe initialise statically; this put in constructor causes NRE 

			var buildPath = Path.GetDirectoryName(Output);
			buildPath = buildPath == string.Empty ? Directory.GetCurrentDirectory() : Path.GetFullPath(buildPath);
			var ofiles = ObjectFiles.Select(x => Path.GetFullPath(x.ItemSpec));
			// linking
			var linker = CompilerProvider.Instance.CLinker;
			return linker.Link(ofiles, Path.Combine(buildPath, Output), Flags.Aggregate(string.Empty, (curr, next) => string.Format("{0} {1}", curr, next.ItemSpec)), (x, y) => true);
		}
	}
}

