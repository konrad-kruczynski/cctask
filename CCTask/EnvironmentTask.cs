using System;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace CCTask
{
	public class EnvironmentTask : Task
	{
		[Output]
		public int WordSize { get; private set; }

		public override bool Execute()
		{
			WordSize = Environment.Is64BitOperatingSystem ? 64 : 32;
			return true;
		}

	}
}

