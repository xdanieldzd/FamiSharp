using System.Diagnostics;

namespace FamiSharp.Utilities
{
	public sealed class ProductInformation(string name, string ver, string desc, string cpr)
	{
		public string Name { get; } = name;
		public string Version { get; } = ver;
		public string Description { get; } = desc;
		public string Copyright { get; } = cpr;

		internal static ProductInformation GetProductInfo()
		{
			if (string.IsNullOrEmpty(Environment.ProcessPath)) return new("Application Name", "0.0.0.0", "No description.", "No copyright.");
			var fileVersionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
			return new ProductInformation(fileVersionInfo.ProductName!, fileVersionInfo.ProductVersion!, fileVersionInfo.Comments!, fileVersionInfo.LegalCopyright!);
		}
	}
}
