using System;
using System.Linq;
using BackupUtility.Core.BackupManager;
using BackupUtility.Core.Extensions;
using BackupUtility.Core.FileManager;
using BackupUtility.Tests.Mocks;
using Xunit;

namespace BackupUtility.Tests {
	public class BackupHistoryTests {
		MockTimeManager _time;
		IFileManager    _fs;
		HistoryProvider _history;
		IBackupManager  _backup;

		public BackupHistoryTests() {
			_time    = new MockTimeManager(DateTime.UnixEpoch);
			_fs      = new MockFileManager(_time);
			_history = new DefaultHistoryProvider(_time, 3);
			_backup  = new BackupManager(_fs, _fs, _history, null, null);
		}

		[Fact]
		public void HistoryReturnsCorrectFileName() {
			Assert.NotNull(_history.ConvertFileNameToVersionedFileName("temp"));
		}

		[Fact]
		public async void FilesInDirectoryBackedUpWithHistory() {
			var sourceDir = "source";
			var backupDir = "backup";
			var fileToBackup = "file";
			var firstContent = new byte[0];
			var secondContent = new byte[] { 42 };
			// Ordinary case
			await _fs.CreateDirectory(sourceDir);
			await _fs.CreateFile(_fs.CombinePath(sourceDir, fileToBackup), firstContent);
			await _backup.Dump(sourceDir, backupDir);
			// Modify file
			await _fs.DeleteFile(fileToBackup);
			await _fs.CreateFile(fileToBackup, secondContent);
			await _backup.Dump(sourceDir, backupDir);
			// Latest file must exists 
			Assert.True(await _fs.IsFileExists(_fs.CombinePath(backupDir, sourceDir, fileToBackup)));
			Assert.Equal(secondContent, await _fs.ReadAllBytes(fileToBackup));
			// First file must exists in history
			var historyDirName = _history.ConvertFileNameToHistoryDirectoryName(fileToBackup);
			var historyDirPath = _fs.CombinePath(backupDir, sourceDir, historyDirName);
			Assert.True(await _fs.IsDirectoryExists(historyDirPath));
			var filesInside = (await _fs.GetFiles(historyDirPath)).ToList();
			Assert.True(filesInside.Count == 1);
			var fileName = filesInside[0];
			var contentInHistory = await _fs.ReadAllBytes(_fs.CombinePath(backupDir, sourceDir, historyDirName, fileName));
			Assert.Equal(firstContent, contentInHistory);
		}

		[Fact]
		public async void FilesBackedUpWithGivenDepth() {
			var depth = _history.Depth;
			var sourceDir = "source";
			var backupDir = "backup";
			var fileToBackup = "file";
			await _fs.CreateDirectory(sourceDir);
			await _fs.CreateFile(_fs.CombinePath(sourceDir, fileToBackup), new byte[0]);
			await _backup.Dump(sourceDir, backupDir);
			for ( var i = 0; i < depth + 1; i++ ) {
				await _fs.DeleteFile(fileToBackup);
				await _fs.CreateFile(fileToBackup, new byte[] { 1, (byte)i });
				_time.Advance(TimeSpan.FromSeconds(5));
				await _backup.Dump(sourceDir, backupDir);
			}
			var historyDirName = _history.ConvertFileNameToHistoryDirectoryName(fileToBackup);
			var historyDirPath = _fs.CombinePath(backupDir, sourceDir, historyDirName);
			var filesInside = (await _fs.GetFiles(historyDirPath)).ToList();
			Assert.Equal(depth, filesInside.Count);
		}
	}
}