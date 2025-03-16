using Hexa.NET.ImGui;
using Hexa.NET.SDL2;

namespace FamiSharp.UserInterface
{
	public sealed class MainMenu
	{
		public static void Draw(object? userData)
		{
			if (userData is not IMainMenuItem[] mainMenuItems) return;

			if (ImGui.BeginMainMenuBar())
			{
				foreach (var mainMenuItem in mainMenuItems)
					DrawMenu(mainMenuItem);
			}
			ImGui.EndMainMenuBar();
		}

		private static void DrawMenu(IMainMenuItem mainMenuItem)
		{
			if (mainMenuItem == null) return;

			if (mainMenuItem is MainMenuSeperatorItem)
				ImGui.Separator();
			else
			{
				if (mainMenuItem.ClickAction == null)
				{
					if (mainMenuItem is MainMenuTextItem mainMenuTextItem && mainMenuTextItem.SubItems.Length > 0)
					{
						if (ImGui.BeginMenu(mainMenuTextItem.Label))
						{
							foreach (var subItem in mainMenuTextItem.SubItems)
								DrawMenu(subItem);
							ImGui.EndMenu();
						}
					}
				}
				else
				{
					mainMenuItem.UpdateAction?.Invoke(mainMenuItem);

					if (mainMenuItem is MainMenuTextItem mainMenuTextItem)
					{
						if (ImGui.MenuItem(mainMenuTextItem.Label, mainMenuTextItem.Shortcut != SDLKeyCode.Unknown ? $"Ctrl+{mainMenuTextItem.Shortcut}" : string.Empty, mainMenuTextItem.IsChecked, mainMenuTextItem.IsEnabled) && mainMenuTextItem.ClickAction != null)
							mainMenuTextItem.ClickAction(mainMenuTextItem);
					}
					else if (mainMenuItem is MainMenuSliderItem mainMenuSliderItem)
					{
						var value = mainMenuSliderItem.Value;
						if (ImGui.SliderInt(mainMenuSliderItem.Label, ref value, mainMenuSliderItem.MinValue, mainMenuSliderItem.MaxValue, mainMenuSliderItem.Format))
						{
							mainMenuSliderItem.Value = value;
							mainMenuSliderItem.ClickAction?.Invoke(mainMenuSliderItem);
						}
					}
				}
			}
		}
	}

	public interface IMainMenuItem
	{
		string Label { get; set; }
		Action<IMainMenuItem>? ClickAction { get; set; }
		Action<IMainMenuItem>? UpdateAction { get; set; }
		bool IsEnabled { get; set; }
	}

	public sealed class MainMenuSeperatorItem : IMainMenuItem
	{
		public string Label { get; set; } = string.Empty;
		public Action<IMainMenuItem>? ClickAction { get; set; }
		public Action<IMainMenuItem>? UpdateAction { get; set; }
		public bool IsEnabled { get; set; } = true;

		public readonly static MainMenuSeperatorItem Default = new();
	}

	public sealed class MainMenuTextItem(string label = "Label", SDLKeyCode shortcut = SDLKeyCode.Unknown, Action<IMainMenuItem> clickAction = null!, Action<IMainMenuItem> updateAction = null!) : IMainMenuItem
	{
		public string Label { get; set; } = label;
		public SDLKeyCode Shortcut { get; set; } = shortcut;
		public Action<IMainMenuItem>? ClickAction { get; set; } = clickAction;
		public Action<IMainMenuItem>? UpdateAction { get; set; } = updateAction;
		public IMainMenuItem[] SubItems { get; set; } = [];
		public bool IsEnabled { get; set; } = true;
		public bool IsChecked { get; set; }
	}

	public sealed class MainMenuSliderItem(string label = "Label", Action<IMainMenuItem> clickAction = null!, Action<IMainMenuItem> updateAction = null!, int minValue = 0, int maxValue = 16, string format = "%d") : IMainMenuItem
	{
		public string Label { get; set; } = label;
		public Action<IMainMenuItem>? ClickAction { get; set; } = clickAction;
		public Action<IMainMenuItem>? UpdateAction { get; set; } = updateAction;
		public bool IsEnabled { get; set; } = true;
		public int Value { get; set; }
		public int MinValue { get; set; } = minValue;
		public int MaxValue { get; set; } = maxValue;
		public string Format { get; set; } = format;
	}
}
