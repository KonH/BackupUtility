using System;
using Xunit;
using BackupUtility.Tests.Mocks;
using BackupUtility.Core.FileManager;

namespace BackupUtility.Tests {
	public abstract class FsTests : IDisposable {
		readonly string       _root;
		readonly IFileManager _manager;

		public FsTests(string root, IFileManager manager) {
			_root = root;
			_manager = manager;
			_manager.CreateDirectory(_root);
		}

		public void Dispose() {
			_manager.DeleteDirectory(_root);
		}

		[Fact]
		public async void CreateDir() {
			var path = _manager.CombinePath(_root, "dir");
			Assert.False(await _manager.IsDirectoryExists(path));
			await _manager.CreateDirectory(path);
			Assert.True(await _manager.IsDirectoryExists(path));
		}

		[Fact]
		public async void CreateFile() {
			var path = _manager.CombinePath(_root, "file");
			Assert.False(await _manager.IsFileExists(path));
			await _manager.CreateFile(path, new byte[0]);
			Assert.True(await _manager.IsFileExists(path));
		}
	}

	public class MockFsTests : FsTests {
		public MockFsTests() : base("temp", new MockFileManager()) { }
	}

	public class LocalFsTests : FsTests {
		public LocalFsTests() : base("Temp", new LocalFileManager()) { }
	}
}
