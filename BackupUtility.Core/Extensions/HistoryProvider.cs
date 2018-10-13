using BackupUtility.Core.TimeManager;
using System;

namespace BackupUtility.Core.Extensions {
	public class HistoryProvider {
		public int Depth { get; }

		readonly ITimeManager                   _time;
		readonly Func<string, string>           _dirConverter;
		readonly Func<string, DateTime, string> _fileConverter;

		/// <summary>
		/// Initialize history provider which allows to store several copies of file
		/// </summary>
		/// <param name="fileNameToDirectoryConverter">
		/// How to convert input file name to backup directory, when several copies is required
		/// </param>
		/// <param name="fileNameToVersionedFileName">
		/// How to convert input file name to file name inside history directory
		/// </param>
		/// <param name="historyDepth">
		/// How many copies we need to store
		/// </param>
		public HistoryProvider(
			ITimeManager                   time,
			Func<string, string>           fileNameToDirectoryConverter,
			Func<string, DateTime, string> fileNameToVersionedFileName,
			int                            historyDepth
		) {
			_time          = time;
			_dirConverter  = fileNameToDirectoryConverter;
			_fileConverter = fileNameToVersionedFileName;
			Depth          = historyDepth;
		}

		public string ConvertFileNameToHistoryDirectoryName(string name) {
			return _dirConverter(name);
		}

		public string ConvertFileNameToVersionedFileName(string name) {
			return _fileConverter(name, _time.CurrentTime);
		}
	}

	public class DefaultHistoryProvider : HistoryProvider {
		public DefaultHistoryProvider(ITimeManager time, int depth) : base(
			time,
			(name)     => $".{name}.history",
			(name, dt) => $"{new DateTimeOffset(dt).ToUnixTimeSeconds()}.{name}",
			depth
		) { }
	}
}
