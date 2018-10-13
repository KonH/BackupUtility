using System;

namespace BackupUtility.Core.Models {
	public class BackupProgress {
		public long     Bytes   { get; private set; }
		public int      Files   { get; private set; }
		public TimeSpan Elapsed { get; private set; }
		public bool     Done    { get; private set; }

		public void Advance(long bytes, TimeSpan elapsed) {
			Bytes += bytes;
			Files++;
			Elapsed = elapsed;
		}

		public void Finish() {
			Done = true;
		}
	}
}
