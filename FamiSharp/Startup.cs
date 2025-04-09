using SDLWindowFlags = Hexa.NET.SDL2.SDLWindowFlags;

namespace FamiSharp
{
	public static class Startup
	{
		static void Main()
		{
			var emulator = new Emulator(new()
			{
				Title = nameof(FamiSharp),
				ClientSize = (1280, 720),
				Icon = Resources.GetEmbeddedRgbaFile("Assets.FC-Icon.rgba"),
				WindowFlags = SDLWindowFlags.Opengl | SDLWindowFlags.Resizable,
				EscToExit = true,
				OpenGLVersion = new(3, 3),
				VSync = VSyncMode.Off,
				ClearColor = new(0x3E / 255f, 0x4F / 255f, 0x65 / 255f) /* ❤️ 🧲 ❤️ */
			});
			emulator.Run();
		}
	}
}
