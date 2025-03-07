using System.Diagnostics;
using System.Globalization;

namespace FamiSharp.Utilities
{
	public sealed class ApplicationInfo(string name, string ver, DateTime bt, string desc, string cpr)
	{
		public string Name { get; } = name;
		public string Version { get; } = ver;
		public DateTime BuildTime { get; } = bt;
		public string Description { get; } = desc;
		public string Copyright { get; } = cpr;

		public static ApplicationInfo GetApplicationInfo()
		{
			if (string.IsNullOrEmpty(Environment.ProcessPath)) return new("Application Name", "0.0.0.0", new(0), "No description.", "No copyright.");
			var fileVersionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
			var productVersion = fileVersionInfo.ProductVersion!;
			DateTime.TryParseExact(productVersion[(productVersion.LastIndexOf('.') + 1)..], "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime buildTime);
			return new ApplicationInfo(fileVersionInfo.ProductName!, fileVersionInfo.ProductVersion!, buildTime, fileVersionInfo.Comments!, fileVersionInfo.LegalCopyright!);
		}
	}
}
