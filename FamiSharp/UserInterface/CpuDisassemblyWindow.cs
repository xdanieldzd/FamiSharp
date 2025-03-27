using FamiSharp.Emulation;
using Hexa.NET.ImGui;
using System.Numerics;

namespace FamiSharp.UserInterface
{
	public class CpuDisassemblyWindow : WindowBase
	{
		public override string Title => "Disassembly";

		readonly (ushort, byte?[], string)[] disassembly = new (ushort, byte?[], string)[256];

		public CpuDisassemblyWindow() : base(new(0f, 400f), ImGuiCond.Always) { }

		protected override void DrawWindow(object? userData)
		{
			if (userData is not NesSystem nes) return;

			if (!ImGui.Begin(Title, ref windowOpen))
			{
				ImGui.End();
				return;
			}

			ImGui.TextDisabled("WIP");

			/* TODO:	pause/reset/step instruction/step scanline
			 *			center opcode at PC vertically
			 *			etc etc
			*/

			var disasmLines = (int)Math.Min(ImGui.GetContentRegionAvail().Y / ImGui.GetTextLineHeightWithSpacing() - 1, disassembly.Length);
			for (int i = 0, j = 0; i < disasmLines; i++)
			{
				var address = (ushort)(nes.Cpu.PC - disasmLines / 2 + j);
				var (bytes, disasm) = nes.Cpu.Disassemble(address);
				disassembly[i] = (address, bytes, disasm);
				j += bytes.Count(x => x != null);
			}

			if (ImGui.BeginTable("disasm", 3, ImGuiTableFlags.BordersInnerV))
			{
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 50f);
				ImGui.TableSetupColumn("Bytes", ImGuiTableColumnFlags.WidthFixed, 75f);
				ImGui.TableSetupColumn("Disassembly", ImGuiTableColumnFlags.WidthFixed, 150f);
				ImGui.TableHeadersRow();

				if (disasmLines >= 0)
				{
					var clipper = new ImGuiListClipper();
					clipper.Begin(disasmLines);

					while (clipper.Step())
					{
						for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						{
							var (address, bytes, disasm) = disassembly[i];
							if (string.IsNullOrWhiteSpace(disasm)) continue;

							var isCurrentPc = address == nes.Cpu.PC;
							if (isCurrentPc)
								ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 1f, 0.25f, 1f));

							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex(0);
							ImGui.Text($"{address:X4}");
							ImGui.TableSetColumnIndex(1);
							ImGui.Text($"{string.Join(' ', bytes.Select(x => x != null ? $"{x:X2}" : "  "))}");
							ImGui.TableSetColumnIndex(2);
							ImGui.Text(disasm);

							if (isCurrentPc)
								ImGui.PopStyleColor();
						}
					}
					clipper.End();
				}
				ImGui.EndTable();
			}

			ImGui.End();
		}
	}
}
