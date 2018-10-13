using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BackupUtility.Core.BackupManager;
using BackupUtility.Core.FileManager;
using BackupUtility.Core.Extensions;

namespace BackupUtility.CLI {
	class Program {
		static IFileManager   _localFs = new LocalFileManager();
		static IBackupManager _manager = null;

		static void Main(string[] args) {
			var changeValidator = new FileChangeValidator();
			var loggerFactory = new LoggerFactory()
				.AddConsole(LogLevel.Debug);
			var logger = loggerFactory.CreateLogger<BackupManager>();
			_manager = new BackupManager(_localFs, _localFs, changeValidator, logger);
			EntryPont().Wait();
		}

		static async Task EntryPont() {
			var source = _localFs.CombinePath("root", "child");
			await _manager.Dump(source, "backup");
			Console.ReadKey();
		}
	}
}
