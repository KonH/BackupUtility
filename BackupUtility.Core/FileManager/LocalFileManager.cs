using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BackupUtility.Core.FileManager {
	public class LocalFileManager : IFileManager {
		public string CombinePath(params string[] parts) {
			return Path.Combine(parts);
		}

		public string GetDirectoryName(string fullPath) {
			var info = new DirectoryInfo(fullPath);
			return info.Name;
		}

		public Task CopyFile(string fromFilePath, string toFilePath) {
			File.Copy(fromFilePath, toFilePath, true);
			return Task.CompletedTask;
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
			File.Delete(filePath);
			return Task.CompletedTask;
		}

		public Task<IEnumerable<string>> GetDirectories(string directoryPath) {
			var fullPathes = Directory.GetDirectories(directoryPath);
			var localPathes = fullPathes.Select(it => Path.GetFileName(it));
			return Task.FromResult(localPathes);
		}

		public Task<IEnumerable<string>> GetFiles(string directoryPath) {
			var fullPathes = Directory.GetFiles(directoryPath);
			var localPathes = fullPathes.Select(it => Path.GetFileName(it));
			return Task.FromResult(localPathes);
		}

		public Task<bool> IsDirectoryExists(string directoryPath) {
			return Task.FromResult(Directory.Exists(directoryPath));
		}

		public Task<bool> IsFileExists(string filePath) {
			return Task.FromResult(File.Exists(filePath));
		}

		public Task<byte[]> ReadAllBytes(string filePath) {
			return File.ReadAllBytesAsync(filePath);
		}
	}
}
