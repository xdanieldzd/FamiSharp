using FamiSharp.UserInterface;
using NativeFileDialogNET;

namespace FamiSharp
{
	public partial class Emulator
	{
		MainMenuItem? fileOpenMenuItem, fileExitMenuItem;
		MainMenuItem? emulationPauseMenuItem, emulationResetMenuItem, emulationShutdownMenuItem;
		MainMenuItem? optionsLimitFpsMenuItem;
		MainMenuItem? helpAboutMenuItem;

		MainMenuItem? fileMenuItem, emulationMenuItem, optionsMenuItem, helpMenuItem;

		StatusBarItem? statusStatusBarItem, fpsStatusBarItem;

		readonly DisplayWindow displayWindow = new() { IsWindowOpen = true, WindowScale = AppEnvironment.Configuration.DisplaySize };
		readonly AboutWindow aboutWindow = new();

		readonly static (string Description, string Extension)[] romFileExtensions = [("NES ROM files", "nes")];
		readonly NativeFileDialog openRomDialog = new();

		private void InitializeUI()
		{
			foreach (var (desc, ext) in romFileExtensions)
				openRomDialog.AddFilter(desc, ext);

			fileOpenMenuItem = new("Open ROM", clickAction: (s) =>
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
			});
			fileExitMenuItem = new("Exit", clickAction: (s) => { Exit(); });

			emulationPauseMenuItem = new("Pause", clickAction: (s) => { isEmulationPaused = !isEmulationPaused; }, updateAction: (s) => { s.IsEnabled = isSystemRunning; s.IsChecked = isEmulationPaused; });
			emulationResetMenuItem = new("Reset", clickAction: (s) => { nes.Reset(); LoadCartridgeRam(); }, updateAction: (s) => { s.IsEnabled = isSystemRunning; });
			emulationShutdownMenuItem = new("Shutdown", clickAction: (s) => { StopEmulation(); }, updateAction: (s) => { s.IsEnabled = isSystemRunning; });

			optionsLimitFpsMenuItem = new("Limit FPS", clickAction: (s) => { AppEnvironment.Configuration.LimitFps = !AppEnvironment.Configuration.LimitFps; }, updateAction: (s) => { s.IsChecked = AppEnvironment.Configuration.LimitFps; });

			helpAboutMenuItem = new("About", clickAction: (s) => { aboutWindow.IsWindowOpen = true; });

			fileMenuItem = new("File") { SubItems = [fileOpenMenuItem, new(MainMenu.Seperator), fileExitMenuItem] };
			emulationMenuItem = new("Emulation") { SubItems = [emulationPauseMenuItem, emulationResetMenuItem, new(MainMenu.Seperator), emulationShutdownMenuItem] };
			optionsMenuItem = new("Options") { SubItems = [optionsLimitFpsMenuItem] };
			helpMenuItem = new("Help") { SubItems = [helpAboutMenuItem] };

			statusStatusBarItem = new("Ready!") { ShowSeparator = false };
			fpsStatusBarItem = new(string.Empty) { TextAlignment = StatusBarItemTextAlign.Center, ItemAlignment = StatusBarItemAlign.Right, Width = 80 };
		}
	}
}
