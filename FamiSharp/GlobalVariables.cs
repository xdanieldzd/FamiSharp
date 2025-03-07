namespace FamiSharp
{
	public static class GlobalVariables
	{
		public static readonly bool IsAuthorsMachine = Environment.MachineName == "RYO-RYZEN";
#if DEBUG
		public static readonly bool IsDebugBuild = true;
#else
		public static readonly bool IsDebugBuild = false;
#endif
	}
}
