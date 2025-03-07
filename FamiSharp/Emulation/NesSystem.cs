using FamiSharp.Emulation.Cartridges;

namespace FamiSharp.Emulation
{
	public class NesSystem
	{
		public NesCpu Cpu { get; private set; }
		public Ppu.Ppu Ppu { get; private set; }
		public byte[] InternalRam { get; private set; }
		public Cartridge? Cartridge { get; private set; }

		public (byte Page, byte Address, bool InProgress, byte Data, bool Dummy) OAMDMA = (0, 0, false, 0, true);

		public uint Ticks { get; private set; } = 0;

		readonly byte[] controllerState = new byte[2];

		public event EventHandler<InputEventArgs>? RequestInput;
		readonly InputEventArgs inputEventArgs = new();

		readonly bool isReady = false;

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

			OAMDMA = (0, 0, false, 0, true);

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
				if (OAMDMA.InProgress)
				{
					if (OAMDMA.Dummy)
					{
						if ((Ticks % 2) == 1)
							OAMDMA.Dummy = false;
					}
					else
					{
						if ((Ticks % 2) == 0)
							OAMDMA.Data = Read((ushort)((OAMDMA.Page << 8) | OAMDMA.Address));
						else
						{
							Ppu.OamData[OAMDMA.Address] = OAMDMA.Data;
							OAMDMA.Address++;
							if (OAMDMA.Address == 0)
							{
								OAMDMA.InProgress = false;
								OAMDMA.Dummy = true;
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
					OAMDMA = (value, 0, true, OAMDMA.Data, OAMDMA.Dummy);
				else if (address >= 0x4016 && address < 0x4018)
				{
					RequestInput?.Invoke(this, inputEventArgs);
					controllerState[address & 0x0001] = inputEventArgs.ControllerData[address & 0x0001];
				}
			}
		}
	}

	public class InputEventArgs : EventArgs
	{
		public byte[] ControllerData { get; set; } = [0, 0];
	}
}
