using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

namespace CCTask
{
	public class FileCacheManager : IDisposable
	{
		public FileCacheManager(string directory = null)
		{
			hasherSource = new ThreadLocal<MD5>(() => MD5.Create());
			hashDb = new Dictionary<string, Tuple<string, string>>();

			hashDbFile = Path.Combine(directory ?? Directory.GetCurrentDirectory(), HashDbFilename);
			Load();
		}

		public void Dispose()
		{
			Save();
		}

		private void Load()
		{
			if(!File.Exists(hashDbFile))
			{
				return;
			}
			foreach(var line in File.ReadLines(hashDbFile))
			{
				var fileAndHash = line.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				hashDb.Add(fileAndHash[0], Tuple.Create(fileAndHash[1], fileAndHash[2]));
			}
		}

		private void Save()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(hashDbFile));
			File.WriteAllLines(hashDbFile, hashDb.Select(x => string.Format("{0};{1};{2}", x.Key, x.Value.Item1, x.Value.Item2)));
		}

		public bool SourceHasChanged(IEnumerable<string> sources, string args)
		{
			var changed = false;
			foreach(var source in sources) 
			{
				changed = changed | SourceHasChanged(source, args);
			}
			return changed;
		}

		private bool SourceHasChanged(string sourcePath, string args)
		{
			if(!File.Exists(sourcePath))
			{
				return true;
			}
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
				result = (hashDb[sourcePath].Item1 != hash || hashDb[sourcePath].Item2 != args);
			}
			if(result)
			{
				hashDb[sourcePath] = Tuple.Create(hash, args);
			}
			return result;
		}

		private string CalculateMD5(string s)
		{
			var md5 = hasherSource.Value;
			return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(s))).ToLower().Replace("-", "");
		}

		private readonly ThreadLocal<MD5> hasherSource;
		private readonly Dictionary<string, Tuple<string, string>> hashDb;
		private readonly string hashDbFile;
		private const string HashDbFilename = "hashes";
	}
}

