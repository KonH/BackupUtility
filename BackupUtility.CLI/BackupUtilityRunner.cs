using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BackupUtility.Core.Models;
using BackupUtility.Core.Extensions;
using BackupUtility.Core.FileManager;
using BackupUtility.Core.TimeManager;
using BackupUtility.Core.BackupManager;

namespace BackupUtility.CLI {
	class BackupUtilityRunner {
		ILoggerFactory LoggerFactory;

		public void EntryPoint() {
			var config = ReadConfiguration();
			if ( config == null ) {
				WriteLineWithColor("No configuration!", ConsoleColor.Red);
				return;
			}

			LoggerFactory = ConfigureLogging(config);

			var tasks = CreateTasks(config, new RealTimeManager());
			if ( (tasks == null) || (tasks.Count == 0) ) {
				WriteLineWithColor("No tasks to run!", ConsoleColor.Red);
				return;
			}

			var results = ExecuteTasks(tasks);
			Console.WriteLine();
			WriteLineWithColor("Summary:", ConsoleColor.Yellow);
			foreach ( var result in results ) {
				WriteBackupResults(result);
			}
		}

		ILoggerFactory ConfigureLogging(BackupUtilityConfiguration config) {
			var loggerFactory = new LoggerFactory();
			foreach ( var log in config.Logging ) {
				switch ( log.Type ) {
					case LogType.Console: {
							loggerFactory.AddConsole(log.Level);
						}
						break;

					case LogType.File: {
							loggerFactory.AddFile("{Date}.txt", log.Level);
						}
						break;
				}
			}
			return loggerFactory;
		}

		BackupUtilityConfiguration ReadConfiguration() {
			var configBuilder = new ConfigurationBuilder();
			var configRoot = configBuilder.AddJsonFile("config.json").Build();
			var instance = new BackupUtilityConfiguration();
			configRoot.Bind(instance);

			WriteLineWithColor("Current configuration:", ConsoleColor.Yellow);
			Console.WriteLine("- Logging:");
			foreach ( var logging in instance.Logging ) {
				Console.WriteLine($"- - {logging.Type}: {logging.Level}");
			}
			Console.WriteLine("- SFTP:");
			foreach ( var sftp in instance.Sftp ) {
				Console.WriteLine(
					$" - - '{sftp.Key}': " +
					$"'{sftp.Value.Host}' ('{sftp.Value.UserName}':'{sftp.Value.Password}')");
			}
			Console.WriteLine("- Backup:");
			foreach ( var backup in instance.Backup ) {
				Console.WriteLine(
					$" - - from: '{backup.From.Path}' [{string.Join(", ", backup.From.Pathes)}] " +
					$"({backup.From.Mode}, '{backup.From.Host}'), " +
					$"to: '{backup.To.Path}' ({backup.To.Mode}, '{backup.To.Host}')");
			}
			Console.WriteLine();

			return instance;
		}

		List<BackupTask> CreateTasks(BackupUtilityConfiguration config, ITimeManager time) {
			var tasks = new List<BackupTask>();
			foreach ( var backup in config.Backup ) {
				var sourceFs = CreateFileManager(config.Sftp, backup.From.Mode, backup.From.Host);
				if ( sourceFs == null ) {
					return null;
				}
				var destFs = CreateFileManager(config.Sftp, backup.To.Mode, backup.To.Host);
				if ( destFs == null ) {
					return null;
				}
				var fromPathes = backup.From.Pathes;
				if ( fromPathes.Count == 0 ) {
					fromPathes = new List<string> { backup.From.Path };
				}
				foreach ( var path in fromPathes ) {
					if ( string.IsNullOrEmpty(path) ) {
						WriteLineWithColor("Empty source path!", ConsoleColor.Red);
						return null;
					}
					if ( string.IsNullOrEmpty(backup.To.Path) ) {
						WriteLineWithColor("Empty destination path!", ConsoleColor.Red);
						return null;
					}
					var task = new BackupTask(
						path,
						backup.To.Path,
						sourceFs,
						destFs,
						new DefaultHistoryProvider(time, 3),
						new FileChangeValidator()
					);
					tasks.Add(task);
				}
			}
			return tasks;
		}

		IFileManager CreateFileManager(Dictionary<string, SftpOptions> sftpOptions, FileSystemMode mode, string host) {
			switch ( mode ) {
				case FileSystemMode.Local: return new LocalFileManager();
				case FileSystemMode.SFTP: {
						if ( sftpOptions.TryGetValue(host, out var options) ) {
							var logger = LoggerFactory.CreateLogger<SftpFileManager>();
							return new SftpFileManager(options.Host, options.UserName, options.Password, logger);
						}
						return null;
					}
				default: return null;
			}
		}

		List<BackupDirResult> ExecuteTasks(List<BackupTask> tasks) {
			return tasks.Select(task => ProcessBackup(task)).ToList();
		}

		BackupDirResult ProcessBackup(BackupTask backupTask) {
			WriteLineWithColor($"Starting backup: '{backupTask.SourceDir}' => '{backupTask.DestinationDir}'.", ConsoleColor.Yellow);
			Console.WriteLine();

			var manager = new BackupManager(
				backupTask.SourceFs,
				backupTask.DestinationFs,
				backupTask.HistoryProvider,
				backupTask.ChangeValidator,
				LoggerFactory.CreateLogger<BackupManager>()
			);
			var task = manager.Dump(backupTask.SourceDir, backupTask.DestinationDir);
			task.ConfigureAwait(true);
			var result = task.Result;

			Thread.Sleep(100); // hack for console output delay (several messages may overrides)

			Console.WriteLine();
			WriteBackupResults(result);
			Console.WriteLine();

			return result;
		}

		void WriteBackupResults(BackupDirResult result) {
			WriteLineWithColor(
				"Backup done: " + result,
				(result.FailedResults > 0) ? ConsoleColor.Red : ConsoleColor.Green
			);
		}

		void WriteLineWithColor(string str, ConsoleColor color) {
			var startColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(str);
			Console.ForegroundColor = startColor;
		}
	}
}
