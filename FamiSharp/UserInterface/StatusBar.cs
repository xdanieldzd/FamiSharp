using Hexa.NET.ImGui;

namespace FamiSharp.UserInterface
{
	public sealed class StatusBar
	{
		public static void Draw(object? userData)
		{
			if (userData is not StatusBarItem[] statusBarItems) return;

			var viewport = ImGui.GetMainViewport();
			var frameHeight = ImGui.GetFrameHeight();

			ImGui.SetNextWindowPos(new(viewport.Pos.X, viewport.Pos.Y + viewport.Size.Y - frameHeight));
			ImGui.SetNextWindowSize(new(viewport.Size.X, frameHeight));

			var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse |
				 ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.MenuBar;

			var framePadding = ImGui.GetStyle().FramePadding.X;
			var itemPadding = framePadding * 4f;

			static void drawItem(StatusBarItem item)
			{
				if (item.IsEnabled) ImGui.Text(item.Label);
				else ImGui.TextDisabled(item.Label);

				if (!string.IsNullOrWhiteSpace(item.ToolTip) && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled | ImGuiHoveredFlags.AllowWhenOverlapped))
					ImGui.SetTooltip(item.ToolTip);
			}

			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
			if (ImGui.Begin("##StatusBar", flags))
			{
				if (ImGui.BeginMenuBar())
				{
					var drawList = ImGui.GetWindowDrawList();
					var windowPos = ImGui.GetWindowPos();

					var cursorFromLeft = framePadding * 2f;
					var cursorFromRight = ImGui.GetWindowWidth() - framePadding * 2f;

					foreach (var item in statusBarItems)
					{
						if (item == null) continue;

						var labelWidth = ImGui.CalcTextSize(item.Label).X;
						var itemWidth = Math.Max(labelWidth, item.Width);

						if (item.ItemAlignment == StatusBarItemAlign.Left)
						{
							ImGui.SetCursorPosX(cursorFromLeft);
							cursorFromLeft += itemWidth + itemPadding;
							if (item.ShowSeparator)
								drawList.AddLine(
									new(windowPos.X + cursorFromLeft - itemPadding / 2f, windowPos.Y),
									new(windowPos.X + cursorFromLeft - itemPadding / 2f, windowPos.Y + frameHeight),
									ImGui.GetColorU32(ImGuiCol.TextDisabled));
						}
						else
						{
							ImGui.SetCursorPosX(cursorFromRight - itemWidth);
							cursorFromRight -= itemWidth + itemPadding;
							if (item.ShowSeparator)
								drawList.AddLine(
									new(windowPos.X + cursorFromRight + itemPadding / 2f, windowPos.Y),
									new(windowPos.X + cursorFromRight + itemPadding / 2f, windowPos.Y + frameHeight),
									ImGui.GetColorU32(ImGuiCol.TextDisabled));
						}

						if (item.TextAlignment != StatusBarItemTextAlign.Left)
						{
							var cursorPos = ImGui.GetCursorPosX();

							if (item.TextAlignment == StatusBarItemTextAlign.Right)
								ImGui.SetCursorPosX(cursorPos + itemWidth - labelWidth);
							else if (item.TextAlignment == StatusBarItemTextAlign.Center)
								ImGui.SetCursorPosX(cursorPos + (itemWidth / 2f - labelWidth / 2f));

							drawItem(item);

							ImGui.SetCursorPosX(cursorPos);
						}
						else
							drawItem(item);
					}
				}
				ImGui.EndMenuBar();
			}
			ImGui.PopStyleVar();

			ImGui.End();
		}
	}

	public enum StatusBarItemAlign { Left, Right }
	public enum StatusBarItemTextAlign { Left, Right, Center }

	internal class StatusBarItem(string label = "Label")
	{
		public string Label { get; set; } = label;
		public string ToolTip { get; set; } = string.Empty;
		public float Width { get; set; } = 0f;
		public StatusBarItemAlign ItemAlignment { get; set; } = StatusBarItemAlign.Left;
		public StatusBarItemTextAlign TextAlignment { get; set; } = StatusBarItemTextAlign.Left;
		public bool ShowSeparator { get; set; } = true;
		public bool IsEnabled { get; set; } = true;
	}
}
