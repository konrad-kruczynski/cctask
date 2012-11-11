using System;
using System.Diagnostics;
using Microsoft.Build.Utilities;

namespace CCTask
{
	internal sealed class RunWrapper
	{
		internal RunWrapper(string path, string options, TaskLoggingHelper log)
		{
			this.log = log;
			startInfo = new ProcessStartInfo(path, options);
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardInput = true;
		}

		internal bool Run ()
		{
			var process = new Process { StartInfo = startInfo };
			process.Start();
			string line;
			while((line = process.StandardOutput.ReadLine()) != null)
			{
				log.LogMessage(line);
			}
			while((line = process.StandardError.ReadLine()) != null) 
			{
				log.LogWarning(line);
			}

		}

		private readonly ProcessStartInfo startInfo;
		private readonly TaskLoggingHelper log;
	}
}

