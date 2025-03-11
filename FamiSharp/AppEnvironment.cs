using FamiSharp.Utilities;

namespace FamiSharp
{
	public sealed class AppEnvironment
	{
		const string jsonConfigFileName = "Config.json";
		const string saveDataDirectoryName = "Saves";

		public readonly static ApplicationInfo ApplicationInfo = ApplicationInfo.GetApplicationInfo();

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ApplicationInfo.Name);

		public readonly static string ConfigurationFilename = Path.Combine(programDataDirectory, jsonConfigFileName);
		public readonly static string SaveDataPath = string.Empty;

		public readonly static Configuration Configuration = Configuration.LoadFromFile(ConfigurationFilename);

		static AppEnvironment()
		{
			Directory.CreateDirectory(SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName));
		}
	}
}
