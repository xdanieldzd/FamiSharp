using FamiSharp.Utilities;
using Hexa.NET.ImGui;

namespace FamiSharp.UserInterface
{
	public class AboutWindow : WindowBase
	{
		public override string Title => "About";

		protected override void DrawWindow(object? userData)
		{
			if (userData is not ApplicationInfo appInfo) return;

			var io = ImGui.GetIO();

			ImGui.SetNextWindowPos(new(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f), ImGuiCond.Always, new(0.5f, 0.5f));
			if (!ImGui.Begin(Title, ref windowOpen, ImGuiWindowFlags.NoCollapse))
			{
				ImGui.End();
				return;
			}

			ImGui.Text($"{appInfo.Name} v{appInfo.Version}");
			ImGui.Text(appInfo.Copyright);
			ImGui.Separator();
			ImGui.TextLinkOpenURL("Homepage", "https://github.com/xdanieldzd/FamiSharp"); ImGui.SameLine();
			ImGui.TextLinkOpenURL("Releases", "https://github.com/xdanieldzd/FamiSharp/releases");
			ImGui.NewLine();

			if (ImGui.Button("Close", new(ImGui.GetContentRegionAvail().X, 0f))) IsWindowOpen = false;

			ImGui.End();
		}
	}
}
