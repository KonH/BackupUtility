using System;

namespace BackupUtility.Core.Models {
	class BackupFileResult {
		public string    SourcePath { get; } 
		public string    DestPath   { get; }
		public Exception Exception  { get; }

		public bool Success => (Exception == null);

		public BackupFileResult(string sourcePath, string destPath, Exception e = null) {
			SourcePath = sourcePath;
			DestPath   = destPath;
			Exception  = e;
		}

		public override string ToString() {
			return $"'{SourcePath}' => '{DestPath}': {Exception}";
		}
	}
}
