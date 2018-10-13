using System.Threading.Tasks;

namespace BackupUtility.Core.BackupManager {
	public interface IBackupManager {
		Task Dump(string sourceDir, string backupDir);
	}
}
