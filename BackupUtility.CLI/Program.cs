namespace BackupUtility.CLI {
	class Program {
		static void Main(string[] args) {
			var runner = new BackupUtilityRunner();
			runner.EntryPoint();
		}
	}
}
