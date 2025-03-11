using FamiSharp.UserInterface;
using Hexa.NET.SDL2;
using NativeFileDialogNET;

namespace FamiSharp
{
	public partial class Emulator
	{
		MainMenuItem? fileOpenMenuItem, fileExitMenuItem;
		MainMenuItem? emulationPauseMenuItem, emulationResetMenuItem, emulationShutdownMenuItem;
		MainMenuItem? debugDisassemblyMenuItem, debugCpuStatusMenuItem, debugPatternTableMenuItem;
		MainMenuItem? optionsLimitFpsMenuItem;
		MainMenuItem? helpAboutMenuItem;

		MainMenuItem? fileMenuItem, emulationMenuItem, debugMenuItem, optionsMenuItem, helpMenuItem;

		readonly List<MainMenuItem> menuItemsWithShortcuts = [];

		StatusBarItem? statusStatusBarItem, fpsStatusBarItem;

		readonly DisplayWindow displayWindow = new() { IsWindowOpen = true, WindowScale = AppEnvironment.Configuration.DisplaySize };
		readonly AboutWindow aboutWindow = new();
		readonly CpuStatusWindow cpuStatusWindow = new();
		readonly CpuDisassemblyWindow cpuDisassemblyWindow = new();
		readonly PatternTableWindow patternTableWindow = new();

		readonly static (string Description, string Extension)[] romFileExtensions = [("NES ROM files", "nes")];
		readonly NativeFileDialog openRomDialog = new();

		private void InitializeUI()
		{
			foreach (var (desc, ext) in romFileExtensions)
				openRomDialog.AddFilter(desc, ext);

			fileOpenMenuItem = new("Open ROM", SDLKeyCode.O, clickAction: (s) => { ShowOpenRomDialog(); });
			fileExitMenuItem = new("Exit", clickAction: (s) => { Exit(); });

			emulationPauseMenuItem = new("Pause", SDLKeyCode.P, clickAction: (s) => { isEmulationPaused = !isEmulationPaused; }, updateAction: (s) => { s.IsEnabled = isSystemRunning; s.IsChecked = isEmulationPaused; });
			emulationResetMenuItem = new("Reset", SDLKeyCode.R, clickAction: (s) => { nes?.Reset(); LoadCartridgeRam(); }, updateAction: (s) => { s.IsEnabled = isSystemRunning; });
			emulationShutdownMenuItem = new("Shutdown", clickAction: (s) => { StopEmulation(); }, updateAction: (s) => { s.IsEnabled = isSystemRunning; });

			debugDisassemblyMenuItem = new("Disassembly", clickAction: (s) => { cpuDisassemblyWindow.IsWindowOpen = true; cpuDisassemblyWindow.IsFocused = true; }, updateAction: (s) => { s.IsChecked = cpuDisassemblyWindow.IsWindowOpen; });
			debugCpuStatusMenuItem = new("CPU Status", clickAction: (s) => { cpuStatusWindow.IsWindowOpen = true; cpuStatusWindow.IsFocused = true; }, updateAction: (s) => { s.IsChecked = cpuStatusWindow.IsWindowOpen; });
			debugPatternTableMenuItem = new("Pattern Tables", clickAction: (s) => { patternTableWindow.IsWindowOpen = true; patternTableWindow.IsFocused = true; }, updateAction: (s) => { s.IsChecked = patternTableWindow.IsWindowOpen; });

			optionsLimitFpsMenuItem = new("Limit FPS", clickAction: (s) => { AppEnvironment.Configuration.LimitFps = !AppEnvironment.Configuration.LimitFps; averageFps.Clear(); }, updateAction: (s) => { s.IsChecked = AppEnvironment.Configuration.LimitFps; });

			helpAboutMenuItem = new("About", clickAction: (s) => { aboutWindow.IsWindowOpen = true; aboutWindow.IsFocused = true; });

			fileMenuItem = new("File") { SubItems = [fileOpenMenuItem, new(MainMenu.Seperator), fileExitMenuItem] };
			emulationMenuItem = new("Emulation") { SubItems = [emulationPauseMenuItem, emulationResetMenuItem, new(MainMenu.Seperator), emulationShutdownMenuItem] };
			debugMenuItem = new("Debug") { SubItems = [debugDisassemblyMenuItem, debugCpuStatusMenuItem, debugPatternTableMenuItem] };
			optionsMenuItem = new("Options") { SubItems = [optionsLimitFpsMenuItem] };
			helpMenuItem = new("Help") { SubItems = [helpAboutMenuItem] };

			menuItemsWithShortcuts.AddRange([fileOpenMenuItem, emulationPauseMenuItem, emulationResetMenuItem]);

			statusStatusBarItem = new("Ready!") { ShowSeparator = false };
			fpsStatusBarItem = new(string.Empty) { TextAlignment = StatusBarItemTextAlign.Center, ItemAlignment = StatusBarItemAlign.Right, Width = 80 };
		}

		private bool HandleMenuShortcuts(KeycodeEventArgs e)
		{
			if ((e.Modifier & SDLKeymod.Ctrl) == 0) return false;

			foreach (var menuItem in menuItemsWithShortcuts)
			{
				if (e.Keycode == menuItem.Shortcut)
				{
					menuItem.ClickAction(menuItem);
					return true;
				}
			}
			return false;
		}
	}
}
