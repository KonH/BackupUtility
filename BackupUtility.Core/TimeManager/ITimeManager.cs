using System;

namespace BackupUtility.Core.TimeManager {
	public interface ITimeManager {
		DateTime CurrentTime { get; }
	}
}
