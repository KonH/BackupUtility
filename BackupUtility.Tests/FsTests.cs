using System;
using BackupUtility.Tests.Mocks;
using BackupUtility.Core.FileManager;
using Xunit;

namespace BackupUtility.Tests {
	public abstract class FsTests : IDisposable {
		protected readonly string       _root;
		protected readonly IFileManager _manager;

		public FsTests(string root, IFileManager manager) {
			_root = root;
			_manager = manager;
			_manager.Connect();
			if ( _manager.IsDirectoryExists(_root).Result ) {
				_manager.DeleteDirectory(_root).Wait();
			}
			_manager.CreateDirectory(_root).Wait();
		}

		public void Dispose() {
			_manager.DeleteDirectory(_root).Wait();
			_manager.Disconnect();
		}

		[Fact]
		public void GetDirectoryNameIsValid() {
			var path = _manager.CombinePath("root", "child");
			Assert.Equal("child", _manager.GetDirectoryName(path));
		}

		[Fact]
		public async void CreateDirProducesNewDirectory() {
			var path = _manager.CombinePath(_root, "createDirTest");
			Assert.False(await _manager.IsDirectoryExists(path));
			await _manager.CreateDirectory(path);
			Assert.True(await _manager.IsDirectoryExists(path));
		}

		[Fact]
		public async void CreateFileProducesFile() {
			var path = _manager.CombinePath(_root, "createFileTest");
			Assert.False(await _manager.IsFileExists(path));
			await _manager.CreateFile(path, new byte[0]);
			Assert.True(await _manager.IsFileExists(path));
		}

		[Fact]
		public async void FileDontExistsAfterDelete() {
			var path = _manager.CombinePath(_root, "fileToDetete");
			await _manager.CreateFile(path, new byte[0]);
			Assert.True(await _manager.IsFileExists(path));
			await _manager.DeleteFile(path);
			Assert.False(await _manager.IsFileExists(path));
		}

		[Fact]
		public async void ReadAllBytesOnNonExistsFileRaisesException() {
			var ex = await Record.ExceptionAsync(() => _manager.ReadAllBytes("invalidFile"));
			Assert.NotNull(ex);
		}

		[Fact]
		public async void ReadAllBytesReturnsCorrectData() {
			var path = _manager.CombinePath(_root, "fileToRead");
			var expectedData = new byte[] { 42 };
			await _manager.CreateFile(path, expectedData);
			var actualData = await _manager.ReadAllBytes(path);
			Assert.Equal(expectedData, actualData);
		}

		[Fact]
		public async void CopyFileProducesCorrectData() {
			var sourcePath = _manager.CombinePath(_root, "source1");
			var destPath = _manager.CombinePath(_root, "destination1");
			var expectedData = new byte[] { 13 };
			await _manager.CreateFile(sourcePath, expectedData);
			await _manager.CopyFile(sourcePath, destPath);
			var actualData = await _manager.ReadAllBytes(destPath);
			Assert.Equal(expectedData, actualData);
		}

		[Fact]
		public async void CopyFileReplacesAnotherFile() {
			var sourcePath = _manager.CombinePath(_root, "source2");
			var destPath = _manager.CombinePath(_root, "destination2");
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
		public async void GetFilesReturnsValidFile() {
			await _manager.CreateFile(_manager.CombinePath(_root, "newFile"), new byte[0]);
			var files = await _manager.GetFiles(_root);
			Assert.Contains("newFile", files);
		}

		[Fact]
		public async void CantCreateDirectoryOverFile() {
			var path = _manager.CombinePath(_root, "uniqueTempFile");
			await _manager.CreateFile(path, new byte[0]);
			var ex = await Record.ExceptionAsync(() => _manager.CreateDirectory(path));
			Assert.NotNull(ex);
		}

		[Fact]
		public async void CantCreateFileOverDirectory() {
			var path = _manager.CombinePath(_root, "uniqueTempDir");
			await _manager.CreateDirectory(path);
			var ex = await Record.ExceptionAsync(() => _manager.CreateFile(path, new byte[0]));
			Assert.NotNull(ex);
		}

		[Fact]
		public async void NewFileHaveCorrectModificationTime() {
			var path = _manager.CombinePath(_root, "justCreatedFile");
			var dt = DateTime.UtcNow;
			var deltaSec = 60.0 * 20; // 20 min safe interval to avoid blinking tests & invalid time on server cases
			await _manager.CreateFile(path, new byte[0]);
			var changeTime = await _manager.GetFileChangeTime(path);
			Assert.True(Math.Abs((dt - changeTime).TotalSeconds) <= deltaSec);
		}

		[Fact]
		public async void CanRemoveNonEmptyDirectory() {
			var dirPath = _manager.CombinePath(_root, "nonEmptyDir");
			await _manager.CreateDirectory(dirPath);
			await _manager.CreateFile(_manager.CombinePath(dirPath, "file"), new byte[] { 42 });
			await _manager.DeleteDirectory(dirPath);
			Assert.False(await _manager.IsDirectoryExists(dirPath));
		}

		[Fact]
		public async void CanCreateFileWithNonASCIIName() {
			var filePath = _manager.CombinePath(_root, "BN Bottleneck - школа игры слайдом в стандартном строе.pdf");
			await _manager.CreateFile(filePath, new byte[] { 42 });
			Assert.True(await _manager.IsFileExists(filePath));
		}

		[Fact]
		public async void CanCreateDirectoryWithNonASCIIName() {
			var dirPath = _manager.CombinePath(_root, "BN Bottleneck - школа игры слайдом в стандартном строе");
			await _manager.CreateDirectory(dirPath);
			Assert.True(await _manager.IsDirectoryExists(dirPath));
		}

		[Fact]
		public async void CanCreateFileWithNonASCIINameInSubdirectory() {
			var dirPath = _manager.CombinePath(_root, "BN Bottleneck - школа игры слайдом в стандартном строе 2");
			var filePath = _manager.CombinePath(
				_root,
				 "BN Bottleneck - школа игры слайдом в стандартном строе 2",
				"BN Bottleneck - школа игры слайдом в стандартном строе.pdf");
			await _manager.CreateDirectory(dirPath);
			await _manager.CreateFile(filePath, new byte[] { 42 });
			Assert.True(await _manager.IsFileExists(filePath));
		}
	}

	public class MockFsTests : FsTests {
		public MockFsTests() : base("temp", new MockFileManager(new MockTimeManager(DateTime.UtcNow))) { }
	}

	public class LocalFsTests : FsTests {
		public LocalFsTests() : base("Temp", new LocalFileManager()) { }
	}

	public class SftpFsTests : FsTests {
		public SftpFsTests() : base(SftpConfig.Path, new SftpFileManager(SftpConfig.Host, SftpConfig.UserName, SftpConfig.Password, null)) { }

		[Fact]
		public async void CanCreateFileIfConnectionBroken() {
			_manager.Disconnect();
			var path = _manager.CombinePath(_root, "connectionCheck");
			await _manager.CreateDirectory(path);
			Assert.True(await _manager.IsDirectoryExists(path));
		}
	}
}
