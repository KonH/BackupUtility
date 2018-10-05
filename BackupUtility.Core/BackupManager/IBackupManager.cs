using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackupUtility.Core.BackupManager {
	public interface IBackupManager {
		Task Dump(IEnumerable<string> sourceDirs, string backupDir);
	}
}
