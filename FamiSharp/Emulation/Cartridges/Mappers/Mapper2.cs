namespace FamiSharp.Emulation.Cartridges.Mappers
{
	public class Mapper2(int numPrgBanks, int numChrBanks) : BaseMapper(numPrgBanks, numChrBanks)
	{
		int prgBankSelect;

		public override void Reset()
		{
			prgBankSelect = 0;
		}

		public override bool MapCpuRead(ushort address, ref int mappedAddress, ref byte value)
		{
			if (address is >= 0x8000)
			{
				if (address is >= 0x8000 and < 0xC000)
					mappedAddress = (prgBankSelect * 0x4000) + (address & 0x3FFF);
				else if (address >= 0xC000)
					mappedAddress = ((NumPrgBanks - 1) * 0x4000) + (address & 0x3FFF);
				return true;
			}
			else
				return false;
		}

		public override bool MapCpuWrite(ushort address, ref int mappedAddress, byte value)
		{
			if (address is >= 0x8000)
			{
				/* NesDev: "UNROM uses bits 2-0; UOROM uses bits 3-0" */
				prgBankSelect = value & 0b1111;
				return true;
			}

			return false;
		}
	}
}
