using System;
using BackupUtility.Core.FileManager;
using BackupUtility.Core.BackupManager;
using BackupUtility.Core.FileHasher;
using BackupUtility.Core.Extensions;
using BackupUtility.Core.TimeManager;
using BackupUtility.Tests.Mocks;
using Xunit;

namespace BackupUtility.Tests {
	public class BackupTests {
		ITimeManager   _time;
		IFileManager   _fs;
		IBackupManager _backup;

		public BackupTests() {
			_time   = new MockTimeManager(DateTime.MinValue);
			_fs     = new MockFileManager(_time);
			_backup = new BackupManager(_fs, _fs, null, null, null);
		}

		[Fact]
		public async void FilesInDirectoryBackedUp() {
			var sourceDir = "source";
			var backupDir = "backup";
			var fileToBackup = "file";
			await _fs.CreateDirectory(sourceDir);
			await _fs.CreateFile(_fs.CombinePath(sourceDir, fileToBackup), new byte[0]);
			await _backup.Dump(sourceDir, backupDir);
			Assert.True(await _fs.IsDirectoryExists(backupDir));
			Assert.True(await _fs.IsFileExists(_fs.CombinePath(backupDir, sourceDir, fileToBackup)));
		}

		[Fact]
		public async void FilesInSubDirectoriesBackedUp() {
			var sourceDir = "source";
			var backupDir = "backup";
			var fileToBackup = "file";
			await _fs.CreateDirectory(sourceDir);
			await _fs.CreateDirectory(_fs.CombinePath(sourceDir, "subdir"));
			await _fs.CreateFile(_fs.CombinePath(sourceDir, "subdir", fileToBackup), new byte[0]);
			await _backup.Dump(sourceDir, backupDir);
			Assert.True(await _fs.IsDirectoryExists(backupDir));
			Assert.True(await _fs.IsFileExists(_fs.CombinePath(backupDir, sourceDir, "subdir", fileToBackup)));
		}

		[Fact]
		public async void FilesInDirectoryWithParentBackedUp() {
			var parentSourceDir = "parent";
			var sourceDir = "source";
			var backupDir = "backup";
			var fileToBackup = "file";
			var fullSourceDir = _fs.CombinePath(parentSourceDir, sourceDir);
			await _fs.CreateDirectory(fullSourceDir);
			await _fs.CreateFile(_fs.CombinePath(fullSourceDir, fileToBackup), new byte[0]);
			await _backup.Dump(fullSourceDir, backupDir);
			Assert.True(await _fs.IsDirectoryExists(backupDir));
			Assert.True(await _fs.IsFileExists(_fs.CombinePath(backupDir, sourceDir, fileToBackup)));
		}

		[Fact]
		public async void FileChangeValidatorReturnsTrueOnDifferentData() {
			await _fs.CreateFile("fileChangeTest", new byte[] { 42 });
			var hasher = new DirectFileHasher(_fs);
			var validator = new FileChangeValidator(hasher, hasher);
			Assert.True(await validator.IsFileChanged(new byte[] { 41 }, "fileChangeTest"));
		}

		[Fact]
		public async void FileChangeValidatorReturnsFalseOnSameData() {
			await _fs.CreateFile("fileChangeTest", new byte[] { 42 });
			var hasher = new DirectFileHasher(_fs);
			var validator = new FileChangeValidator(hasher, hasher);
			Assert.False(await validator.IsFileChanged(new byte[] { 42 }, "fileChangeTest"));
		}
	}
}
