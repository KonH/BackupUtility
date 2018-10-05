using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BackupUtility.Core.FileManager;

namespace BackupUtility.Tests.Mocks {
	class MockFileException : Exception {}

	class MockFileManager : IFileManager {
		const char Delimiter = ':';

		class MockDirectory {
			public Dictionary<string, byte[]>        Files       = new Dictionary<string, byte[]>();
			public Dictionary<string, MockDirectory> Directories = new Dictionary<string, MockDirectory>();
		}

		MockDirectory _root = new MockDirectory();

		public string CombinePath(params string[] parts) {
			return string.Join(Delimiter, parts);
		}

		public Task CopyFile(string fromFilePath, string toFilePath) {
			throw new System.NotImplementedException();
		}

		public Task CreateDirectory(string directoryPath) {
			CreateDirectory(_root, directoryPath.Split(Delimiter));
			return Task.CompletedTask;
		}

		void CreateDirectory(MockDirectory parent, Span<string> dirs) {
			if ( dirs.Length == 0 ) {
				return;
			}
			MockDirectory cur;
			if ( !parent.Directories.TryGetValue(dirs[0], out cur) ) {
				cur = new MockDirectory();
				parent.Directories.Add(dirs[0], cur);
			}
			CreateDirectory(cur, dirs.Slice(1));
		}

		public Task CreateFile(string filePath, byte[] bytes) {
			CreateFile(_root, filePath.Split(Delimiter), bytes);
			return Task.CompletedTask;
		}

		void CreateFile(MockDirectory parent, Span<string> parts, byte[] bytes) {
			if ( parts.Length == 0 ) {
				throw new MockFileException();
			}
			if ( parts.Length == 1 ) {
				parent.Files.Add(parts[0], bytes);
				return;
			}
			MockDirectory cur;
			if ( !parent.Directories.TryGetValue(parts[0], out cur) ) {
				throw new MockFileException();
			}
			CreateFile(cur, parts.Slice(1), bytes);
		}

		public Task DeleteDirectory(string directoryPath) {
			DeleteDirectory(_root, directoryPath.Split(Delimiter));
			return Task.CompletedTask;
		}

		void DeleteDirectory(MockDirectory parent, Span<string> dirs) {
			if ( dirs.Length == 0 ) {
				throw new MockFileException();
			}
			if ( dirs.Length == 1 ) {
				parent.Directories.Remove(dirs[0]);
				return;
			}
			MockDirectory cur;
			if ( !parent.Directories.TryGetValue(dirs[0], out cur) ) {
				throw new MockFileException();
			}
			DeleteDirectory(cur, dirs.Slice(1));
		}

		public Task DeleteFile(string filePath) {
			throw new NotImplementedException();
		}

		public Task<IEnumerable<string>> GetDirectories(string directoryPath) {
			throw new NotImplementedException();
		}

		public Task<IEnumerable<string>> GetFiles(string directoryPath) {
			throw new NotImplementedException();
		}

		public Task<bool> IsDirectoryExists(string directoryPath) {
			return Task.FromResult(IsDirectoryExists(_root, directoryPath.Split(Delimiter)));
		}

		bool IsDirectoryExists(MockDirectory parent, Span<string> dirs) {
			if ( dirs.Length == 0 ) {
				return true;
			}
			MockDirectory cur;
			if ( !parent.Directories.TryGetValue(dirs[0], out cur) ) {
				return false;
			}
			return IsDirectoryExists(cur, dirs.Slice(1));
		}

		public Task<bool> IsFileExists(string filePath) {
			return Task.FromResult(IsFileExists(_root, filePath.Split(Delimiter)));
		}

		bool IsFileExists(MockDirectory parent, Span<string> dirs) {
			if ( dirs.Length == 0 ) {
				return true;
			}
			if ( dirs.Length == 1 ) {
				return parent.Files.ContainsKey(dirs[0]);
			}
			MockDirectory cur;
			if ( !parent.Directories.TryGetValue(dirs[0], out cur) ) {
				return false;
			}
			return IsFileExists(cur, dirs.Slice(1));
		}

		public Task<byte[]> ReadAllBytes(string filePath) {
			throw new System.NotImplementedException();
		}
	}
}
