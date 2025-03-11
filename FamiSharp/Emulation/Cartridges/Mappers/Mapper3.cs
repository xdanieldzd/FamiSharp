namespace FamiSharp.Emulation.Cartridges.Mappers
{
	public class Mapper3(int numPrgBanks, int numChrBanks) : BaseMapper(numPrgBanks, numChrBanks)
	{
		int chrBankSelect;

		public override void Reset()
		{
			chrBankSelect = 0;
		}

		public override bool MapCpuRead(ushort address, ref int mappedAddress, ref byte value)
		{
			if (address is >= 0x8000)
			{
				mappedAddress = (ushort)(address & (NumPrgBanks > 1 ? 0x7FFF : 0x3FFF));
				return true;
			}
			else
				return false;
		}

		public override bool MapCpuWrite(ushort address, ref int mappedAddress, byte value)
		{
			if (address is >= 0x8000)
			{
				/* NesDev: "CNROM only implements the lowest 2 bits, capping it at 32 KiB CHR. Other boards may implement 4 or more bits for larger CHR." */
				chrBankSelect = value & 0b11;
				return true;
			}

			return false;
		}

		public override bool MapPpuRead(ushort address, ref int mappedAddress)
		{
			if (address is >= 0x0000 and < 0x2000)
			{
				mappedAddress = (chrBankSelect * 0x2000) + (address & 0x1FFF);
				return true;
			}
			else
				return false;
		}
	}
}
