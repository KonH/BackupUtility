using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using BackupUtility.Core.FileManager;

namespace BackupUtility.Core.FileHasher {
	public class CachedFileHasher : IFileHasher {
		readonly IFileHasher  _origin;
		readonly IFileManager _fs;
		readonly string       _cachePath;
		readonly int          _processedFilesRange;
		readonly bool         _checkChangeTime;

		int  _curProcessedFiles = 0;
		int  _loadedCount       = 0;
		bool _saveInProgress    = false;

		Dictionary<string, string> _cache = new Dictionary<string, string>();

		public CachedFileHasher(IFileHasher origin, IFileManager fs, string cachePath, int procesedFilesRange, bool checkChangeTime) {
			_origin              = origin;
			_fs                  = fs;
			_cachePath           = cachePath;
			_processedFilesRange = procesedFilesRange;
			_checkChangeTime     = checkChangeTime;
		}

		public async Task<string> GetFileHash(string filePath) {
			if ( string.IsNullOrEmpty(filePath) ) {
				return string.Empty;
			}
			if ( _cache.TryGetValue(filePath, out var savedHash) ) {
				return savedHash;
			}
			var originCache = await _origin.GetFileHash(filePath);
			_cache.TryAdd(filePath, originCache);
			return originCache;
		}

		public string GetFileHash(byte[] fileContent) {
			return _origin.GetFileHash(fileContent);
		}

		public void ResetFileHash(string filePath) {
			if ( !string.IsNullOrEmpty(filePath) ) {
				_cache.Remove(filePath);
			}
		}

		public async Task Load() {
			if ( await _fs.IsFileExists(_cachePath) ) {
				var bytes = await _fs.ReadAllBytes(_cachePath);
				var text = Encoding.UTF8.GetString(bytes);
				var lines = text.Split(Environment.NewLine);
				foreach ( var line in lines ) {
					if ( string.IsNullOrWhiteSpace(line) ) {
						continue;
					}
					var parts = line.Split('|');
					if ( parts.Length == 2 ) {
						var key = parts[0];
						var value = parts[1];
						_cache.Add(key, value);
					}
				}
				_loadedCount = _cache.Count;
				if ( _checkChangeTime ) {
					var lastCacheChangeTime = await _fs.GetFileChangeTime(_cachePath);
					var cacheFiles = new List<string>(_cache.Keys);
					foreach ( var file in cacheFiles ) {
						if ( await _fs.GetFileChangeTime(file) > lastCacheChangeTime ) {
							ResetFileHash(file);
						}
					}
				}
			}
		}

		public async Task Save(bool force, int processedFiles) {
			if ( !force ) {
				if ( processedFiles > _curProcessedFiles + _processedFilesRange) {
					_curProcessedFiles = processedFiles;
				} else {
					return;
				}
				if ( _cache.Count == _loadedCount ) {
					return;
				}
			}
			if ( _saveInProgress ) {
				return;
			}
			_saveInProgress = true;
			var text = new StringBuilder();
			var cacheCopy = new Dictionary<string, string>(_cache);
			foreach ( var pair in cacheCopy ) {
				text = text
					.Append(pair.Key)
					.Append('|')
					.Append(pair.Value)
					.Append(Environment.NewLine);
			}
			if ( await _fs.IsFileExists(_cachePath) ) {
				await _fs.DeleteFile(_cachePath);
			}
			var str = text.ToString();
			var bytes = Encoding.UTF8.GetBytes(str);
			await _fs.CreateFile(_cachePath, bytes);
			_loadedCount = cacheCopy.Count;
			_saveInProgress = false;
		}
	}
}
