using System.Threading.Tasks;
using BackupUtility.Core.FileHasher;

namespace BackupUtility.Core.Extensions {
	public class FileChangeValidator {
		readonly IFileHasher _source;
		readonly IFileHasher _destination;

		public FileChangeValidator(IFileHasher source, IFileHasher destination) {
			_source      = source;
			_destination = destination;
		}

		public async Task<bool> IsFileChanged(byte[] sourceContent, string destinationFilePath) {
			var sourceHash = _source.GetFileHash(sourceContent);
			var destinationHash = await _destination.GetFileHash(destinationFilePath);
			return sourceHash != destinationHash;
		}

		public void OnFileChanged(string destinationFilePath) {
			_destination.ResetFileHash(destinationFilePath);
		}

		public async Task Load() {
			await _destination.Load();
		}

		public async Task Save(bool force, int processedFiles) {
			await _destination.Save(force, processedFiles);
		}
	}
}
