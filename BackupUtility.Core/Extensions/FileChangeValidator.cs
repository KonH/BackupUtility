using System.Security.Cryptography;

namespace BackupUtility.Core.Extensions {
	public class FileChangeValidator {
		public bool IsFileChanged(byte[] oldContents, byte[] newContents) {
			using ( var md5 = MD5.Create() ) {
				var oldHash = md5.ComputeHash(oldContents);
				var newHash = md5.ComputeHash(newContents);
				if ( oldHash.Length != newHash.Length ) {
					return true;
				}
				for ( var i = 0; i < oldHash.Length; i++ ) {
					if ( oldHash[i] != newHash[i] ) {
						return true;
					}
				}
				return false;
			}
		}
	}
}
