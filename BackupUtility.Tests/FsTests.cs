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
		public void GetDirectoryNameIsValid() {
			var path = _manager.CombinePath("root", "child");
			Assert.Equal("child", _manager.GetDirectoryName(path));
		}

		[Fact]
		public async void CreateDirProducesNewDirectory() {
			var path = _manager.CombinePath(_root, "dir");
			Assert.False(await _manager.IsDirectoryExists(path));
			await _manager.CreateDirectory(path);
			Assert.True(await _manager.IsDirectoryExists(path));
		}

		[Fact]
		public async void CreateFileProducesFile() {
			var path = _manager.CombinePath(_root, "file");
			Assert.False(await _manager.IsFileExists(path));
			await _manager.CreateFile(path, new byte[0]);
			Assert.True(await _manager.IsFileExists(path));
		}

		[Fact]
		public async void FileDontExistsAfterDelete() {
			var path = _manager.CombinePath(_root, "file_to_detete");
			await _manager.CreateFile(path, new byte[0]);
			Assert.True(await _manager.IsFileExists(path));
			await _manager.DeleteFile(path);
			Assert.False(await _manager.IsFileExists(path));
		}

		[Fact]
		public async void ReadAllBytesOnNonExistsFileRaisesException() {
			var ex = await Record.ExceptionAsync(() => _manager.ReadAllBytes("invalid"));
			Assert.NotNull(ex);
		}

		[Fact]
		public async void ReadAllBytesReturnsCorrectData() {
			var path = _manager.CombinePath(_root, "file");
			var expectedData = new byte[] { 42 };
			await _manager.CreateFile(path, expectedData);
			var actualData = await _manager.ReadAllBytes(path);
			Assert.Equal(expectedData, actualData);
		}

		[Fact]
		public async void CopyFileProducesCorrectData() {
			var sourcePath = _manager.CombinePath(_root, "source");
			var destPath = _manager.CombinePath(_root, "destination");
			var expectedData = new byte[] { 13 };
			await _manager.CreateFile(sourcePath, expectedData);
			await _manager.CopyFile(sourcePath, destPath);
			var actualData = await _manager.ReadAllBytes(destPath);
			Assert.Equal(expectedData, actualData);
		}

		[Fact]
		public async void CopyFileReplacesAnotherFile() {
			var sourcePath = _manager.CombinePath(_root, "source");
			var destPath = _manager.CombinePath(_root, "destination");
			var expectedData = new byte[] { 13 };
			await _manager.CreateFile(sourcePath, expectedData);
			await _manager.CreateFile(destPath, new byte[0]);
			await _manager.CopyFile(sourcePath, destPath);
			var actualData = await _manager.ReadAllBytes(destPath);
			Assert.Equal(expectedData, actualData);
		}

		[Fact]
		public async void GetDirectoriesReturnsValidDir() {
			await _manager.CreateDirectory(_manager.CombinePath(_root, "newDir"));
			var dirs = await _manager.GetDirectories(_root);
			Assert.Contains("newDir", dirs);
		}

		[Fact]
		public async void GetFielsReturnsValidFile() {
			await _manager.CreateFile(_manager.CombinePath(_root, "newFile"), new byte[0]);
			var files = await _manager.GetFiles(_root);
			Assert.Contains("newFile", files);
		}
	}

	public class MockFsTests : FsTests {
		public MockFsTests() : base("temp", new MockFileManager()) { }
	}

	public class LocalFsTests : FsTests {
		public LocalFsTests() : base("Temp", new LocalFileManager()) { }
	}
}
