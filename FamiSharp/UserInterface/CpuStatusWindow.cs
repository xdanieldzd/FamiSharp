using FamiSharp.Emulation;
using FamiSharp.Emulation.Cpu;
using Hexa.NET.ImGui;

namespace FamiSharp.UserInterface
{
	public class CpuStatusWindow : WindowBase
	{
		public override string Title => "CPU Status";

		protected override void DrawWindow(object? userData)
		{
			if (userData is not NesSystem nes) return;

			if (!ImGui.Begin(Title, ref windowOpen))
			{
				ImGui.End();
				return;
			}

			static bool registerEditor(string label, int value, int length, out int newValue)
			{
				newValue = value;

				var valueFormat = $"{{0:X{length}}}";
				var valueString = string.Format(valueFormat, value);
				if (ImGui.InputText(label, ref valueString, (nuint)(length + 1), ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue))
					return int.TryParse(valueString, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out newValue);

				return false;
			}

			static bool statusEditor(ProcessorStatus value, out ProcessorStatus newValue)
			{
				newValue = new(value);

				if (ImGui.BeginTable("flags", 6, ImGuiTableFlags.SizingStretchSame))
				{
					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);
					var n = value.N;
					if (ImGui.Checkbox("N", ref n)) newValue.N = n;

					ImGui.TableSetColumnIndex(1);
					var v = value.V;
					if (ImGui.Checkbox("V", ref v)) newValue.V = v;

					ImGui.TableSetColumnIndex(2);
					var d = value.D;
					if (ImGui.Checkbox("D", ref d)) newValue.D = d;

					ImGui.TableSetColumnIndex(3);
					var i = value.I;
					if (ImGui.Checkbox("I", ref i)) newValue.I = i;

					ImGui.TableSetColumnIndex(4);
					var z = value.Z;
					if (ImGui.Checkbox("Z", ref z)) newValue.Z = z;

					ImGui.TableSetColumnIndex(5);
					var c = value.C;
					if (ImGui.Checkbox("C", ref c)) newValue.C = c;

					ImGui.EndTable();
				}

				return !value.Equals(newValue);
			}

			ImGui.SeparatorText("Registers");

			if (ImGui.BeginTable("regs", 2, ImGuiTableFlags.SizingStretchSame))
			{
				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				if (registerEditor("PC", nes.Cpu.PC, 4, out int newPc)) nes.Cpu.PC = (ushort)newPc;
				ImGui.TableSetColumnIndex(1);
				if (registerEditor("A", nes.Cpu.A, 2, out int newA)) nes.Cpu.A = (byte)newA;

				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				if (registerEditor("S", nes.Cpu.S, 2, out int newS)) nes.Cpu.S = (byte)newS;
				ImGui.TableSetColumnIndex(1);
				if (registerEditor("X", nes.Cpu.X, 2, out int newX)) nes.Cpu.X = (byte)newX;

				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				if (registerEditor("P", nes.Cpu.P, 2, out int newP)) nes.Cpu.P = (byte)newP;
				ImGui.TableSetColumnIndex(1);
				if (registerEditor("Y", nes.Cpu.Y, 2, out int newY)) nes.Cpu.Y = (byte)newY;

				ImGui.EndTable();
			}

			ImGui.SeparatorText("Status (P)");
			if (statusEditor(nes.Cpu.P, out ProcessorStatus newP2)) nes.Cpu.P = newP2;

			ImGui.End();
		}
	}
}
