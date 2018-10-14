using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using BackupUtility.Core.Models;
using BackupUtility.Core.Extensions;
using BackupUtility.Core.FileManager;

namespace BackupUtility.Core.BackupManager {
	public class BackupManager : IBackupManager {
		readonly IFileManager           _source;
		readonly IFileManager           _destination;
		readonly FileChangeValidator    _changeValidator;
		readonly HistoryProvider        _historyProvider;
		readonly ILogger<BackupManager> _logger;

		Stopwatch      _stopWatch;
		BackupProgress _progress;

		int _currentConcurrentFileDumps = 0;
		int _concurrentFileDumpLimit    = 0;

		public BackupManager(
			IFileManager           source,
			IFileManager           destination,
			HistoryProvider        historyProvider, 
			FileChangeValidator    changeValidator,
			ILogger<BackupManager> logger,
			int                    concurrentFileDumpLimit = -1
		) {
			_source                  = source;
			_destination             = destination;
			_historyProvider         = historyProvider;
			_changeValidator         = changeValidator;
			_logger                  = logger;
			_concurrentFileDumpLimit = concurrentFileDumpLimit;
		}

		public async Task<BackupDirResult> Dump(string sourceDir, string backupDir) {
			_progress = new BackupProgress();
			_logger?.LogInformation($"Dump directory: '{sourceDir}' into '{backupDir}'");
			await Connect();
			await EnsureBackupDirectory(backupDir);
			var result = await DumpSourceDir(sourceDir, backupDir);
			await Disconnect();
			return result;
		}

		async Task Connect() {
			_source.Connect();
			_destination.Connect();
			if ( _changeValidator != null ) {
				await _changeValidator.Load();
			}
		}

		async Task Disconnect() {
			if ( _changeValidator != null ) {
				await _changeValidator.Save();
			}
			_source.Disconnect();
			_destination.Disconnect();
		}

		async Task<BackupDirResult> DumpSourceDir(string sourceDir, string backupDir) {
			_stopWatch = Stopwatch.StartNew();
			_logger?.LogDebug($"DumpSourceDir: '{sourceDir}' => '{backupDir}'");
			var shortSourceDir = _source.GetDirectoryName(sourceDir);
			var results = await DumpDirectory(sourceDir, _destination.CombinePath(backupDir, shortSourceDir));
			_stopWatch.Stop();
			_progress.Finish();
			var result = new BackupDirResult(sourceDir, backupDir, results, _stopWatch.Elapsed);
			_logger?.LogDebug($"DumpSourceDir: {result}");
			return result;
		}

		async Task EnsureBackupDirectory(string path) {
			if ( !await _destination.IsDirectoryExists(path) ) {
				await _destination.CreateDirectory(path);
			}
		}

		async Task<List<BackupFileResult>> DumpDirectory(string sourceDir, string backupDir) {
			_logger?.LogDebug($"DumpDirectory: '{sourceDir}' => '{backupDir}'");
			await EnsureBackupDirectory(backupDir);
			var totalResults = new List<BackupFileResult>();
			var files = await _source.GetFiles(sourceDir);
			totalResults.AddRange(await Task.WhenAll(files.Select(sourceFile => DumpFile(sourceDir, backupDir, sourceFile))));
			var dirs = await _source.GetDirectories(sourceDir);
			var dirFileResults = await Task.WhenAll(dirs.Select(subDir => DumpSubDirectory(sourceDir, backupDir, subDir)));
			foreach ( var subResult in dirFileResults ) {
				totalResults.AddRange(subResult);
			}
			_logger?.LogDebug($"DumpDirectory: '{sourceDir}' => '{backupDir}': {totalResults.Count(r => r.Success)}/{totalResults.Count}");
			return totalResults;
		}

		async Task<BackupFileResult> DumpFile(string sourceDir, string backupDir, string sourceFile) {
			while ( !TryStartNewDump() ) {
				await Task.Delay(100);
			}
			_logger?.LogDebug($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}')");
			var sourcePath = _source.CombinePath(sourceDir, sourceFile);
			var destPath = _destination.CombinePath(backupDir, sourceFile);
			try {
				var sourceContent = await _source.ReadAllBytes(sourcePath);
				if ( await IsNeedToSkipFile(sourceContent, destPath) ) {
					_logger?.LogDebug($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}'): skipped");
					AdvanceFileProgress(sourceContent.Length);
					StopDump();
					return new BackupFileResult(sourcePath, destPath, skipped: true);
				}
				if ( await _destination.IsFileExists(destPath) ) {
					var destContent = await _destination.ReadAllBytes(destPath);
					await TryMoveOldCopyToHistory(backupDir, sourceFile, destContent);
					await _destination.DeleteFile(destPath);
					_changeValidator?.OnFileChanged(destPath);
				}

				await _destination.CreateFile(destPath, sourceContent);
				_changeValidator?.OnFileChanged(destPath);
				_logger?.LogDebug($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}'): success");
				AdvanceFileProgress(sourceContent.Length);
				StopDump();
				return new BackupFileResult(sourcePath, destPath);
			} catch ( Exception e ) {
				_logger?.LogWarning($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}'): {e}");
				StopDump();
				return new BackupFileResult(sourcePath, destPath, exception: e);
			}
		}

		bool TryStartNewDump() {
			if ( _concurrentFileDumpLimit < 0 ) {
				return true;
			}
			var concurrentFileDumps = _currentConcurrentFileDumps;
			if ( concurrentFileDumps < _concurrentFileDumpLimit ) {
				Interlocked.Increment(ref _currentConcurrentFileDumps);
				return true;
			}
			return false;
		}

		void StopDump() {
			Interlocked.Decrement(ref _currentConcurrentFileDumps);
		}

		async Task<bool> IsNeedToSkipFile(byte[] sourceContent, string destPath) {
			if ( _changeValidator != null ) {
				var isChanged = await _changeValidator.IsFileChanged(sourceContent, destPath);
				return !isChanged;
			}
			return false;
		}

		async Task TryMoveOldCopyToHistory(string backupDir, string sourceFile, byte[] destContent) {
			if ( _historyProvider == null ) {
				return;
			}
			var historyDirName = _historyProvider.ConvertFileNameToHistoryDirectoryName(sourceFile);
			var historyDirPath = _destination.CombinePath(backupDir, historyDirName);
			if ( await _destination.IsDirectoryExists(historyDirPath) ) {
				await TryCleanupHistoryDirectory(historyDirPath);
			} else {
				await _destination.CreateDirectory(historyDirPath);
			}
			var nameInHistory = _historyProvider.ConvertFileNameToVersionedFileName(sourceFile);
			var pathInHistory = _destination.CombinePath(historyDirPath, nameInHistory);
			_logger?.LogDebug($"TryMoveOldCopyToHistory('{backupDir}', '{sourceFile}'): create file in history: '{pathInHistory}'");
			await _destination.CreateFile(pathInHistory, destContent);
			_changeValidator?.OnFileChanged(pathInHistory);
		}

		async Task TryCleanupHistoryDirectory(string historyDirPath) {
			do {
				var files = new List<string>(await _destination.GetFiles(historyDirPath));
				if ( files.Count < _historyProvider.Depth ) {
					break;
				}
				var timeRequests = files.Select(
					(name) => _destination.GetFileChangeTime(_destination.CombinePath(historyDirPath, name))
				);
				var fileTimes = await Task.WhenAll(timeRequests);
				var minTimeIndex = Array.IndexOf(fileTimes, fileTimes.Min());
				var minTimeName = files[minTimeIndex];
				var minTimePath = _destination.CombinePath(historyDirPath, minTimeName);
				_logger?.LogDebug($"TryCleanupHistoryDirectory('{historyDirPath}'): delete oldest copy: '{minTimePath}'");
				await _destination.DeleteFile(minTimePath);
				_changeValidator?.OnFileChanged(minTimePath);
			} while ( true );
		}

		Task<List<BackupFileResult>> DumpSubDirectory(string sourceDir, string backupDir, string subDir) {
			var sourceSubDir = _source.CombinePath(sourceDir, subDir);
			var destSubDir = _destination.CombinePath(backupDir, subDir);
			return DumpDirectory(sourceSubDir, destSubDir);
		}

		static string FormatResults(IEnumerable<BackupFileResult> results) {
			return $"[{string.Join(',', results)}]";
		}

		void AdvanceFileProgress(int bytes) {
			_progress.Advance(bytes, _stopWatch.Elapsed);
		}

		public BackupProgress GetLatestProgress() {
			return _progress;
		}
	}
}
