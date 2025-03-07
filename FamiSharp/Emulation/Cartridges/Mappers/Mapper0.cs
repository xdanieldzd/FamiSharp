namespace FamiSharp.Emulation.Cartridges.Mappers
{
	public class Mapper0(int numPrgBanks, int numChrBanks) : BaseMapper(numPrgBanks, numChrBanks)
	{
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
				mappedAddress = (ushort)(address & (NumPrgBanks > 1 ? 0x7FFF : 0x3FFF));
				return true;
			}
			else
				return false;
		}
	}
}
