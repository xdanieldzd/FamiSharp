namespace FamiSharp.Emulation.Ppu
{
	public class RegisterControl()
	{
		public bool EnableNmi { get; set; } = false;
		public bool SlaveMode { get; set; } = false;
		public bool EnableLargeSprites { get; set; } = false;
		public int BgPatternBaseAddress { get; set; } = 0;
		public int SprPatternBaseAddress { get; set; } = 0;
		public bool AddressIncrementIsVertical { get; set; } = false;
		public bool NametableY { get; set; } = false;
		public bool NametableX { get; set; } = false;

		public RegisterControl(RegisterControl status) : this()
		{
			EnableNmi = status.EnableNmi;
			SlaveMode = status.SlaveMode;
			EnableLargeSprites = status.EnableLargeSprites;
			BgPatternBaseAddress = status.BgPatternBaseAddress;
			SprPatternBaseAddress = status.SprPatternBaseAddress;
			AddressIncrementIsVertical = status.AddressIncrementIsVertical;
			NametableY = status.NametableY;
			NametableX = status.NametableX;
		}

		public static implicit operator byte(RegisterControl status) => (byte)(
			(status.EnableNmi ? 1 << 7 : 0) |
			(status.SlaveMode ? 1 << 6 : 0) |
			(status.EnableLargeSprites ? 1 << 5 : 0) |
			(((status.BgPatternBaseAddress >> 12) & 0b1) << 4) |
			(((status.SprPatternBaseAddress >> 12) & 0b1) << 3) |
			(status.AddressIncrementIsVertical ? 1 << 1 : 0) |
			(status.NametableY ? 1 << 1 : 0) |
			(status.NametableX ? 1 << 0 : 0));

		public static implicit operator RegisterControl(byte status) => new()
		{
			EnableNmi = (status & 1 << 7) != 0,
			SlaveMode = (status & 1 << 6) != 0,
			EnableLargeSprites = (status & 1 << 5) != 0,
			BgPatternBaseAddress = ((status >> 4) & 0b1) << 12,
			SprPatternBaseAddress = ((status >> 3) & 0b1) << 12,
			AddressIncrementIsVertical = (status & 1 << 2) != 0,
			NametableY = (status & 1 << 1) != 0,
			NametableX = (status & 1 << 0) != 0,
		};
	}
}
