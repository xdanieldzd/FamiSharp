namespace FamiSharp.Emulation.Cartridges.Mappers
{
	public abstract class BaseMapper(int numPrgBanks, int numChrBanks)
	{
		public int NumPrgBanks { get; private set; } = numPrgBanks;
		public int NumChrBanks { get; private set; } = numChrBanks;

		public virtual void Reset() { }

		public virtual NametableArrangement NametableArrangement => NametableArrangement.Unset;
		public virtual bool IsIrqPending => false;
		public virtual void ClearIrq() { }

		public virtual void OnEndOfScanline() { }

		public abstract bool MapCpuRead(ushort address, ref int mappedAddress, ref byte value);
		public abstract bool MapCpuWrite(ushort address, ref int mappedAddress, byte value);

		public virtual bool MapPpuRead(ushort address, ref int mappedAddress)
		{
			if (address is >= 0x0000 and < 0x2000) { mappedAddress = address; return true; }
			else return false;
		}

		public virtual bool MapPpuWrite(ushort address, ref int mappedAddress)
		{
			if (address is >= 0x0000 and < 0x2000 && NumChrBanks == 0) { mappedAddress = address; return true; }
			else return false;
		}
	}
}
