using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using BackupUtility.Core.Models;
using BackupUtility.Core.FileManager;
using BackupUtility.Core.Extensions;

namespace BackupUtility.Core.BackupManager {
	public class BackupManager : IBackupManager {
		readonly IFileManager           _source;
		readonly IFileManager           _destination;
		readonly FileChangeValidator    _changeValidator;
		readonly ILogger<BackupManager> _logger;

		public BackupManager(
			IFileManager source, IFileManager destination,
			FileChangeValidator changeValidator = null, ILogger<BackupManager> logger = null) {
			_source          = source;
			_destination     = destination;
			_changeValidator = changeValidator;
			_logger          = logger;
		}

		public async Task Dump(IEnumerable<string> sourceDirs, string backupDir) {
			_logger?.LogInformation($"Dump directories: [{string.Join(',', sourceDirs)}] into '{backupDir}'");
			await EnsureBackupDirectory(backupDir);
			var results = await Task.WhenAll(sourceDirs.Select(sourceDir => DumpSourceDir(sourceDir, backupDir)));
			_logger?.LogInformation($"Dump completed: {FormatResults(results)}");
		}

		async Task<BackupDirResult> DumpSourceDir(string sourceDir, string backupDir) {
			_logger?.LogDebug($"DumpSourceDir: '{sourceDir}' => '{backupDir}'");
			var shortSourceDir = _source.GetDirectoryName(sourceDir);
			var results = await DumpDirectory(sourceDir, _destination.CombinePath(backupDir, shortSourceDir));
			var result = new BackupDirResult(sourceDir, backupDir, results);
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
			_logger?.LogDebug($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}')");
			var sourcePath = _source.CombinePath(sourceDir, sourceFile);
			var destPath = _destination.CombinePath(backupDir, sourceFile);
			try {
				var sourceContent = await _source.ReadAllBytes(sourcePath);
				if ( await _destination.IsFileExists(destPath) ) {
					if ( _changeValidator != null ) {
						var destContent = await _destination.ReadAllBytes(destPath);
						if ( !_changeValidator.IsFileChanged(sourceContent, destContent) ) {
							_logger?.LogDebug($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}'): skipped");
							return new BackupFileResult(sourcePath, destPath, skipped: true);
						}
					}
					await _destination.DeleteFile(destPath);
				}
				await _destination.CreateFile(destPath, sourceContent);
				_logger?.LogDebug($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}'): success");
				return new BackupFileResult(sourcePath, destPath);
			} catch ( Exception e ) {
				_logger?.LogWarning($"DumpFile: '{sourceFile}' ('{sourceDir}' => '{backupDir}'): {e}");
				return new BackupFileResult(sourcePath, destPath, exception: e);
			}
		}

		Task<List<BackupFileResult>> DumpSubDirectory(string sourceDir, string backupDir, string subDir) {
			var sourceSubDir = _source.CombinePath(sourceDir, subDir);
			var destSubDir = _destination.CombinePath(backupDir, subDir);
			return DumpDirectory(sourceSubDir, destSubDir);
		}

		static string FormatResults(IEnumerable<BackupDirResult> results) {
			return $"[{string.Join(',', results)}]";
		}

		static string FormatResults(IEnumerable<BackupFileResult> results) {
			return $"[{string.Join(',', results)}]";
		}
	}
}
