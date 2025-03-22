using Hexa.NET.ImGui;
using System.Text;

namespace FamiSharp.UserInterface
{
	public class AboutWindow : WindowBase
	{
		public override string Title => "About";

		bool showDebugInfo;

		protected override void DrawWindow(object? userData)
		{
			if (userData is not (ProductInformation productInfo, GLInfo glInfo)) return;

			var io = ImGui.GetIO();

			ImGui.SetNextWindowPos(new(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f), ImGuiCond.Always, new(0.5f, 0.5f));
			if (!ImGui.Begin(Title, ref windowOpen, ImGuiWindowFlags.NoCollapse))
			{
				ImGui.End();
				return;
			}

			ImGui.Text($"{productInfo.Name} v{productInfo.Version}");
			ImGui.Text(productInfo.Copyright);
			ImGui.Separator();
			ImGui.TextLinkOpenURL("Homepage", "https://github.com/xdanieldzd/FamiSharp"); ImGui.SameLine();
			ImGui.TextLinkOpenURL("Releases", "https://github.com/xdanieldzd/FamiSharp/releases");
			ImGui.NewLine();

			ImGui.Checkbox("Show debug information", ref showDebugInfo);
			if (showDebugInfo)
			{
				var debugInfoBuilder = new StringBuilder();
				debugInfoBuilder.AppendLine("System information");
				debugInfoBuilder.AppendLine($"- OS Version:  {Environment.OSVersion}");
				debugInfoBuilder.AppendLine($"- CLR Version: {Environment.Version}");
				debugInfoBuilder.AppendLine($"- Working Set: {Environment.WorkingSet} bytes");
				debugInfoBuilder.AppendLine();

				debugInfoBuilder.AppendLine("OpenGL information");
				debugInfoBuilder.AppendLine($"- Renderer:               {glInfo.Renderer}");
				debugInfoBuilder.AppendLine($"- Vendor:                 {glInfo.Vendor}");
				debugInfoBuilder.AppendLine($"- Version:                {glInfo.Version}");
				debugInfoBuilder.AppendLine($"- ShadingLanguageVersion: {glInfo.ShadingLanguageVersion}");
				debugInfoBuilder.AppendLine($"- MaxTextureSize:         {glInfo.MaxTextureSize}");

				debugInfoBuilder.AppendLine("- Supported extensions");
				foreach (var extension in glInfo.Extensions)
					debugInfoBuilder.AppendLine($" - {extension}");
				debugInfoBuilder.AppendLine();

				var debugInfo = debugInfoBuilder.ToString() + '\0';
				ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
				ImGui.InputTextMultiline("##debuginfo", ref debugInfo, (nuint)debugInfo.Length, ImGuiInputTextFlags.ReadOnly);
			}
			ImGui.NewLine();

			if (ImGui.Button("Close", new(ImGui.GetContentRegionAvail().X, 0f))) IsWindowOpen = false;

			ImGui.End();
		}
	}
}
