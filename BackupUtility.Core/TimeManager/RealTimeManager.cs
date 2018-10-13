using System;

namespace BackupUtility.Core.TimeManager {
	public class RealTimeManager : ITimeManager {
		public DateTime CurrentTime => DateTime.Now;
	}
}
