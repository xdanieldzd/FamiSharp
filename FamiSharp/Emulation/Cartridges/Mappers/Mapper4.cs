namespace FamiSharp.Emulation.Cartridges.Mappers
{
	public class Mapper4(int numPrgBanks, int numChrBanks) : BaseMapper(numPrgBanks, numChrBanks)
	{
		/*
		 * https://www.nesdev.org/wiki/MMC3
		 * 
		 * CPU $6000-$7FFF: 8 KB PRG RAM bank (optional)
		 * CPU $8000-$9FFF (or $C000-$DFFF): 8 KB switchable PRG ROM bank
		 * CPU $A000-$BFFF: 8 KB switchable PRG ROM bank
		 * CPU $C000-$DFFF (or $8000-$9FFF): 8 KB PRG ROM bank, fixed to the second-last bank
		 * CPU $E000-$FFFF: 8 KB PRG ROM bank, fixed to the last bank
		 * PPU $0000-$07FF (or $1000-$17FF): 2 KB switchable CHR bank
		 * PPU $0800-$0FFF (or $1800-$1FFF): 2 KB switchable CHR bank
		 * PPU $1000-$13FF (or $0000-$03FF): 1 KB switchable CHR bank
		 * PPU $1400-$17FF (or $0400-$07FF): 1 KB switchable CHR bank
		 * PPU $1800-$1BFF (or $0800-$0BFF): 1 KB switchable CHR bank
		 * PPU $1C00-$1FFF (or $0C00-$0FFF): 1 KB switchable CHR bank
		 */

		/* Optional PRG RAM */
		readonly byte[] prgRam = new byte[0x2000];

		/* Bank select / bank data registers */
		readonly byte[] bankRegisters = [0, 0, 0, 0, 0, 0, 0, 0];
		int bankRegisterIndex;
		int prgRomBankMode;
		int chrA12Inversion;
		readonly int[] chrBankAddresses = [0, 0, 0, 0, 0, 0, 0, 0];
		readonly int[] prgBankAddresses = [0, 0, 0, 0];

		/* Mirroring / PRG RAM write-protect */
		int nametableArrangement;
		// TODO: write protect stuff

		/* Interrupts */
		byte irqCounterReload;
		int irqCounter;
		bool irqEnable;
		bool irqPending;
		bool irqReloadPending;

		public override void Reset()
		{
			Array.Clear(prgRam);

			Array.Clear(bankRegisters);
			bankRegisterIndex = 0;
			prgRomBankMode = 0;
			chrA12Inversion = 0;
			Array.Clear(chrBankAddresses);
			Array.Clear(prgBankAddresses);

			nametableArrangement = 0;

			irqCounterReload = 0;
			irqCounter = 0;
			irqEnable = false;
			irqPending = false;
			irqReloadPending = false;

			bankRegisters[6] = 0;
			bankRegisters[7] = 1;
			prgBankAddresses[0] = 0 << 13;
			prgBankAddresses[1] = 1 << 13;
			prgBankAddresses[2] = ((NumPrgBanks * 2) - 2) << 13;
			prgBankAddresses[3] = ((NumPrgBanks * 2) - 1) << 13;
		}

		public override NametableArrangement NametableArrangement => nametableArrangement == 0 ? NametableArrangement.VerticalMirror : NametableArrangement.HorizontalMirror;
		public override bool IsIrqPending => irqEnable && irqPending;
		public override void ClearIrq() => irqPending = false;

		public override void OnEndOfScanline()
		{
			if (irqCounter < 0 || irqReloadPending)
			{
				irqCounter = irqCounterReload;
				irqReloadPending = false;
			}
			else
				irqCounter--;

			if (irqCounter <= 0 && irqEnable)
				irqPending = true;
		}

		public override bool MapCpuRead(ushort address, ref int mappedAddress, ref byte value)
		{
			if (address is >= 0x6000 and < 0x8000)
			{
				mappedAddress = -1;
				value = prgRam[address & 0x1FFF];
				return true;
			}
			else if (address is >= 0x8000)
			{
				mappedAddress = prgBankAddresses[(address >> 13) & 0b011] + (address & 0x1FFF);
				return true;
			}

			return false;
		}

		public override bool MapCpuWrite(ushort address, ref int mappedAddress, byte value)
		{
			if (address is >= 0x6000 and < 0x8000)
			{
				prgRam[address & 0x1FFF] = value;
				return true;
			}
			else if (address is >= 0x8000)
			{
				switch (address & 0xE001)
				{
					case 0x8000:
						bankRegisterIndex = value & 0b111;
						prgRomBankMode = (value >> 6) & 0b1;
						chrA12Inversion = (value >> 7) & 0b1;
						break;
					case 0x8001:
						bankRegisters[bankRegisterIndex] = value;

						if (prgRomBankMode == 0)
						{
							prgBankAddresses[0] = (bankRegisters[6] & 0x3F) << 13;
							prgBankAddresses[1] = (bankRegisters[7] & 0x3F) << 13;
							prgBankAddresses[2] = ((NumPrgBanks * 2) - 2) << 13;
							prgBankAddresses[3] = ((NumPrgBanks * 2) - 1) << 13;
						}
						else
						{
							prgBankAddresses[0] = ((NumPrgBanks * 2) - 2) << 13;
							prgBankAddresses[1] = (bankRegisters[7] & 0x3F) << 13;
							prgBankAddresses[2] = (bankRegisters[6] & 0x3F) << 13;
							prgBankAddresses[3] = ((NumPrgBanks * 2) - 1) << 13;

						}
						if (chrA12Inversion == 0)
						{
							chrBankAddresses[0] = ((bankRegisters[0] & 0xFE) + 0) << 10;
							chrBankAddresses[1] = ((bankRegisters[0] & 0xFE) + 1) << 10;
							chrBankAddresses[2] = ((bankRegisters[1] & 0xFE) + 0) << 10;
							chrBankAddresses[3] = ((bankRegisters[1] & 0xFE) + 1) << 10;
							chrBankAddresses[4] = bankRegisters[2] << 10;
							chrBankAddresses[5] = bankRegisters[3] << 10;
							chrBankAddresses[6] = bankRegisters[4] << 10;
							chrBankAddresses[7] = bankRegisters[5] << 10;
						}
						else
						{
							chrBankAddresses[0] = bankRegisters[2] << 10;
							chrBankAddresses[1] = bankRegisters[3] << 10;
							chrBankAddresses[2] = bankRegisters[4] << 10;
							chrBankAddresses[3] = bankRegisters[5] << 10;
							chrBankAddresses[4] = ((bankRegisters[0] & 0xFE) + 0) << 10;
							chrBankAddresses[5] = ((bankRegisters[0] & 0xFE) + 1) << 10;
							chrBankAddresses[6] = ((bankRegisters[1] & 0xFE) + 0) << 10;
							chrBankAddresses[7] = ((bankRegisters[1] & 0xFE) + 1) << 10;
						}
						break;
					case 0xA000:
						nametableArrangement = value & 0b1;
						break;
					case 0xA001:
						// TODO ram prot
						break;
					case 0xC000:
						irqCounterReload = value;
						break;
					case 0xC001:
						irqCounter = 0;
						irqReloadPending = true;
						break;
					case 0xE000:
						irqEnable = false;
						irqPending = false;
						break;
					case 0xE001:
						irqEnable = true;
						break;
				}

				return true;
			}

			return false;
		}

		public override bool MapPpuRead(ushort address, ref int mappedAddress)
		{
			if (address is >= 0x0000 and < 0x2000)
			{
				mappedAddress = chrBankAddresses[(address >> 10) & 0b111] + (address & 0x03FF);
				return true;
			}

			return false;
		}

		public override bool MapPpuWrite(ushort address, ref int mappedAddress)
		{
			return false;
		}
	}
}
