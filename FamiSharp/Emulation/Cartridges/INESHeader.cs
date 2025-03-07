namespace FamiSharp.Emulation.Cartridges
{
	public class INESHeader(BinaryReader reader)
	{
		public string Magic { get; private set; } = new string(reader.ReadBytes(4).Select(x => (char)x).ToArray());
		public byte PrgRomSize { get; private set; } = reader.ReadByte();
		public byte ChrRomSize { get; private set; } = reader.ReadByte();
		public byte[] Flags { get; private set; } = reader.ReadBytes(5);
		public byte[] Unused { get; private set; } = reader.ReadBytes(5);

		/* Flags 6 */
		public NametableArrangement NametableArrangement => (Flags[0] & 0b00000001) != 0 ? NametableArrangement.VerticalMirror : NametableArrangement.HorizontalMirror;
		public bool HasPersistantMemory => (Flags[0] & 0b00000010) != 0;
		public bool HasTrainer => (Flags[0] & 0b00000100) != 0;
		public bool UsesAlternateNametableLayout => (Flags[0] & 0b00001000) != 0;
		public int MapperIdLowerBits => (Flags[0] & 0b11110000) >> 4;

		/* Flags 7 */
		public bool IsVsUnisystemRom => (Flags[1] & 0b00000001) != 0;
		public bool IsPlayChoice10Rom => (Flags[1] & 0b00000010) != 0;
		public int MapperIdUpperBits => (Flags[1] & 0b11110000) >> 4;

		/* Flags 8 -- rarely used */
		public byte PrgRamSize => Flags[2];

		/* Flags 9 -- rarely used */
		public bool IsPalRom => (Flags[3] & 0b00000001) != 0;

		/* Flags 10 -- unofficial, rarely used */
		public int TvSystem => Flags[4] & 0b00000011;
		public bool HasPrgRam => (Flags[4] & 0b00010000) == 0;
		public bool HasBusConflicts => (Flags[4] & 0b00100000) != 0;

		/* Shortcuts */
		public bool IsValidNesRom => Magic == "NES\x1A";
		public bool IsNes2FormatHeader => IsValidNesRom && (Flags[1] & 0b00001100) == 8;
		public int MapperId => (MapperIdUpperBits << 4) | MapperIdLowerBits;
	}
}
