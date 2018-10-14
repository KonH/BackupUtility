using System.Threading.Tasks;

namespace BackupUtility.Core.FileHasher {
	public interface IFileHasher {
		Task<string> GetFileHash(string filePath);
		string GetFileHash(byte[] fileContent);
		void ResetFileHash(string filePath);
		Task Load();
		Task Save();
	}
}
