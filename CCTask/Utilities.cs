using System;
using System.IO;

namespace CCTask
{
	internal static class Utilities
	{
		public static bool SourceHasChanged(string source, string result)
		{
			if(!File.Exists(result))
			{
				return true;
			}
			return File.GetLastWriteTime(source) >= File.GetLastWriteTime(result);
		}
	}
}

