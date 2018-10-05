using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackupUtility.Core.FileManager {
	public interface IFileManager {
		string CombinePath(params string[] parts);
		string GetDirectoryName(string fullPath);
		Task<bool> IsFileExists(string filePath);
		Task<byte[]> ReadAllBytes(string filePath);
		Task CreateFile(string filePath, byte[] bytes);
		Task CopyFile(string fromFilePath, string toFilePath);
		Task DeleteFile(string filePath);
		Task<bool> IsDirectoryExists(string directoryPath);
		Task<IEnumerable<string>> GetFiles(string directoryPath);
		Task<IEnumerable<string>> GetDirectories(string directoryPath);
		Task CreateDirectory(string directoryPath);
		Task DeleteDirectory(string directoryPath);
	}
}
