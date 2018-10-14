using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace BackupUtility.Core.FileManager {
	public class SftpFileManager : IFileManager {
		readonly ILogger<SftpFileManager> _logger;
		readonly SftpClient               _client;
		readonly ConnectionInfo           _connectionInfo;

		public SftpFileManager(string host, string userName, string password, ILogger<SftpFileManager> logger) {
			var authMethod = new PasswordAuthenticationMethod(userName, password);
			_connectionInfo = new ConnectionInfo(host, userName, authMethod);
			_client = new SftpClient(_connectionInfo);
			_logger = logger;
		}

		public void Connect() {
			_logger?.LogDebug("Connect");
			if ( _client.IsConnected ) {
				_logger?.LogDebug("Connect: already connected");
				return;
			}
			_client.Connect();
			_logger?.LogDebug($"Connect: isConnected: {_client.IsConnected}");
		}

		public void Disconnect() {
			_logger?.LogDebug("Disconnect");
			if ( !_client.IsConnected ) {
				_logger?.LogDebug("Connect: already disconnected");
				return;
			}
			_client.Disconnect();
			_logger?.LogDebug($"Disconnect: isConnected: {_client.IsConnected}");
		}

		public string CombinePath(params string[] parts) {
			return string.Join('/', parts);
		}

		public Task CopyFile(string fromFilePath, string toFilePath) {
			return HandleCommonExceptions(async () => {
				_logger?.LogDebug($"CopyFile('{fromFilePath}', '{toFilePath}')");
				var data = await ReadAllBytes(fromFilePath);
				await CreateFile(toFilePath, data);
				_logger?.LogDebug($"CopyFile('{fromFilePath}', '{toFilePath}'): done");
			});
		}

		public Task CreateDirectory(string directoryPath) {
			return HandleCommonExceptions(() => {
				_logger?.LogDebug($"CreateDirectory('{directoryPath}')");
				_client.CreateDirectory(directoryPath);
				_logger?.LogDebug($"CreateDirectory('{directoryPath}'): done");
			});
		}

		public Task CreateFile(string filePath, byte[] bytes) {
			return HandleCommonExceptions(() => {
				_logger?.LogDebug($"CreateFile('{filePath}')");
				using ( var stream = _client.Create(filePath) ) {
					stream.Write(bytes);
				}
				_logger?.LogDebug($"CreateFile('{filePath}'): done");
			});
		}

		public Task DeleteDirectory(string directoryPath) {
			return HandleCommonExceptions(async () => {
				_logger?.LogDebug($"DeleteDirectory('{directoryPath}')");
				var files = await GetFiles(directoryPath);
				var fileRmReqs = files.Select(file => DeleteFile(CombinePath(directoryPath, file)));
				await Task.WhenAll(fileRmReqs);
				var dirs = await GetDirectories(directoryPath);
				var rmDirReqs = dirs.Select(dir => DeleteDirectory(CombinePath(directoryPath, dir)));
				await Task.WhenAll(rmDirReqs);
				_client.DeleteDirectory(directoryPath);
				_logger?.LogDebug($"DeleteDirectory('{directoryPath}'): done");
			});
		}

		public Task DeleteFile(string filePath) {
			return HandleCommonExceptions(() => {
				_logger?.LogDebug($"DeleteFile('{filePath}')");
				_client.DeleteFile(filePath);
				_logger?.LogDebug($"DeleteFile('{filePath}'): done");
			});
		}

		public Task<IEnumerable<string>> GetDirectories(string directoryPath) {
			return HandleCommonExceptions(() => {
				_logger?.LogDebug($"GetDirectories('{directoryPath}')");
				var allFiles = _client.ListDirectory(directoryPath);
				var dirs = allFiles
					.Where(file => file.IsDirectory)
					.Select(file => file.Name)
					.Where(name => !IsSpecificDirectory(name));
				_logger?.LogDebug($"GetDirectories('{directoryPath}'): {dirs.Count()} dirs");
				return dirs;
			});
		}

		public string GetDirectoryName(string fullPath) {
			var index = fullPath.LastIndexOf('/');
			if ( index > 0 ) {
				return fullPath.Substring(index + 1);
			}
			return fullPath;
		}

		public Task<DateTime> GetFileChangeTime(string filePath) {
			return HandleCommonExceptions(() => {
				return _client.GetLastWriteTimeUtc(filePath);
			});
		}

		public Task<IEnumerable<string>> GetFiles(string directoryPath) {
			return HandleCommonExceptions(() => {
				_logger?.LogDebug($"GetFiles('{directoryPath}')");
				var allFiles = _client.ListDirectory(directoryPath);
				var dirs = allFiles
					.Where(file => file.IsRegularFile)
					.Select(file => file.Name);
				_logger?.LogDebug($"GetFiles('{directoryPath}'): {dirs.Count()} dirs");
				return dirs;
			});
		}

		public Task<bool> IsDirectoryExists(string directoryPath) {
			return HandleCommonExceptions(() => {
				if ( _client.Exists(directoryPath) ) {
					var file = _client.Get(directoryPath);
					return file.IsDirectory;
				}
				return false;
			});
		}

		public Task<bool> IsFileExists(string filePath) {
			return HandleCommonExceptions(() => {
				if ( _client.Exists(filePath) ) {
					var file = _client.Get(filePath);
					return file.IsRegularFile;
				}
				return false;
			});
		}

		public Task<byte[]> ReadAllBytes(string filePath) {
			return HandleCommonExceptions(() => {
				_logger?.LogDebug($"ReadAllBytes('{filePath}')");
				var data = _client.ReadAllBytes(filePath);
				_logger?.LogDebug($"ReadAllBytes('{filePath}'): read {data.Length} bytes");
				return data;
			});
		}

		bool IsSpecificDirectory(string dir) {
			return (dir == ".") || (dir == "..");
		}

		Task HandleCommonExceptions(Action action) {
			return Task.Run(() => {
				try {
					action();
				} catch ( SshConnectionException ) {
					Connect();
					action();
				}
			});
		}

		Task HandleCommonExceptions(Func<Task> action) {
			return Task.Run(async () => {
				try {
					await action();
				} catch ( SshConnectionException ) {
					Connect();
					await action();
				}
			});
		}

		Task<T> HandleCommonExceptions<T>(Func<T> action) {
			return Task.Run(() => {
				try {
					return action();
				} catch ( SshConnectionException ) {
					Connect();
					return action();
				}
			});
		}

		Task<T> HandleCommonExceptions<T>(Func<Task<T>> action) {
			return Task.Run(async () => {
				try {
					return await action();
				} catch ( SshConnectionException ) {
					Connect();
					return await action();
				}
			});
		}
	}
}
