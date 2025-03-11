using FamiSharp.Emulation.Cartridges.Mappers;
using FamiSharp.Exceptions;

namespace FamiSharp.Emulation.Cartridges
{
	public enum NametableArrangement { Unset = -1, HorizontalMirror, VerticalMirror, OneScreenLowerBank, OneScreenUpperBank }

	public class Cartridge
	{
		public INESHeader Header { get; private set; }

		public byte[] PrgMemory { get; private set; } = [];
		public byte[] ChrMemory { get; private set; } = [];

		public BaseMapper Mapper { get; private set; }

		public NametableArrangement NametableArrangement => (Mapper.NametableArrangement != NametableArrangement.Unset) ? Mapper.NametableArrangement : Header.NametableArrangement;

		public Cartridge(BinaryReader reader)
		{
			Header = new(reader);
			if (Header.IsValidNesRom)
			{
				if (Header.HasTrainer)
					reader.BaseStream.Seek(512, SeekOrigin.Current);

				PrgMemory = reader.ReadBytes(Header.PrgRomSize * 16 * 1024);
				if (Header.ChrRomSize != 0)
					ChrMemory = reader.ReadBytes(Header.ChrRomSize * 8 * 1024);
				else
					ChrMemory = new byte[0x2000];

				Mapper = Header.MapperId switch
				{
					1 => new Mapper1(Header.PrgRomSize, Header.ChrRomSize),
					2 => new Mapper2(Header.PrgRomSize, Header.ChrRomSize),
					3 => new Mapper3(Header.PrgRomSize, Header.ChrRomSize),
					4 => new Mapper4(Header.PrgRomSize, Header.ChrRomSize),
					_ => new Mapper0(Header.PrgRomSize, Header.ChrRomSize),
				};

				Mapper.Reset();
			}
			else
				throw new EmulationException("Selected file is not an NES ROM image.");
		}

		public bool CpuRead(ushort address, ref byte value)
		{
			var mappedAddress = 0;
			if (Mapper != null && Mapper.MapCpuRead(address, ref mappedAddress, ref value))
			{
				if (mappedAddress != -1)
					value = PrgMemory[mappedAddress & (PrgMemory.Length - 1)];
				return true;
			}
			else
				return false;
		}

		public bool CpuWrite(ushort address, byte value)
		{
			var mappedAddress = 0;
			if (Mapper != null && Mapper.MapCpuWrite(address, ref mappedAddress, value))
			{
				if (mappedAddress != -1)
					PrgMemory[mappedAddress & (PrgMemory.Length - 1)] = value;
				return true;
			}
			else
				return false;
		}

		public bool PpuRead(ushort address, ref byte value)
		{
			var mappedAddress = 0;
			if (Mapper != null && Mapper.MapPpuRead(address, ref mappedAddress))
			{
				value = ChrMemory[mappedAddress & (ChrMemory.Length - 1)];
				return true;
			}
			else
				return false;
		}

		public bool PpuWrite(ushort address, byte value)
		{
			var mappedAddress = 0;
			if (Mapper != null && Mapper.MapPpuWrite(address, ref mappedAddress))
			{
				ChrMemory[mappedAddress & (ChrMemory.Length - 1)] = value;
				return true;
			}
			else
				return false;
		}
	}
}
