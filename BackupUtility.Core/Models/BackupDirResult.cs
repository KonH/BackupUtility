using System.Linq;
using System.Collections.Generic;

namespace BackupUtility.Core.Models {
	class BackupDirResult {
		public string                 SourcePath { get; }
		public string                 DestPath   { get; }
		public List<BackupFileResult> Results    { get; }

		public int TotalResults   => Results.Count;
		public int SuccessResults => Results.Count(r => r.Success);
		public int FailedResults  => Results.Count(r => !r.Success);

		public BackupDirResult(string sourcePath, string destPath, List<BackupFileResult> results) {
			SourcePath = sourcePath;
			DestPath   = destPath;
			Results    = results;
		}

		public override string ToString() {
			return $"'{SourcePath}' => '{DestPath}': {SuccessResults}/{TotalResults}";
		}
	}
}
