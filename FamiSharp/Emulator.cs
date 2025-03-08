using FamiSharp.Emulation;
using FamiSharp.Emulation.Cartridges;
using FamiSharp.UserInterface;
using NativeFileDialogNET;
using SDLKeyCode = Hexa.NET.SDL2.SDLKeyCode;

namespace FamiSharp
{
	public partial class Emulator : Application
	{
		readonly NesSystem nes;
		readonly OpenGLTexture displayTexture;
		readonly bool[,] buttonsDown = new bool[2, 8];

		string cartridgeFilename = string.Empty, cartSaveFilename = string.Empty;
		bool isSystemRunning = false, isEmulationPaused = false;

		double frameTimeElapsed = 0.0, framesPerSecond = 0.0;

		public Emulator() : base($"{AppEnvironment.ApplicationInfo.Name} v{AppEnvironment.ApplicationInfo.Version}", 1280, 720)
		{
			BackgroundColor = new(0x3E / 255.0f, 0x4F / 255.0f, 0x65 / 255.0f); /* ❤️ 🧲 ❤️ */

			InitializeUI();

			nes = new();
			nes.Ppu.LoadPalette(File.ReadAllBytes(@"Assets\2C02G_wiki.pal"));
			nes.Ppu.TransferFramebuffer += (s, e) =>
			{
				displayTexture?.Update(e.Data);
			};
			nes.RequestInput += (s, e) =>
			{
				if (!displayWindow.IsFocused) return;

				for (var ctrl = 0; ctrl < buttonsDown.GetLength(0); ctrl++)
				{
					for (var btn = 0; btn < buttonsDown.GetLength(1); btn++)
					{
						if (buttonsDown[ctrl, btn])
							e.ControllerData[ctrl] |= (byte)(1 << btn);
						else
							e.ControllerData[ctrl] &= (byte)~(1 << btn);
					}
				}
			};

			displayTexture = new(256, 240);

			if (GlobalVariables.IsAuthorsMachine && GlobalVariables.IsDebugBuild)
				LoadAndRunCartridge(AppEnvironment.Configuration.LastRomLoaded);
		}

		public override void OnKeyDown(KeycodeEventArgs e)
		{
			if (HandleMenuShortcuts(e))
				return;

			switch (e.Keycode)
			{
				case SDLKeyCode.Escape: Exit(); break;

				case var value when value == AppEnvironment.Configuration.Controller1.Right: buttonsDown[0, 0] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Left: buttonsDown[0, 1] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Down: buttonsDown[0, 2] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Up: buttonsDown[0, 3] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Start: buttonsDown[0, 4] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Select: buttonsDown[0, 5] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller1.B: buttonsDown[0, 6] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller1.A: buttonsDown[0, 7] = true; break;

				case var value when value == AppEnvironment.Configuration.Controller2.Right: buttonsDown[1, 0] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Left: buttonsDown[1, 1] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Down: buttonsDown[1, 2] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Up: buttonsDown[1, 3] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Start: buttonsDown[1, 4] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Select: buttonsDown[1, 5] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller2.B: buttonsDown[1, 6] = true; break;
				case var value when value == AppEnvironment.Configuration.Controller2.A: buttonsDown[1, 7] = true; break;
			}
		}

		public override void OnKeyUp(KeycodeEventArgs e)
		{
			switch (e.Keycode)
			{
				case var value when value == AppEnvironment.Configuration.Controller1.Right: buttonsDown[0, 0] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Left: buttonsDown[0, 1] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Down: buttonsDown[0, 2] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Up: buttonsDown[0, 3] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Start: buttonsDown[0, 4] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller1.Select: buttonsDown[0, 5] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller1.B: buttonsDown[0, 6] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller1.A: buttonsDown[0, 7] = false; break;

				case var value when value == AppEnvironment.Configuration.Controller2.Right: buttonsDown[1, 0] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Left: buttonsDown[1, 1] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Down: buttonsDown[1, 2] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Up: buttonsDown[1, 3] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Start: buttonsDown[1, 4] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller2.Select: buttonsDown[1, 5] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller2.B: buttonsDown[1, 6] = false; break;
				case var value when value == AppEnvironment.Configuration.Controller2.A: buttonsDown[1, 7] = false; break;
			}
		}

		public override void OnUpdate(DeltaTimeEventArgs e)
		{
			frameTimeElapsed += e.Delta;

			if (frameTimeElapsed >= 1.0 / 60.0988 || !AppEnvironment.Configuration.LimitFps)
			{
				if (isSystemRunning && !isEmulationPaused)
				{
					while (!nes.Tick()) { }
					do { nes.Tick(); } while (nes.Cpu.Cycles != 0);

					framesPerSecond = 1.0 / frameTimeElapsed;
				}
				frameTimeElapsed = 0.0;
			}

			if (fpsStatusBarItem != null)
			{
				fpsStatusBarItem.Label =
					$"{(isSystemRunning ? (isEmulationPaused ? "Paused" : $"{framesPerSecond:0} FPS") : "Stopped")}";

				fpsStatusBarItem.ToolTip =
					$"Emulator: {framesPerSecond,8:0.00} FPS" + Environment.NewLine +
					$"GUI:      {GuiFramerate,8:0.00} FPS" + Environment.NewLine +
					$"Emulation is {(isSystemRunning ? (isEmulationPaused ? "paused" : "running") : "stopped")}";
			}
		}

		public override void OnRenderGUI(DeltaTimeEventArgs e)
		{
			MainMenu.Draw(new MainMenuItem?[] { fileMenuItem, emulationMenuItem, debugMenuItem, optionsMenuItem, helpMenuItem });
			StatusBar.Draw(new StatusBarItem?[] { statusStatusBarItem, fpsStatusBarItem });

			displayWindow.Draw(displayTexture);
			aboutWindow.Draw(AppEnvironment.ApplicationInfo);

			cpuStatusWindow.Draw(nes);
			cpuDisassemblyWindow.Draw(nes);
			patternTableWindow.Draw(nes);

			if (GlobalVariables.IsAuthorsMachine)
			{
				cpuStatusWindow.IsWindowOpen = true;
				cpuDisassemblyWindow.IsWindowOpen = true;
				patternTableWindow.IsWindowOpen = true;
				if (GlobalVariables.IsDebugBuild)
					Hexa.NET.ImGui.ImGui.ShowDemoWindow();
			}
		}

		public override void OnShutdown()
		{
			SaveCartridgeRam();

			AppEnvironment.Configuration.DisplaySize = displayWindow.WindowScale;
			AppEnvironment.Configuration.SaveToFile(AppEnvironment.ConfigurationFilename);
		}

		private void ShowOpenRomDialog()
		{
			var (lastRomDirectory, lastRomFilename) = (string.Empty, string.Empty);
			if (!string.IsNullOrEmpty(AppEnvironment.Configuration.LastRomLoaded))
			{
				lastRomDirectory = Path.GetDirectoryName(AppEnvironment.Configuration.LastRomLoaded);
				lastRomFilename = Path.GetFileName(AppEnvironment.Configuration.LastRomLoaded);
			}
			if (openRomDialog.Open(out string? filename, lastRomDirectory, lastRomFilename) == DialogResult.Okay && filename != null)
			{
				LoadAndRunCartridge(filename);
				displayWindow.IsFocused = true;
			}
		}

		private void LoadAndRunCartridge(string filename)
		{
			if (nes == null || string.IsNullOrEmpty(filename)) return;

			if (isSystemRunning)
			{
				isSystemRunning = false;
				SaveCartridgeRam();
			}

			cartridgeFilename = filename;
			cartSaveFilename = $"{Path.GetFileNameWithoutExtension(cartridgeFilename)}.sav";

			using var stream = new FileStream(cartridgeFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			nes.LoadCartridge(new Cartridge(new BinaryReader(stream)));

			LoadCartridgeRam();

			nes.Reset();

			if (statusStatusBarItem != null)
				statusStatusBarItem.Label = $"Emulation started, running '{cartridgeFilename}'";

			AppEnvironment.Configuration.LastRomLoaded = cartridgeFilename;
			AppEnvironment.Configuration.SaveToFile(AppEnvironment.ConfigurationFilename);

			isSystemRunning = true;
		}

		private void StopEmulation()
		{
			if (isSystemRunning)
			{
				isSystemRunning = false;
				SaveCartridgeRam();

				displayTexture?.Clear();

				if (statusStatusBarItem != null)
					statusStatusBarItem.Label = "Emulation stopped";
			}
		}

		private void LoadCartridgeRam()
		{
			if (nes.Cartridge == null) return;

			if (nes.Cartridge.Header.HasPersistantMemory)
			{
				var savePath = Path.Combine(AppEnvironment.SaveDataPath, cartSaveFilename);

				if (!File.Exists(savePath)) return;
				var prgRam = File.ReadAllBytes(savePath);
				for (var i = 0; i < Math.Min(0x2000, prgRam.Length); i++)
					nes.Write((ushort)(0x6000 + i), prgRam[i]);
			}
		}

		private void SaveCartridgeRam()
		{
			if (nes.Cartridge == null) return;

			if (nes.Cartridge.Header.HasPersistantMemory)
			{
				if (string.IsNullOrWhiteSpace(cartSaveFilename)) return;

				var prgRam = new byte[0x2000];
				for (var i = 0; i < Math.Min(0x2000, prgRam.Length); i++)
					prgRam[i] = nes.Read((ushort)(0x6000 + i));

				var savePath = Path.Combine(AppEnvironment.SaveDataPath, cartSaveFilename);
				File.WriteAllBytes(savePath, prgRam);
			}
		}
	}
}
