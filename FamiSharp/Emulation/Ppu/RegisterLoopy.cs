namespace FamiSharp.Emulation.Ppu
{
	public class RegisterLoopy()
	{
		public int FineYScroll { get; set; } = 0;
		public bool NametableY { get; set; } = false;
		public bool NametableX { get; set; } = false;
		public int CoarseYScroll { get; set; } = 0;
		public int CoarseXScroll { get; set; } = 0;

		public RegisterLoopy(RegisterLoopy loopy) : this()
		{
			FineYScroll = loopy.FineYScroll;
			NametableY = loopy.NametableY;
			NametableX = loopy.NametableX;
			CoarseYScroll = loopy.CoarseYScroll;
			CoarseXScroll = loopy.CoarseXScroll;
		}

		public static implicit operator ushort(RegisterLoopy loopy) => (ushort)(
			((loopy.FineYScroll & 0b111) << 12) |
			(loopy.NametableY ? 1 << 11 : 0) |
			(loopy.NametableX ? 1 << 10 : 0) |
			((loopy.CoarseYScroll & 0b11111) << 5) |
			((loopy.CoarseXScroll & 0b11111) << 0));

		public static implicit operator RegisterLoopy(ushort loopy) => new()
		{
			FineYScroll = (loopy >> 12) & 0b111,
			NametableY = (loopy & 1 << 11) != 0,
			NametableX = (loopy & 1 << 10) != 0,
			CoarseYScroll = (loopy >> 5) & 0b11111,
			CoarseXScroll = (loopy >> 0) & 0b11111
		};
	}
}
