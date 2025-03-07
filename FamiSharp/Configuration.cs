using System.Text.Json;
using System.Text.Json.Serialization;
using SDLKeyCode = Hexa.NET.SDL2.SDLKeyCode;

namespace FamiSharp
{
	public sealed class Configuration
	{
		public string LastRomLoaded { get; set; } = string.Empty;
		public bool LimitFps { get; set; } = true;
		public int DisplaySize { get; set; } = 2;

		public ControllerConfiguration Controller1 { get; set; } = new(SDLKeyCode.Right, SDLKeyCode.Left, SDLKeyCode.Down, SDLKeyCode.Up, SDLKeyCode.Return, SDLKeyCode.Space, SDLKeyCode.A, SDLKeyCode.S);
		public ControllerConfiguration Controller2 { get; set; } = new(SDLKeyCode.Kp6, SDLKeyCode.Kp4, SDLKeyCode.Kp2, SDLKeyCode.Kp8, SDLKeyCode.Return2, SDLKeyCode.Kp0, SDLKeyCode.Kp1, SDLKeyCode.Kp3);

		public static Configuration LoadFromFile(string filename)
		{
			var directory = Path.GetDirectoryName(filename);
			if (directory != null) Directory.CreateDirectory(directory);

			Configuration? configuration;
			if (!File.Exists(filename) || (configuration = JsonSerializer.Deserialize(File.ReadAllText(filename), SourceGenerationContext.Default.Configuration)) == null)
			{
				configuration = new();
				File.WriteAllText(filename, JsonSerializer.Serialize(configuration, SourceGenerationContext.Default.Configuration));
			}

			return configuration;
		}

		public void SaveToFile(string filename)
		{
			File.WriteAllText(filename, JsonSerializer.Serialize(this, SourceGenerationContext.Default.Configuration));
		}
	}

	public sealed class ControllerConfiguration(SDLKeyCode right, SDLKeyCode left, SDLKeyCode down, SDLKeyCode up, SDLKeyCode start, SDLKeyCode select, SDLKeyCode b, SDLKeyCode a)
	{
		public SDLKeyCode Right { get; set; } = right;
		public SDLKeyCode Left { get; set; } = left;
		public SDLKeyCode Down { get; set; } = down;
		public SDLKeyCode Up { get; set; } = up;
		public SDLKeyCode Start { get; set; } = start;
		public SDLKeyCode Select { get; set; } = select;
		public SDLKeyCode B { get; set; } = b;
		public SDLKeyCode A { get; set; } = a;
	}

	[JsonSourceGenerationOptions(WriteIndented = true)]
	[JsonSerializable(typeof(Configuration))]
	internal partial class SourceGenerationContext : JsonSerializerContext { }
}
