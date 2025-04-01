using FamiSharp.Utilities;
using Hexa.NET.ImGui;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FamiSharp.UserInterface
{
	public unsafe class AboutWindow : WindowBase
	{
		public override string Title => "About";

		readonly Process currentProcess;

		readonly List<string> debugInfo = [];
		readonly ImGuiTextSelect textSelect;
		bool showDebugInfo;

		public AboutWindow()
		{
			currentProcess = Process.GetCurrentProcess();
			textSelect = new((idx) => debugInfo[idx], () => debugInfo.Count);
		}

		protected override void DrawWindow(object? userData)
		{
			if (userData is not (ProductInformation productInfo, GLInfo glInfo)) return;

			var io = ImGui.GetIO();

			ImGui.SetNextWindowPos(new(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f), ImGuiCond.Always, new(0.5f, 0.5f));
			if (!ImGui.Begin(Title, ref windowOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
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
				if (debugInfo.Count == 0)
				{
					//debugInfo.Add("THIS IS A LOOOOOOOONG LINE!!! THIS IS A LOOOOOOOONG LINE!!! THIS IS A LOOOOOOOONG LINE!!! THIS IS A LOOOOOOOONG LINE!!! THIS IS A LOOOOOOOONG LINE!!!");
					debugInfo.Add("Application information:");
					debugInfo.Add($"- Process Name:           {currentProcess.ProcessName}");
					debugInfo.Add($"- Module Name:            {currentProcess.MainModule?.ModuleName}");
					debugInfo.Add($"- Handle:                 {currentProcess.Handle}");
					debugInfo.Add($"- Peak Working Set:       {currentProcess.PeakWorkingSet64} bytes");
					debugInfo.Add($"- Start Time:             {currentProcess.StartTime}");
					debugInfo.Add($"- Process Architecture:   {RuntimeInformation.ProcessArchitecture}");
					debugInfo.Add("- Dear ImGui information:");
					debugInfo.Add($" - Version:               {ImGui.GetVersionS()}");
					debugInfo.Add($" - Backend Platform Name: {Marshal.PtrToStringUTF8((nint)io.BackendPlatformName)}");
					debugInfo.Add($" - Backend Renderer Name: {Marshal.PtrToStringUTF8((nint)io.BackendRendererName)}");
					debugInfo.Add(Environment.NewLine);

					debugInfo.Add("System information:");
					debugInfo.Add($"- OS Version:             {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}");
					debugInfo.Add($"- Framework Description:  {RuntimeInformation.FrameworkDescription}");
					debugInfo.Add($"- Runtime Identifier:     {RuntimeInformation.RuntimeIdentifier}");
					debugInfo.Add(Environment.NewLine);

					debugInfo.Add("OpenGL information:");
					debugInfo.Add($"- Renderer:               {glInfo.Renderer}");
					debugInfo.Add($"- Vendor:                 {glInfo.Vendor}");
					debugInfo.Add($"- Version:                {glInfo.Version}");
					debugInfo.Add($"- ShadingLanguageVersion: {glInfo.ShadingLanguageVersion}");
					debugInfo.Add($"- Major/MinorVersion:     {glInfo.ContextVersion}");
					debugInfo.Add($"- MaxTextureSize:         {glInfo.MaxTextureSize}");
					debugInfo.Add("- Supported extensions:");
					foreach (var extension in glInfo.Extensions) debugInfo.Add($" - {extension}");
				}

				if (ImGui.BeginChild("##debuginfo",
					ImGuiChildFlags.ResizeY | ImGuiChildFlags.FrameStyle,
					ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
				{
					textSelect.Update();

					if (ImGui.BeginPopupContextWindow())
					{
						ImGui.BeginDisabled(!textSelect.HasSelection());
						if (ImGui.MenuItem("Copy", "Ctrl+C")) textSelect.Copy();
						ImGui.EndDisabled();

						if (ImGui.MenuItem("Select All", "Ctrl+A")) textSelect.SelectAll();

						ImGui.EndPopup();
					}

					foreach (var line in debugInfo) ImGui.TextUnformatted(line);
				}
				ImGui.EndChild();
			}

			ImGui.NewLine();

			if (ImGui.Button("Close", new(ImGui.GetContentRegionAvail().X, 0f))) IsWindowOpen = false;

			ImGui.End();
		}
	}
}
