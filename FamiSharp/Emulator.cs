using FamiSharp.Emulation;
using FamiSharp.Emulation.Cartridges;
using FamiSharp.UserInterface;
using FamiSharp.Utilities;
using Hexa.NET.SDL2;
using NativeFileDialogNET;

namespace FamiSharp
{
	public partial class Emulator(ApplicationSettings appSettings) : Application(appSettings)
	{
		const string configurationFilename = "Config.json";
		const string saveDataDirectoryName = "Saves";

		static readonly ProductInformation productInformation = ProductInformation.GetProductInfo();
		static readonly string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), productInformation.Name);
		static readonly string configurationPath = Path.Combine(dataDirectory, configurationFilename);

		Configuration configuration = new();
		string saveDataPath = string.Empty;

		readonly AudioHandler audioHandler = new();

		NesSystem? nes;
		OpenGLTexture? displayTexture;
		readonly bool[,] buttonsDown = new bool[2, 8];

		string cartridgeFilename = string.Empty, cartSaveFilename = string.Empty;
		bool isSystemRunning, isEmulationPaused;

		double frameTimeElapsed, framesPerSecond;
		readonly AverageFramerate averageFps = new(250);

		public override void OnLoad()
		{
			try
			{
				Title = $"{productInformation.Name} v{productInformation.Version}";

				if (!audioHandler.Initialize(2, 44100, 1024))
					FatalError($"Failed to initialize audio handler: {SDL.GetErrorS()}", -128);

				configuration = Configuration.LoadFromFile(configurationPath);
				Directory.CreateDirectory(saveDataPath = Path.Combine(dataDirectory, saveDataDirectoryName));

				nes = new(audioHandler.SampleRate);
				nes.Ppu.LoadPalette(File.ReadAllBytes(@"Assets\2C02G_wiki.pal")); /* https://www.nesdev.org/w/index.php?title=File:2C02G_wiki.pal&oldid=22304 */
				nes.Ppu.TransferFramebuffer += (s, e) => displayTexture?.Update(e.Data);
				nes.Apu.TransferSamples += (s, e) => audioHandler.Output(e.Samples);
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

				displayTexture = new(GL, 256, 240);

				if (GlobalVariables.IsAuthorsMachine)
				{
					if (GlobalVariables.IsDebugBuild)
					{
						LoadAndRunCartridge(configuration.LastRomLoaded);

						cpuStatusWindow.IsWindowOpen = true;
						cpuDisassemblyWindow.IsWindowOpen = true;
						patternTableWindow.IsWindowOpen = true;
					}
				}
			}
			catch (Exception e)
			{
				ShowMessageBox("Error", $"An error occured while starting the emulator:\n\n{e.Message}", SDLMessageBoxFlags.Error);
				Exit();
			}
		}

		public override void OnKeyDown(KeycodeEventArgs e)
		{
			if (HandleMenuShortcuts(e)) return;

			HandleControllerInput(e, 0, configuration.Controller1);
			HandleControllerInput(e, 1, configuration.Controller2);
		}

		public override void OnKeyUp(KeycodeEventArgs e)
		{
			HandleControllerInput(e, 0, configuration.Controller1);
			HandleControllerInput(e, 1, configuration.Controller2);
		}

		private void HandleControllerInput(KeycodeEventArgs e, int index, ControllerConfiguration config)
		{
			var value = e.EventType == SDLEventType.Keydown;
			switch (e.Keycode)
			{
				case var key when key == config.Right: buttonsDown[index, 0] = value; break;
				case var key when key == config.Left: buttonsDown[index, 1] = value; break;
				case var key when key == config.Down: buttonsDown[index, 2] = value; break;
				case var key when key == config.Up: buttonsDown[index, 3] = value; break;
				case var key when key == config.Start: buttonsDown[index, 4] = value; break;
				case var key when key == config.Select: buttonsDown[index, 5] = value; break;
				case var key when key == config.B: buttonsDown[index, 6] = value; break;
				case var key when key == config.A: buttonsDown[index, 7] = value; break;
			}
		}

		public override void OnUpdate(DeltaTimeEventArgs e)
		{
			frameTimeElapsed += e.Delta;

			if (frameTimeElapsed >= 1.0 / 60.0988 || !configuration.LimitFps)
			{
				if (isSystemRunning && !isEmulationPaused)
				{
					nes?.RunFrame();

					framesPerSecond = 1.0 / frameTimeElapsed;
					averageFps.Update(frameTimeElapsed);
				}
				frameTimeElapsed = 0.0;
			}

			if (fpsStatusBarItem != null)
			{
				fpsStatusBarItem.Label =
					$"{(isSystemRunning ? (isEmulationPaused ? "Paused" : $"{averageFps.Average:0} FPS") : "Stopped")}";

				fpsStatusBarItem.ToolTip =
					$"Emulator (avg): {averageFps.Average,8:0.00} FPS" + Environment.NewLine +
					$"Emulator:       {framesPerSecond,8:0.00} FPS" + Environment.NewLine +
					$"Application:    {Framerate,8:0.00} FPS" + Environment.NewLine +
					$"Emulation is {(isSystemRunning ? (isEmulationPaused ? "paused" : "running") : "stopped")}";
			}
		}

		public override void OnRenderGUI(DeltaTimeEventArgs e)
		{
			MainMenu.Draw(new IMainMenuItem?[] { fileMenuItem, emulationMenuItem, debugMenuItem, optionsMenuItem, helpMenuItem });
			StatusBar.Draw(new StatusBarItem?[] { statusStatusBarItem, fpsStatusBarItem });

			displayWindow.Draw(displayTexture);
			aboutWindow.Draw((productInformation, GL.GetContextInfo()));

			cpuStatusWindow.Draw(nes);
			cpuDisassemblyWindow.Draw(nes);
			patternTableWindow.Draw(nes);

			if (GlobalVariables.IsAuthorsMachine && GlobalVariables.IsDebugBuild)
				Hexa.NET.ImGui.ImGui.ShowDemoWindow();
		}

		public override void OnShutdown()
		{
			SaveCartridgeRam();

			configuration.DisplaySize = displayWindow.WindowScale;
			configuration.SaveToFile(configurationPath);
		}

		private void ShowOpenRomDialog()
		{
			var (lastRomDirectory, lastRomFilename) = (string.Empty, string.Empty);
			if (!string.IsNullOrEmpty(configuration.LastRomLoaded))
			{
				lastRomDirectory = Path.GetDirectoryName(configuration.LastRomLoaded);
				lastRomFilename = Path.GetFileName(configuration.LastRomLoaded);
			}
			if (openRomDialog.Open(out string? filename, lastRomDirectory, lastRomFilename) == DialogResult.Okay && filename != null)
			{
				LoadAndRunCartridge(filename);
				displayWindow.IsFocused = true;
			}
		}

		private void LoadAndRunCartridge(string filename)
		{
			try
			{
				if (nes == null || string.IsNullOrEmpty(filename)) return;

				using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				nes.LoadCartridge(new Cartridge(new BinaryReader(stream)));

				cartridgeFilename = filename;
				cartSaveFilename = $"{Path.GetFileNameWithoutExtension(cartridgeFilename)}.sav";

				LoadCartridgeRam();

				nes.Reset();

				if (statusStatusBarItem != null)
					statusStatusBarItem.Label = $"Emulation started, running '{cartridgeFilename}'";

				configuration.LastRomLoaded = cartridgeFilename;
				configuration.SaveToFile(configurationPath);

				isSystemRunning = true;
			}
			catch (Exception e)
			{
				ShowMessageBox("Error", e.Message, SDLMessageBoxFlags.Error);
			}
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
			if (nes?.Cartridge == null) return;

			if (nes.Cartridge.Header.HasPersistantMemory)
			{
				var savePath = Path.Combine(saveDataPath, cartSaveFilename);

				if (!File.Exists(savePath)) return;
				var prgRam = File.ReadAllBytes(savePath);
				for (var i = 0; i < Math.Min(0x2000, prgRam.Length); i++)
					nes.Write((ushort)(0x6000 + i), prgRam[i]);
			}
		}

		private void SaveCartridgeRam()
		{
			if (nes?.Cartridge == null) return;

			if (nes.Cartridge.Header.HasPersistantMemory)
			{
				if (string.IsNullOrWhiteSpace(cartSaveFilename)) return;

				var prgRam = new byte[0x2000];
				for (var i = 0; i < Math.Min(0x2000, prgRam.Length); i++)
					prgRam[i] = nes.Read((ushort)(0x6000 + i));

				var savePath = Path.Combine(saveDataPath, cartSaveFilename);
				File.WriteAllBytes(savePath, prgRam);
			}
		}

		protected override void DisposeManaged()
		{
			displayTexture?.Dispose();
			audioHandler.Dispose();

			base.DisposeManaged();
		}
	}
}
