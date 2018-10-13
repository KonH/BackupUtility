using System;

namespace BackupUtility.Core.Models {
	public class BackupFileResult {
		public string    SourcePath { get; } 
		public string    DestPath   { get; }
		public bool      Skipped    { get; }
		public Exception Exception  { get; }

		public bool Success => (Exception == null);

		public BackupFileResult(string sourcePath, string destPath, bool skipped = false, Exception exception = null) {
			SourcePath = sourcePath;
			DestPath   = destPath;
			Skipped    = skipped;
			Exception  = exception;
		}

		public override string ToString() {
			return $"'{SourcePath}' => '{DestPath}': {Exception}";
		}
	}
}
