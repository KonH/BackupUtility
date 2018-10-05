using System;
using System.Threading.Tasks;
using BackupUtility.Core.BackupManager;
using BackupUtility.Core.FileManager;

namespace BackupUtility.CLI {
	class Program {
		static IFileManager   _localFs = new LocalFileManager();
		static IBackupManager _manager = new BackupManager(_localFs, _localFs);

		static void Main(string[] args) {
			EntryPont().Wait();
		}

		static async Task EntryPont() {
			Console.WriteLine("Starting...");
			var sources = new string[] { _localFs.CombinePath("root", "child") };
			await _manager.Dump(sources, "backup");
			Console.WriteLine("Backup done.");
			Console.ReadKey();
		}  
	}
}
