using Microsoft.Extensions.Logging;
using BackupUtility.Core.Extensions;
using BackupUtility.Core.FileManager;

namespace BackupUtility.CLI {
	class BackupTask {
		public string              SourceDir       { get; }
		public string              DestinationDir  { get; }
		public IFileManager        SourceFs        { get; }
		public IFileManager        DestinationFs   { get; }
		public HistoryProvider     HistoryProvider { get; }
		public FileChangeValidator ChangeValidator { get; }

		public BackupTask
		(
			string              sourceDir,
			string              destinationDir,
			IFileManager        sourceFs,
			IFileManager        destinationFs,
			HistoryProvider     historyProvider,
			FileChangeValidator changeValidator
		) {
			SourceDir       = sourceDir;
			DestinationDir  = destinationDir;
			SourceFs        = sourceFs;
			DestinationFs   = destinationFs;
			HistoryProvider = historyProvider;
			ChangeValidator = changeValidator;
		}
	}
}
