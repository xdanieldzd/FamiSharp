namespace FamiSharp.Emulation.Ppu
{
	public class Sprite
	{
		public byte Y { get; set; } = 0xFF;
		public byte TileIndex { get; set; } = 0xFF;
		public byte Attributes { get; set; } = 0xFF;
		public byte X { get; set; } = 0xFF;

		public bool VerticalFlip => (Attributes & 0x80) != 0;
		public bool HorizontalFlip => (Attributes & 0x40) != 0;
		public bool IsBehindBackground => (Attributes & 0x20) != 0;
		public int PaletteIndex => Attributes & 0b11;
	}
}
