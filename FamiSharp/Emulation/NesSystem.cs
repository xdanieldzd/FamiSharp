using FamiSharp.Emulation.Cartridges;

namespace FamiSharp.Emulation
{
	public class NesSystem
	{
		public NesCpu Cpu { get; private set; }
		public Ppu.Ppu Ppu { get; private set; }
		public byte[] InternalRam { get; private set; }
		public Cartridge? Cartridge { get; private set; }

		public OamDma OamDma { get; private set; } = new();

		public uint Ticks { get; private set; }

		readonly byte[] controllerState = new byte[2];

		public event EventHandler<InputEventArgs>? RequestInput;
		readonly InputEventArgs inputEventArgs = new();

		readonly bool isReady;

		public NesSystem()
		{
			Cpu = new(this);
			Ppu = new(this);

			InternalRam = new byte[0x0800];

			isReady = true;
		}

		public void LoadCartridge(Cartridge cartridge)
		{
			Cartridge = cartridge;
		}

		public void Reset()
		{
			Cpu.Reset();
			Ppu.Reset();
			Array.Clear(InternalRam);

			OamDma = OamDma.Empty;

			Ticks = 0;
		}

		public void RunFrame()
		{
			while (!Tick()) { }
			do { Tick(); } while (Cpu.Cycles != 0);
		}

		public bool Tick()
		{
			if (!isReady) return false;

			var frameComplete = Ppu.Tick();

			if ((Ticks % 3) == 0)
			{
				if (OamDma.InProgress)
				{
					if (OamDma.Dummy)
					{
						if ((Ticks % 2) == 1)
							OamDma.Dummy = false;
					}
					else
					{
						if ((Ticks % 2) == 0)
							OamDma.Data = Read((ushort)((OamDma.Page << 8) | OamDma.Address));
						else
						{
							Ppu.OamData[OamDma.Address] = OamDma.Data;
							OamDma.Address++;
							if (OamDma.Address == 0)
							{
								OamDma.InProgress = false;
								OamDma.Dummy = true;
							}
						}
					}
				}
				else
					Cpu.Tick();
			}

			if (Ppu.NmiOccured)
			{
				Ppu.NmiOccured = false;
				Cpu.NMI();
			}

			if (Cartridge != null && Cartridge.Mapper != null && Cartridge.Mapper.IsIrqPending)
			{
				Cartridge.Mapper.ClearIrq();
				Cpu.IRQ();
			}

			Ticks++;

			return frameComplete;
		}

		public byte Read(ushort address)
		{
			if (Cartridge == null) return 0;

			var value = (byte)0;

			if (!Cartridge.CpuRead(address, ref value))
			{
				if (address >= 0x0000 && address < 0x2000)
					value = InternalRam[address & 0x07FF];
				else if (address >= 0x2000 && address < 0x4000)
					value = Ppu.ExternalRead((ushort)(address & 0x0007));
				else if (address >= 0x4016 && address < 0x4018)
				{
					value = (byte)((controllerState[address & 0x0001] >> 7) & 0b1);
					controllerState[address & 0x0001] <<= 1;
				}
			}
			return value;
		}

		public void Write(ushort address, byte value)
		{
			if (Cartridge == null) return;

			if (!Cartridge.CpuWrite(address, value))
			{
				if (address >= 0x0000 && address < 0x2000)
					InternalRam[address & 0x07FF] = value;
				else if (address >= 0x2000 && address < 0x4000)
					Ppu.ExternalWrite((ushort)(address & 0x0007), value);
				else if (address == 0x4014)
					OamDma = new() { Page = value, Address = 0, InProgress = true, Data = OamDma.Data, Dummy = OamDma.Dummy };
				else if (address >= 0x4016 && address < 0x4018)
				{
					RequestInput?.Invoke(this, inputEventArgs);
					controllerState[address & 0x0001] = inputEventArgs.ControllerData[address & 0x0001];
				}
			}
		}
	}

	public class OamDma
	{
		public byte Page { get; set; }
		public byte Address { get; set; }
		public bool InProgress { get; set; }
		public byte Data { get; set; }
		public bool Dummy { get; set; }

		public static OamDma Empty => new() { Page = 0, Address = 0, InProgress = false, Data = 0, Dummy = true };
	}

	public class InputEventArgs : EventArgs
	{
		public byte[] ControllerData { get; set; } = [0, 0];
	}
}
