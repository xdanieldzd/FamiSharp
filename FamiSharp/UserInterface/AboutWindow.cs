using Hexa.NET.ImGui;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiSharp.UserInterface
{
	public unsafe class AboutWindow : WindowBase
	{
		public override string Title => "About";

		readonly Process currentProcess;

		bool showDebugInfo;

		public AboutWindow() => currentProcess = Process.GetCurrentProcess();

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
				debugInfoBuilder.AppendLine("Application information:");
				debugInfoBuilder.AppendLine($"- Process Name:           {currentProcess.ProcessName}");
				debugInfoBuilder.AppendLine($"- Module Name:            {currentProcess.MainModule?.ModuleName}");
				debugInfoBuilder.AppendLine($"- Handle:                 {currentProcess.Handle}");
				debugInfoBuilder.AppendLine($"- Peak Working Set:       {currentProcess.PeakWorkingSet64} bytes");
				debugInfoBuilder.AppendLine($"- Start Time:             {currentProcess.StartTime}");
				debugInfoBuilder.AppendLine($"- Process Architecture:   {RuntimeInformation.ProcessArchitecture}");
				debugInfoBuilder.AppendLine("- Dear ImGui information:");
				debugInfoBuilder.AppendLine($" - Version:               {ImGui.GetVersionS()}");
				debugInfoBuilder.AppendLine($" - Backend Platform Name: {Marshal.PtrToStringUTF8((nint)io.BackendPlatformName)}");
				debugInfoBuilder.AppendLine($" - Backend Renderer Name: {Marshal.PtrToStringUTF8((nint)io.BackendRendererName)}");
				debugInfoBuilder.AppendLine();

				debugInfoBuilder.AppendLine("System information:");
				debugInfoBuilder.AppendLine($"- OS Version:             {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}");
				debugInfoBuilder.AppendLine($"- Framework Description:  {RuntimeInformation.FrameworkDescription}");
				debugInfoBuilder.AppendLine($"- Runtime Identifier:     {RuntimeInformation.RuntimeIdentifier}");
				debugInfoBuilder.AppendLine();

				debugInfoBuilder.AppendLine("OpenGL information:");
				debugInfoBuilder.AppendLine($"- Renderer:               {glInfo.Renderer}");
				debugInfoBuilder.AppendLine($"- Vendor:                 {glInfo.Vendor}");
				debugInfoBuilder.AppendLine($"- Version:                {glInfo.Version}");
				debugInfoBuilder.AppendLine($"- ShadingLanguageVersion: {glInfo.ShadingLanguageVersion}");
				debugInfoBuilder.AppendLine($"- Major/MinorVersion:     {glInfo.ContextVersion}");
				debugInfoBuilder.AppendLine($"- MaxTextureSize:         {glInfo.MaxTextureSize}");

				debugInfoBuilder.AppendLine("- Supported extensions:");
				foreach (var extension in glInfo.Extensions)
					debugInfoBuilder.AppendLine($" - {extension}");

				var debugInfo = debugInfoBuilder.ToString() + '\0';
				ImGui.InputTextMultiline("##debuginfo", ref debugInfo, (nuint)debugInfo.Length, new(ImGui.GetContentRegionAvail().X, 200f), ImGuiInputTextFlags.ReadOnly);
			}
			ImGui.NewLine();

			if (ImGui.Button("Close", new(ImGui.GetContentRegionAvail().X, 0f))) IsWindowOpen = false;

			ImGui.End();
		}
	}
}
