using FamiSharp.UserInterface;
using Hexa.NET.SDL2;
using NativeFileDialogNET;

namespace FamiSharp
{
	public partial class Emulator
	{
		readonly DisplayWindow displayWindow = new() { IsWindowOpen = true };
		readonly AboutWindow aboutWindow = new();
		readonly CpuStatusWindow cpuStatusWindow = new();
		readonly CpuDisassemblyWindow cpuDisassemblyWindow = new();
		readonly PatternTableWindow patternTableWindow = new();

		readonly static (string Description, string Extension)[] romFileExtensions = [("NES ROM files", "nes")];
		readonly NativeFileDialog openRomDialog = new();

		MainMenuTextItem? fileOpenMenuItem, fileExitMenuItem;
		MainMenuTextItem? emulationPauseMenuItem, emulationResetMenuItem, emulationShutdownMenuItem;
		MainMenuTextItem? debugDisassemblyMenuItem, debugCpuStatusMenuItem, debugPatternTableMenuItem;
		MainMenuTextItem? optionsLimitFpsMenuItem;
		MainMenuSliderItem? optionsDisplaySizeMenuItem;
		MainMenuTextItem? helpAboutMenuItem;

		MainMenuTextItem? fileMenuItem, emulationMenuItem, debugMenuItem, optionsMenuItem, helpMenuItem;

		readonly List<MainMenuTextItem> menuItemsWithShortcuts = [];

		StatusBarItem? statusStatusBarItem, fpsStatusBarItem;

		private void InitializeUI()
		{
			displayWindow.WindowScale = configuration.DisplaySize;

			foreach (var (desc, ext) in romFileExtensions)
				openRomDialog.AddFilter(desc, ext);

			fileOpenMenuItem = new(
				label: "Open ROM",
				shortcut: SDLKeyCode.O,
				clickAction: (s) => { ShowOpenRomDialog(); });

			fileExitMenuItem = new(
				label: "Exit",
				clickAction: (s) => { Exit(); });

			emulationPauseMenuItem = new(
				label: "Pause",
				shortcut: SDLKeyCode.P,
				clickAction: (s) => { isEmulationPaused = !isEmulationPaused; },
				updateAction: (s) => { s.IsEnabled = isSystemRunning; (s as MainMenuTextItem)!.IsChecked = isEmulationPaused; });

			emulationResetMenuItem = new(
				label: "Reset",
				shortcut: SDLKeyCode.R,
				clickAction: (s) => { nes?.Reset(); LoadCartridgeRam(); },
				updateAction: (s) => { s.IsEnabled = isSystemRunning; });

			emulationShutdownMenuItem = new(
				label: "Shutdown",
				clickAction: (s) => { StopEmulation(); },
				updateAction: (s) => { s.IsEnabled = isSystemRunning; });

			debugDisassemblyMenuItem = new(
				label: "Disassembly",
				clickAction: (s) => { cpuDisassemblyWindow.IsWindowOpen = true; cpuDisassemblyWindow.IsFocused = true; },
				updateAction: (s) => { if (s is MainMenuTextItem textItem) textItem.IsChecked = cpuDisassemblyWindow.IsWindowOpen; });

			debugCpuStatusMenuItem = new(
				label: "CPU Status",
				clickAction: (s) => { cpuStatusWindow.IsWindowOpen = true; cpuStatusWindow.IsFocused = true; },
				updateAction: (s) => { if (s is MainMenuTextItem textItem) textItem.IsChecked = cpuStatusWindow.IsWindowOpen; });

			debugPatternTableMenuItem = new(
				label: "Pattern Tables",
				clickAction: (s) => { patternTableWindow.IsWindowOpen = true; patternTableWindow.IsFocused = true; },
				updateAction: (s) => { if (s is MainMenuTextItem textItem) textItem.IsChecked = patternTableWindow.IsWindowOpen; });

			optionsLimitFpsMenuItem = new(
				label: "Limit FPS",
				clickAction: (s) => { configuration.LimitFps = !configuration.LimitFps; averageFps.Clear(); },
				updateAction: (s) => { if (s is MainMenuTextItem textItem) textItem.IsChecked = configuration.LimitFps; });

			optionsDisplaySizeMenuItem = new(
				label: "Display Size",
				clickAction: (s) => { if (s is MainMenuSliderItem sliderItem) configuration.DisplaySize = sliderItem.Value; },
				updateAction: (s) => { if (s is MainMenuSliderItem sliderItem) sliderItem.Value = displayWindow.WindowScale = configuration.DisplaySize; },
				minValue: 1,
				maxValue: 3,
				format: "%dx");

			helpAboutMenuItem = new(
				label: "About",
				clickAction: (s) => { aboutWindow.IsWindowOpen = true; aboutWindow.IsFocused = true; });

			fileMenuItem = new("File") { SubItems = [fileOpenMenuItem, MainMenuSeperatorItem.Default, fileExitMenuItem] };
			emulationMenuItem = new("Emulation") { SubItems = [emulationPauseMenuItem, emulationResetMenuItem, MainMenuSeperatorItem.Default, emulationShutdownMenuItem] };
			debugMenuItem = new("Debug") { SubItems = [debugDisassemblyMenuItem, debugCpuStatusMenuItem, debugPatternTableMenuItem] };
			optionsMenuItem = new("Options") { SubItems = [optionsLimitFpsMenuItem, MainMenuSeperatorItem.Default, optionsDisplaySizeMenuItem] };
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
