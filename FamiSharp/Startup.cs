global using System.Numerics;

namespace FamiSharp
{
	public static class Startup
	{
		static void Main()
		{
			var emulator = new Emulator();
			emulator.Run();
		}
	}
}
