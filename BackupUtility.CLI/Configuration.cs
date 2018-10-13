using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace BackupUtility.CLI {
	enum LogType {
		Console,
		File,
	}

	class LoggingOptions {
		public LogType  Type  { get; set; }
		public LogLevel Level { get; set; }
	}

	class SftpOptions {
		public string Host     { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
	}

	enum FileSystemMode {
		Local,
		SFTP,
	}

	class FileSystemOptions {
		public string         Path { get; set; }
		public FileSystemMode Mode { get; set; }
		public string         Host { get; set; }
	}

	class BackupOptions {
		public FileSystemOptions From { get; set; }
		public FileSystemOptions To   { get; set; }
	}

	class BackupUtilityConfiguration {
		public List<LoggingOptions>            Logging { get; set; } = new List<LoggingOptions>();
		public Dictionary<string, SftpOptions> Sftp    { get; set; } = new Dictionary<string, SftpOptions>();
		public List<BackupOptions>             Backup  { get; set; } = new List<BackupOptions>();
	}
}
