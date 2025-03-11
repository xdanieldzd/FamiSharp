namespace FamiSharp.Emulation.Ppu
{
	public class RegisterMask()
	{
		public bool EmphasizeBlue { get; set; }
		public bool EmphasizeGreen { get; set; }
		public bool EmphasizeRed { get; set; }
		public bool ShowSprites { get; set; }
		public bool ShowBackground { get; set; }
		public bool ShowSprLeftBorder { get; set; }
		public bool ShowBgLeftBorder { get; set; }
		public bool Grayscale { get; set; }

		public int Emphasis => (EmphasizeRed ? 1 << 0 : 0) | (EmphasizeGreen ? 1 << 1 : 0) | (EmphasizeBlue ? 1 << 2 : 0);

		public RegisterMask(RegisterMask mask) : this()
		{
			Grayscale = mask.Grayscale;
			ShowBgLeftBorder = mask.ShowBgLeftBorder;
			ShowSprLeftBorder = mask.ShowSprLeftBorder;
			ShowBackground = mask.ShowBackground;
			ShowSprites = mask.ShowSprites;
			EmphasizeRed = mask.EmphasizeRed;
			EmphasizeGreen = mask.EmphasizeGreen;
			EmphasizeBlue = mask.EmphasizeBlue;
		}

		public static implicit operator byte(RegisterMask mask) => (byte)(
			(mask.EmphasizeBlue ? 1 << 7 : 0) |
			(mask.EmphasizeGreen ? 1 << 6 : 0) |
			(mask.EmphasizeRed ? 1 << 5 : 0) |
			(mask.ShowSprites ? 1 << 4 : 0) |
			(mask.ShowBackground ? 1 << 3 : 0) |
			(mask.ShowSprLeftBorder ? 1 << 2 : 0) |
			(mask.ShowBgLeftBorder ? 1 << 1 : 0) |
			(mask.Grayscale ? 1 << 0 : 0));

		public static implicit operator RegisterMask(byte mask) => new()
		{
			EmphasizeBlue = (mask & 1 << 7) != 0,
			EmphasizeGreen = (mask & 1 << 6) != 0,
			EmphasizeRed = (mask & 1 << 5) != 0,
			ShowSprites = (mask & 1 << 4) != 0,
			ShowBackground = (mask & 1 << 3) != 0,
			ShowSprLeftBorder = (mask & 1 << 2) != 0,
			ShowBgLeftBorder = (mask & 1 << 1) != 0,
			Grayscale = (mask & 1 << 0) != 0
		};
	}
}
