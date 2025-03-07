namespace FamiSharp.Emulation.Ppu
{
	public class RegisterStatus()
	{
		public bool VerticalBlank { get; set; } = false;
		public bool Sprite0Hit { get; set; } = false;
		public bool SpriteOverflow { get; set; } = false;

		public RegisterStatus(RegisterStatus status) : this()
		{
			VerticalBlank = status.VerticalBlank;
			Sprite0Hit = status.Sprite0Hit;
			SpriteOverflow = status.SpriteOverflow;
		}

		public static implicit operator byte(RegisterStatus status) => (byte)(
			(status.VerticalBlank ? 1 << 7 : 0) |
			(status.Sprite0Hit ? 1 << 6 : 0) |
			(status.SpriteOverflow ? 1 << 5 : 0));

		public static implicit operator RegisterStatus(byte status) => new()
		{
			VerticalBlank = (status & 1 << 7) != 0,
			Sprite0Hit = (status & 1 << 6) != 0,
			SpriteOverflow = (status & 1 << 5) != 0
		};
	}
}
