using Hexa.NET.ImGui;

namespace FamiSharp.UserInterface
{
	public sealed class MainMenu
	{
		public const string Seperator = "---";

		public static void Draw(object? userData)
		{
			if (userData is not MainMenuItem[] mainMenuItems) return;

			if (ImGui.BeginMainMenuBar())
			{
				foreach (var mainMenuItem in mainMenuItems)
					DrawMenu(mainMenuItem);
			}
			ImGui.EndMainMenuBar();
		}

		private static void DrawMenu(MainMenuItem mainMenuItem)
		{
			if (mainMenuItem == null) return;

			if (mainMenuItem.Label == Seperator)
				ImGui.Separator();
			else
			{
				if (mainMenuItem.ClickAction == null && mainMenuItem.SubItems.Length > 0)
				{
					if (ImGui.BeginMenu(mainMenuItem.Label))
					{
						foreach (var subItem in mainMenuItem.SubItems)
							DrawMenu(subItem);
						ImGui.EndMenu();
					}
				}
				else
				{
					mainMenuItem.UpdateAction?.Invoke(mainMenuItem);
					if (ImGui.MenuItem(mainMenuItem.Label, string.Empty, mainMenuItem.IsChecked, mainMenuItem.IsEnabled) && mainMenuItem.ClickAction != null)
						mainMenuItem.ClickAction(mainMenuItem);
				}
			}
		}
	}

	public sealed class MainMenuItem(string label = "Label", Action<MainMenuItem> clickAction = null!, Action<MainMenuItem> updateAction = null!)
	{
		public string Label { get; set; } = label;
		public Action<MainMenuItem> ClickAction { get; set; } = clickAction;
		public Action<MainMenuItem> UpdateAction { get; set; } = updateAction;
		public MainMenuItem[] SubItems { get; set; } = [];
		public bool IsEnabled { get; set; } = true;
		public bool IsChecked { get; set; } = false;
	}
}
