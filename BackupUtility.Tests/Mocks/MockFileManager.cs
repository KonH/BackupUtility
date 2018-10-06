using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using BackupUtility.Core.FileManager;

namespace BackupUtility.Tests.Mocks {
	class MockFileException : Exception { }

	class MockFileManager : IFileManager {
		const char Delimiter = ':';

		class MockDirectory {
			public IEnumerable<string> Directories => _directories.Keys;
			public IEnumerable<string> Files       => _files.Keys;

			Dictionary<string, byte[]>        _files       = new Dictionary<string, byte[]>();
			Dictionary<string, MockDirectory> _directories = new Dictionary<string, MockDirectory>();

			public bool TryGetDirectory(string name, out MockDirectory dir) {
				return _directories.TryGetValue(name, out dir);
			}

			public void AddDirectory(string name, MockDirectory dir) {
				EnsureEntryDontExist(name);
				_directories.Add(name, dir);
			}

			public bool ContainsDirectory(string name) {
				return _directories.ContainsKey(name);
			}

			public void RemoveDirectory(string name) {
				_directories.Remove(name);
			}

			public bool TryGetFile(string name, out byte[] file) {
				return _files.TryGetValue(name, out file);
			}

			public void AddFile(string name, byte[] file) {
				EnsureEntryDontExist(name);
				_files.Add(name, file);
			}

			public bool ContainsFile(string name) {
				return _files.ContainsKey(name);
			}

			public void RemoveFile(string name) {
				_files.Remove(name);
			}

			void EnsureEntryDontExist(string name) {
				if ( ContainsDirectory(name) || ContainsFile(name) ) {
					throw new MockFileException();
				} 
			}
		}

		MockDirectory _root = new MockDirectory();

		public string CombinePath(params string[] parts) {
			return string.Join(Delimiter, parts);
		}

		public string GetDirectoryName(string fullPath) {
			return fullPath.Split(Delimiter).LastOrDefault();
		}

		public async Task CopyFile(string fromFilePath, string toFilePath) {
			var data = await ReadAllBytes(fromFilePath);
			if ( await IsFileExists(toFilePath) ) {
				await DeleteFile(toFilePath);
			}
			await CreateFile(toFilePath, data);
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
			if ( !parent.TryGetDirectory(dirs[0], out cur) ) {
				cur = new MockDirectory();
				parent.AddDirectory(dirs[0], cur);
			}
			CreateDirectory(cur, dirs.Slice(1));
		}

		public Task CreateFile(string filePath, byte[] bytes) {
			CreateFile(_root, filePath.Split(Delimiter), bytes);
			return Task.CompletedTask;
		}

		void CreateFile(MockDirectory parent, Span<string> parts, byte[] bytes) {
			if ( parts.Length == 0 ) {
				ThrowCommonException();
			}
			if ( parts.Length == 1 ) {
				parent.AddFile(parts[0], bytes);
				return;
			}
			CreateFile(GetDirectoryOrThrow(parent, parts[0]), parts.Slice(1), bytes);
		}

		public Task DeleteDirectory(string directoryPath) {
			DeleteDirectory(_root, directoryPath.Split(Delimiter));
			return Task.CompletedTask;
		}

		void DeleteDirectory(MockDirectory parent, Span<string> dirs) {
			if ( dirs.Length == 0 ) {
				ThrowCommonException();
			}
			if ( dirs.Length == 1 ) {
				parent.RemoveDirectory(dirs[0]);
				return;
			}
			DeleteDirectory(GetDirectoryOrThrow(parent, dirs[0]), dirs.Slice(1));
		}

		public Task DeleteFile(string filePath) {
			DeleteFile(_root, filePath.Split(Delimiter));
			return Task.CompletedTask;
		}

		void DeleteFile(MockDirectory parent, Span<string> dirs) {
			if ( dirs.Length == 0 ) {
				ThrowCommonException();
			}
			if ( dirs.Length == 1 ) {
				parent.RemoveFile(dirs[0]);
				return;
			}
			DeleteFile(GetDirectoryOrThrow(parent, dirs[0]), dirs.Slice(1));
		}

		public Task<IEnumerable<string>> GetDirectories(string directoryPath) {
			IEnumerable<string> dirs = GetDirectoryOrThrow(_root, directoryPath.Split(Delimiter)).Directories;
			return Task.FromResult(dirs);
		}

		public Task<IEnumerable<string>> GetFiles(string directoryPath) {
			IEnumerable<string> files = GetDirectoryOrThrow(_root, directoryPath.Split(Delimiter)).Files;
			return Task.FromResult(files);
		}

		public Task<bool> IsDirectoryExists(string directoryPath) {
			return Task.FromResult(IsDirectoryExists(_root, directoryPath.Split(Delimiter)));
		}

		bool IsDirectoryExists(MockDirectory parent, Span<string> dirs) {
			if ( dirs.Length == 0 ) {
				return true;
			}
			MockDirectory cur;
			if ( !parent.TryGetDirectory(dirs[0], out cur) ) {
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
				return parent.ContainsFile(dirs[0]);
			}
			MockDirectory cur;
			if ( !parent.TryGetDirectory(dirs[0], out cur) ) {
				return false;
			}
			return IsFileExists(cur, dirs.Slice(1));
		}

		public Task<byte[]> ReadAllBytes(string filePath) {
			return Task.FromResult(ReadAllBytes(_root, filePath.Split(Delimiter)));
		}

		byte[] ReadAllBytes(MockDirectory parent, Span<string> parts) {
			if ( parts.Length == 0 ) {
				ThrowCommonException();
			}
			if ( parts.Length == 1 ) {
				if ( parent.TryGetFile(parts[0], out var bytes) ) {
					return bytes;
				}
				ThrowCommonException();
			}
			return ReadAllBytes(GetDirectoryOrThrow(parent, parts[0]), parts.Slice(1));
		}

		MockDirectory GetDirectoryOrThrow(MockDirectory parent, string name) {
			MockDirectory cur;
			if ( !parent.TryGetDirectory(name, out cur) ) {
				ThrowCommonException();
			}
			return cur;
		}

		MockDirectory GetDirectoryOrThrow(MockDirectory parent, Span<string> parts) {
			if ( parts.Length == 0 ) {
				ThrowCommonException();
			}
			if ( parts.Length == 1 ) {
				return GetDirectoryOrThrow(parent, parts[0]);
			}
			return GetDirectoryOrThrow(GetDirectoryOrThrow(parent, parts[0]), parts.Slice(1));
		}

		void ThrowCommonException() {
			throw new MockFileException();
		}
	}
}
