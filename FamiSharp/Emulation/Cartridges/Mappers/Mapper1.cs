namespace FamiSharp.Emulation.Cartridges.Mappers
{
	public class Mapper1(int numPrgBanks, int numChrBanks) : BaseMapper(numPrgBanks, numChrBanks)
	{
		/*
		 * https://www.nesdev.org/wiki/MMC1
		 * 
		 * CPU $6000-$7FFF: 8 KB PRG RAM bank, (optional)
		 * CPU $8000-$BFFF: 16 KB PRG ROM bank, either switchable or fixed to the first bank
		 * CPU $C000-$FFFF: 16 KB PRG ROM bank, either fixed to the last bank or switchable
		 * PPU $0000-$0FFF: 4 KB switchable CHR bank
		 * PPU $1000-$1FFF: 4 KB switchable CHR bank
		 */

		/* Optional PRG RAM */
		readonly byte[] prgRam = new byte[0x2000];

		/* Load register */
		byte registerLoad = 0;
		int registerLoadWriteCount = 0;

		/* Control register */
		int nametableArrangement = 0;   /* 0: single-screen, lower bank; 1: single-screen, upper bank; 2: vertical mirror; 3: horizontal mirror */
		int prgRomBankMode = 0;
		int chrRomBankMode = 0;

		/* CHR banks (4k & 8k modes) */
		int chr4kBank0 = 0;
		int chr4kBank1 = 0;
		int chr8kBank = 0;

		/* PRG banks (16k & 32k modes) */
		int prg16kBank0 = 0;
		int prg16kBank1 = 0;
		int prg32kBank = 0;

		public override NametableArrangement NametableArrangement => nametableArrangement switch
		{
			0 => NametableArrangement.OneScreenLowerBank,
			1 => NametableArrangement.OneScreenUpperBank,
			2 => NametableArrangement.VerticalMirror,
			3 => NametableArrangement.HorizontalMirror,
			_ => NametableArrangement.Unset
		};

		public override void Reset()
		{
			Array.Clear(prgRam);

			registerLoad = 0b10000;
			registerLoadWriteCount = 0;

			nametableArrangement = 0;
			prgRomBankMode = 3;
			chrRomBankMode = 1;

			chr4kBank0 = chr4kBank1 = chr8kBank = 0;
			prg16kBank0 = 0;
			prg16kBank1 = NumPrgBanks - 1;
			prg32kBank = 0;
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
				switch (prgRomBankMode)
				{
					case 0:
					case 1:
						mappedAddress = (prg32kBank * 0x8000) + (address & 0x7FFF);
						return true;
					case 2:
					case 3:
						if (address is >= 0x8000 and < 0xC000)
						{
							mappedAddress = (prg16kBank0 * 0x4000) + (address & 0x3FFF);
							return true;
						}
						else if (address is >= 0xC000)
						{
							mappedAddress = (prg16kBank1 * 0x4000) + (address & 0x3FFF);
							return true;
						}
						break;
				}
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
				if ((value & 0x80) == 0x80)
				{
					/* Reset shift register */
					registerLoad = 0b10000;
					registerLoadWriteCount = 0;
					prgRomBankMode = 3;
				}
				else
				{
					/* Shift bit into register */
					registerLoad = (byte)(registerLoad >> 1 | (value & 0b1) << 4);
					registerLoadWriteCount++;

					/* Shift register full? */
					if (registerLoadWriteCount == 5)
					{
						/* Write to destination register */
						switch ((address >> 13) & 0b11)
						{
							case 0:
								/* Control register */
								nametableArrangement = (registerLoad >> 0) & 0b11;
								prgRomBankMode = (registerLoad >> 2) & 0b11;
								chrRomBankMode = (registerLoad >> 4) & 0b1;
								break;
							case 1:
								/* CHR bank 0 */
								if (chrRomBankMode != 0)
								{
									/* Switch two 4k banks (0x0000 to 0x0FFF, 0x1000 to 0x1FFF) */
									chr4kBank0 = registerLoad & 0b11111;
								}
								else
								{
									/* Switch one 8k bank (0x0000 to 0x1FFF) */
									chr8kBank = registerLoad & 0b11110;
								}
								break;
							case 2:
								/* CHR bank 1 */
								if (chrRomBankMode != 0)
								{
									chr4kBank1 = registerLoad & 0b11111;
								}
								break;
							case 3:
								/* PRG bank */
								switch (prgRomBankMode)
								{
									case 0:
									case 1:
										/* Switch one 32k bank (0x8000 to 0xFFFF) */
										prg32kBank = (registerLoad & 0b01110) >> 1;
										break;
									case 2:
										/* Switch one 16k bank (0xC000 to 0xFFFF), set first bank (0x8000 to 0xBFFF) */
										prg16kBank0 = 0;
										prg16kBank1 = registerLoad & 0b01111;
										break;
									case 3:
										/* Switch one 16k bank (0x8000 to 0xBFFF), set last bank (0xC000 to 0xFFFF) */
										prg16kBank0 = registerLoad & 0b01111;
										prg16kBank1 = NumPrgBanks - 1;
										break;
								}
								break;
						}

						/* Reset shift register */
						registerLoad = 0b10000;
						registerLoadWriteCount = 0;
					}
				}
			}

			return false;
		}

		public override bool MapPpuRead(ushort address, ref int mappedAddress)
		{
			if (address is >= 0x0000 and < 0x2000)
			{
				if (NumChrBanks == 0)
				{
					mappedAddress = address;
					return true;
				}
				else
				{
					if (chrRomBankMode != 0)
					{
						if (address is >= 0x0000 and < 0x1000)
						{
							mappedAddress = (chr4kBank0 * 0x1000) + (address & 0x0FFF);
							return true;
						}
						else if (address is >= 0x1000 and < 0x2000)
						{
							mappedAddress = (chr4kBank1 * 0x1000) + (address & 0x0FFF);
							return true;
						}
					}
					else
					{
						mappedAddress = (chr8kBank * 0x2000) + (address & 0x1FFF);
						return true;
					}
				}
			}

			return false;
		}
	}
}
