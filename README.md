# BackupUtility

Utility to backup files using .NET core.

Features:
- Save last 3 copies of each file in directory beside
- SFTP support using SSH.NET
- Simple JSON file based configuration
- Skip non-changed files
- Fast implementation based on async operations (partially)

Limitations and ways of extension:
- Support only password auth for SFTP
- Don't support ignore lists
- Not designed to backup really large files, can raise out of memory
- Need resource utilization controls
- Need more extended cache
