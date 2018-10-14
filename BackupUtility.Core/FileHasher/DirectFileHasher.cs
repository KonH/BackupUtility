using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using BackupUtility.Core.FileManager;

namespace BackupUtility.Core.FileHasher {
	public class DirectFileHasher : IFileHasher {
		readonly IFileManager _fs;

		public DirectFileHasher(IFileManager fs) {
			_fs = fs;
		}

		public async Task<string> GetFileHash(string filePath) {
			if ( await _fs.IsFileExists(filePath) ) {
				var contents = await _fs.ReadAllBytes(filePath);
				return GetFileHash(contents);
			}
			return string.Empty;
		}

		public string GetFileHash(byte[] fileContent) {
			using ( var md5 = MD5.Create() ) {
				return Convert.ToBase64String(md5.ComputeHash(fileContent));
			}
		}

		public void ResetFileHash(string filePath) { }
		public Task Load() => Task.CompletedTask;
		public Task Save(bool force, int processedFiles) => Task.CompletedTask;
	}
}
