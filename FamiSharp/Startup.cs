namespace FamiSharp
{
	public static class Startup
	{
		static void Main() => new Emulator() { EscToExit = true }.Run();
	}
}
