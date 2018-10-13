using System;
using BackupUtility.Core.TimeManager;

namespace BackupUtility.Tests.Mocks {
	class MockTimeManager : ITimeManager {
		public DateTime CurrentTime { get; private set; }

		public MockTimeManager(DateTime startTime) {
			CurrentTime = startTime;
		}

		public void Advance(TimeSpan span) {
			CurrentTime.Add(span);
		}
	}
}
