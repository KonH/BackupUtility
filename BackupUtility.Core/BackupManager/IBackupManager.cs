using System.Threading.Tasks;
using BackupUtility.Core.Models;

namespace BackupUtility.Core.BackupManager {
	public interface IBackupManager {
		Task<BackupDirResult> Dump(string sourceDir, string backupDir);
		BackupProgress GetLatestProgress();
	}
}
