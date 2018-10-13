using System;
using System.Threading;
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
			var managerLogger = loggerFactory.CreateLogger<BackupManager>();
			_manager = new BackupManager(_localFs, _localFs, historyProvider, changeValidator, managerLogger);
			EntryPont();
		}

		static void EntryPont() {
			Console.WriteLine("Starting backup.");
			Console.WriteLine();

			var source = _localFs.CombinePath("root", "child");
			var task = _manager.Dump(source, "backup");
			task.ConfigureAwait(true);
			var result = task.Result;

			Thread.Sleep(100); // hack for console output delay (several messages may overrides)

			Console.WriteLine();
			WriteLineWithColor("Backup done: " + result, (result.FailedResults > 0) ? ConsoleColor.Red : ConsoleColor.Green);
			Console.ReadKey();
		}

		static void WriteLineWithColor(string str, ConsoleColor color) {
			var startColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(str);
			Console.ForegroundColor = startColor;
		}
	}
}
