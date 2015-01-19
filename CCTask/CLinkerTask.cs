using Microsoft.Build.Utilities;
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

			if(!ObjectFiles.Any())
			{
				return true;
			}

			var ofiles = ObjectFiles.Select(x => x.ItemSpec);
			// linking
			var linker = CompilerProvider.Instance.CLinker;
			var flags = (Flags != null && Flags.Any()) ? Flags.Aggregate(string.Empty, (curr, next) => string.Format("{0} {1}", curr, next.ItemSpec)) : string.Empty;
			return linker.Link(ofiles, Output, flags, (x, y) => true);
		}
	}
}

