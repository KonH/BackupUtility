using System;
using System.Linq;
using System.Collections.Generic;

namespace BackupUtility.Core.Models {
	public class BackupDirResult {
		public string                 SourcePath { get; }
		public string                 DestPath   { get; }
		public List<BackupFileResult> Results    { get; }
		public TimeSpan               Duration   { get; }

		public int TotalResults   => Results.Count;
		public int SuccessResults => Results.Count(r => r.Success);
		public int FailedResults  => Results.Count(r => !r.Success);
		public int SkippedResults => Results.Count(r => r.Skipped);

		public BackupDirResult(string sourcePath, string destPath, List<BackupFileResult> results, TimeSpan duration) {
			SourcePath = sourcePath;
			DestPath   = destPath;
			Results    = results;
			Duration   = duration;
		}

		public override string ToString() {
			return $"'{SourcePath}' => '{DestPath}': total: {TotalResults}, failed: {FailedResults}, skipped: {SkippedResults}, duration: {Duration}";
		}
	}
}
