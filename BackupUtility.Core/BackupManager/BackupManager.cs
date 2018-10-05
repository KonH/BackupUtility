using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using BackupUtility.Core.FileManager;

namespace BackupUtility.Core.BackupManager {
	public class BackupManager : IBackupManager {
		readonly IFileManager _source;
		readonly IFileManager _destination;

		public BackupManager(IFileManager source, IFileManager destination) {
			_source      = source;
			_destination = destination;
		}

		public async Task Dump(IEnumerable<string> sourceDirs, string backupDir) {
			await EnsureBackupDirectory(backupDir);
			await Task.WhenAll(sourceDirs.Select(sourceDir => DumpSourceDir(sourceDir, backupDir)));
		}

		async Task DumpSourceDir(string sourceDir, string backupDir) {
			var shortSourceDir = _source.GetDirectoryName(sourceDir);
			await DumpDirectory(sourceDir, _destination.CombinePath(backupDir, shortSourceDir));
		}

		async Task EnsureBackupDirectory(string path) {
			if ( !await _destination.IsDirectoryExists(path) ) {
				await _destination.CreateDirectory(path);
			}
		}

		async Task DumpDirectory(string sourceDir, string backupDir) {
			await EnsureBackupDirectory(backupDir);
			var files = await _source.GetFiles(sourceDir);
			await Task.WhenAll(files.Select(sourceFile => DumpFile(sourceDir, backupDir, sourceFile)));
			var dirs = await _source.GetDirectories(sourceDir);
			await Task.WhenAll(dirs.Select(subDir => DumpSubDirectory(sourceDir, backupDir, subDir)));
		}

		async Task DumpFile(string sourceDir, string backupDir, string sourceFile) {
			var contents = await _source.ReadAllBytes(_source.CombinePath(sourceDir, sourceFile));
			var destPath = _destination.CombinePath(backupDir, sourceFile);
			if ( await _destination.IsFileExists(destPath) ) {
				await _destination.DeleteFile(destPath);
			}
			await _destination.CreateFile(destPath, contents);
		}

		async Task DumpSubDirectory(string sourceDir, string backupDir, string subDir) {
			var sourceSubDir = _source.CombinePath(sourceDir, subDir);
			var destSubDir = _destination.CombinePath(backupDir, subDir);
			await DumpDirectory(sourceSubDir, destSubDir);
		}
	}
}
