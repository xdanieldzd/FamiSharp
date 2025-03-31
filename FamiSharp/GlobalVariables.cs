namespace FamiSharp
{
	public static class GlobalVariables
	{
		public static readonly bool IsAuthorsMachine = Environment.MachineName == "RYO-RYZEN";
#if DEBUG
		public static readonly bool IsDebugBuild = true;
		public static readonly bool OutputApuSineTest = true;
#else
		public static readonly bool IsDebugBuild;
		public static readonly bool OutputApuSineTest;
#endif
	}
}
