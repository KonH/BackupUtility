using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BackupUtility.Core.BackupManager;
using BackupUtility.Core.FileManager;
using BackupUtility.Core.Extensions;
using BackupUtility.Core.TimeManager;

namespace BackupUtility.CLI {
	class Program {
		static IFileManager   _localFs;
		static IBackupManager _manager;

		static void Main(string[] args) {
			_localFs = new LocalFileManager();
			var changeValidator = new FileChangeValidator();
			var historyProvider = new DefaultHistoryProvider(new RealTimeManager(), 3);
			var loggerFactory = new LoggerFactory()
				.AddConsole(LogLevel.Debug);
			var logger = loggerFactory.CreateLogger<BackupManager>();
			_manager = new BackupManager(_localFs, _localFs, historyProvider, changeValidator, logger);
			EntryPont().Wait();
		}

		static async Task EntryPont() {
			var source = _localFs.CombinePath("root", "child");
			await _manager.Dump(source, "backup");
			Console.ReadKey();
		}
	}
}
