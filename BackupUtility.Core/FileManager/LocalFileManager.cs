using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackupUtility.Core.FileManager {
	public class LocalFileManager : IFileManager {
		public string CombinePath(params string[] parts) {
			return Path.Combine(parts);
		}

		public Task CopyFile(string fromFilePath, string toFilePath) {
			throw new NotImplementedException();
		}

		public Task CreateDirectory(string directoryPath) {
			Directory.CreateDirectory(directoryPath);
			return Task.CompletedTask;
		}

		public Task CreateFile(string filePath, byte[] bytes) {
			using
			( var stream = new FileStream(
				filePath, FileMode.Create, FileAccess.Write, FileShare.None,
				bufferSize:1024, useAsync:true
			) ) {
				return stream.WriteAsync(bytes).AsTask();
			}
		}

		public Task DeleteDirectory(string directoryPath) {
			Directory.Delete(directoryPath, true);
			return Task.CompletedTask;
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
			return Task.FromResult(Directory.Exists(directoryPath));
		}

		public Task<bool> IsFileExists(string filePath) {
			return Task.FromResult(File.Exists(filePath));
		}

		public Task<byte[]> ReadAllBytes(string filePath) {
			throw new NotImplementedException();
		}
	}
}
