using FamiSharp.Exceptions;

namespace FamiSharp.Emulation.Ppu
{
	/* https://github.com/OneLoneCoder/olcNES/blob/ac5ce64cdb3a390a89d550c5f130682b37eeb080/Part%20%237%20-%20Mappers%20%26%20Basic%20Sounds/olc2C02.cpp */

	public class Ppu(NesSystem nes)
	{
		public int Cycle { get; private set; }
		public int Scanline { get; private set; }

		public event EventHandler<FramebufferEventArgs>? TransferFramebuffer;

		public RegisterControl RegisterControl { get; private set; } = new();
		public RegisterMask RegisterMask { get; private set; } = new();
		public RegisterStatus RegisterStatus { get; private set; } = new();

		public RegisterLoopy CurrentVramAddress { get; private set; } = new();
		public RegisterLoopy TemporaryVramAddress { get; private set; } = new();
		public int FineXScroll { get; private set; }
		public bool WriteLatchToggle { get; private set; }
		public byte DataBuffer { get; private set; }

		public byte[,] Nametables { get; private set; } = new byte[2, 1024];
		public byte[,] PatternTables { get; private set; } = new byte[2, 4096];
		public byte[] Palette { get; private set; } = new byte[32];

		public NextTileData NextTileData { get; private set; } = new();
		public BgShifter BgShifters { get; private set; } = new();

		public bool NmiOccured { get; set; }
		public bool IsOddFrame { get; set; }

		public byte[] OamData { get; private set; } = new byte[64 * 4];
		public byte OamAddress { get; private set; }
		public Sprite[] CurrentSprites { get; private set; } = [new(), new(), new(), new(), new(), new(), new(), new()];
		public int SpritesOnScanline { get; private set; }
		public (byte PatternLo, byte PatternHi)[] SpriteShifters { get; private set; } = [(0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0), (0, 0)];
		public bool Sprite0HitPossible { get; private set; }
		public bool Sprite0Rendered { get; private set; }

		public byte[] PaletteColors { get; private set; } = new byte[0x200 * 3];

		readonly byte[] framebuffer = new byte[256 * 240 * 4];

		public bool NametablesDirty { get; private set; }
		public bool PatternTablesDirty { get; private set; }
		public bool PaletteDirty { get; private set; }

		public void Initialize()
		{
			for (var i = 0; i < framebuffer.Length; i += 4)
			{
				framebuffer[i + 0] = 0;
				framebuffer[i + 1] = 0;
				framebuffer[i + 2] = 0;
				framebuffer[i + 3] = 255;
			}
		}

		public void LoadPalette(byte[] bytes)
		{
			if (bytes.Length != 0x600) throw new EmulationException("Error loading PPU palette: Size mismatch, expected 0x600 bytes. Required format is binary with RGB888 data for all emphasis variants.");
			Buffer.BlockCopy(bytes, 0, PaletteColors, 0, bytes.Length);
		}

		public void Reset()
		{
			FineXScroll = 0;
			WriteLatchToggle = false;
			DataBuffer = 0;
			Scanline = 0;
			Cycle = 0;
			NextTileData = NextTileData.Empty;
			BgShifters = BgShifter.Empty;
			NmiOccured = false;
			IsOddFrame = false;
			RegisterStatus = 0;
			RegisterMask = 0;
			RegisterControl = 0;
			CurrentVramAddress = 0;
			TemporaryVramAddress = 0;
		}

		public byte ExternalRead(ushort address)
		{
			var value = (byte)0;

			switch (address & 0x0007)
			{
				case 0x0002:
					/* PPUSTATUS */
					value = (byte)((RegisterStatus & 0xE0) | (DataBuffer & 0x1F));
					RegisterStatus.VerticalBlank = false;
					WriteLatchToggle = false;
					break;
				case 0x0004:
					/* OAMDATA */
					value = OamData[OamAddress];
					break;
				case 0x0007:
					/* PPUDATA */
					value = DataBuffer;
					DataBuffer = InternalRead(CurrentVramAddress);
					if (CurrentVramAddress >= 0x3F00) value = DataBuffer;
					CurrentVramAddress = (ushort)(CurrentVramAddress + (RegisterControl.AddressIncrementIsVertical ? 32 : 1));
					break;
				case 0x0000: /* PPUCTRL */
				case 0x0001: /* PPUMASK */
				case 0x0003: /* OAMADDR */
				case 0x0005: /* PPUSCROLL */
				case 0x0006: /* PPUADDR */
					/* Write-only! */
					break;
			}

			return value;
		}

		public void ExternalWrite(ushort address, byte value)
		{
			switch (address & 0x0007)
			{
				case 0x0000:
					/* PPUCTRL */
					RegisterControl = value;
					TemporaryVramAddress.NametableX = RegisterControl.NametableX;
					TemporaryVramAddress.NametableY = RegisterControl.NametableY;
					break;

				case 0x0001:
					/* PPUMASK */
					RegisterMask = value;
					break;

				case 0x0002:
					/* PPUSTATUS */
					/* Read-only! */
					break;

				case 0x0003:
					/* OAMADDR */
					OamAddress = value;
					break;

				case 0x0004:
					/* OAMDATA */
					OamData[OamAddress] = value;
					break;

				case 0x0005:
					/* PPUSCROLL */
					if (!WriteLatchToggle)
					{
						FineXScroll = value & 0x07;
						TemporaryVramAddress.CoarseXScroll = value >> 3;
					}
					else
					{
						TemporaryVramAddress.FineYScroll = value & 0x07;
						TemporaryVramAddress.CoarseYScroll = value >> 3;
					}
					WriteLatchToggle = !WriteLatchToggle;
					break;

				case 0x0006:
					/* PPUADDR */
					if (!WriteLatchToggle)
					{
						TemporaryVramAddress = (ushort)(((value & 0x3F) << 8) | (TemporaryVramAddress & 0x00FF));
					}
					else
					{
						TemporaryVramAddress = (ushort)((TemporaryVramAddress & 0xFF00) | value);
						CurrentVramAddress = new(TemporaryVramAddress);
					}
					WriteLatchToggle = !WriteLatchToggle;
					break;

				case 0x0007:
					/* PPUDATA */
					InternalWrite(CurrentVramAddress, value);
					CurrentVramAddress = (ushort)(CurrentVramAddress + (RegisterControl.AddressIncrementIsVertical ? 32 : 1));
					break;
			}
		}

		public byte InternalRead(ushort address)
		{
			var value = (byte)0;

			address &= 0x3FFF;

			if (nes.Cartridge != null && !nes.Cartridge.PpuRead(address, ref value))
			{
				if (address is >= 0x0000 and < 0x2000)
				{
					value = PatternTables[(address & 0x1000) >> 12, address & 0x0FFF];
				}
				else if (address is >= 0x2000 and < 0x3F00)
				{
					address &= 0x0FFF;

					switch (nes.Cartridge.NametableArrangement)
					{
						case Cartridges.NametableArrangement.VerticalMirror:
							if ((address is < 0x0400) || (address is >= 0x0800 and < 0x0C00))
								value = Nametables[0, address & 0x03FF];
							else if ((address is >= 0x0400 and < 0x0800) || (address is >= 0x0C00))
								value = Nametables[1, address & 0x03FF];
							break;

						case Cartridges.NametableArrangement.HorizontalMirror:
							if (address is < 0x0800)
								value = Nametables[0, address & 0x03FF];
							else
								value = Nametables[1, address & 0x03FF];
							break;

						case Cartridges.NametableArrangement.OneScreenLowerBank:
							value = Nametables[1, address & 0x03FF];
							break;

						case Cartridges.NametableArrangement.OneScreenUpperBank:
							value = Nametables[0, address & 0x03FF];
							break;
					}
				}
				else if (address is >= 0x3F00 and < 0x4000)
				{
					address &= 0x001F;
					if (address == 0x0010) address = 0x0000;
					if (address == 0x0014) address = 0x0004;
					if (address == 0x0018) address = 0x0008;
					if (address == 0x001C) address = 0x000C;
					value = (byte)(Palette[address] & (RegisterMask.Grayscale ? 0x30 : 0x3F));
				}
			}

			return value;
		}

		public void InternalWrite(ushort address, byte value)
		{
			address &= 0x3FFF;

			if (nes.Cartridge != null && !nes.Cartridge.PpuWrite(address, value))
			{
				if (address is >= 0x0000 and < 0x2000)
				{
					PatternTables[(address & 0x1000) >> 12, address & 0x0FFF] = value;
					PatternTablesDirty = true;
				}
				else if (address is >= 0x2000 and < 0x3F00)
				{
					address &= 0x0FFF;

					switch (nes.Cartridge.NametableArrangement)
					{
						case Cartridges.NametableArrangement.VerticalMirror:
							if ((address is < 0x0400) || (address is >= 0x0800 and < 0x0C00))
								Nametables[0, address & 0x03FF] = value;
							else if ((address is >= 0x0400 and < 0x0800) || (address is >= 0x0C00))
								Nametables[1, address & 0x03FF] = value;
							break;

						case Cartridges.NametableArrangement.HorizontalMirror:
							if (address is < 0x0800)
								Nametables[0, address & 0x03FF] = value;
							else
								Nametables[1, address & 0x03FF] = value;
							break;

						case Cartridges.NametableArrangement.OneScreenLowerBank:
							Nametables[1, address & 0x03FF] = value;
							break;

						case Cartridges.NametableArrangement.OneScreenUpperBank:
							Nametables[0, address & 0x03FF] = value;
							break;
					}
					NametablesDirty = true;
				}
				else if (address is >= 0x3F00 and < 0x4000)
				{
					address &= 0x001F;
					if (address == 0x0010) address = 0x0000;
					if (address == 0x0014) address = 0x0004;
					if (address == 0x0018) address = 0x0008;
					if (address == 0x001C) address = 0x000C;
					Palette[address] = value;
					PaletteDirty = true;
				}
			}
		}

		public bool Tick()
		{
			void incrementScrollX()
			{
				if (RegisterMask.ShowBackground || RegisterMask.ShowSprites)
				{
					if (CurrentVramAddress.CoarseXScroll == 31)
					{
						CurrentVramAddress.CoarseXScroll = 0;
						CurrentVramAddress.NametableX = !CurrentVramAddress.NametableX;
					}
					else
						CurrentVramAddress.CoarseXScroll++;
				}
			}

			void incrementScrollY()
			{
				if (RegisterMask.ShowBackground || RegisterMask.ShowSprites)
				{
					if (CurrentVramAddress.FineYScroll < 7)
						CurrentVramAddress.FineYScroll++;
					else
					{
						CurrentVramAddress.FineYScroll = 0;

						if (CurrentVramAddress.CoarseYScroll == 29)
						{
							CurrentVramAddress.CoarseYScroll = 0;
							CurrentVramAddress.NametableY = !CurrentVramAddress.NametableY;
						}
						else if (CurrentVramAddress.CoarseYScroll == 31)
							CurrentVramAddress.CoarseYScroll = 0;
						else
							CurrentVramAddress.CoarseYScroll++;
					}
				}
			}

			void transferAddressX()
			{
				if (RegisterMask.ShowBackground || RegisterMask.ShowSprites)
				{
					CurrentVramAddress.NametableX = TemporaryVramAddress.NametableX;
					CurrentVramAddress.CoarseXScroll = TemporaryVramAddress.CoarseXScroll;
				}
			}

			void transferAddressY()
			{
				if (RegisterMask.ShowBackground || RegisterMask.ShowSprites)
				{
					CurrentVramAddress.FineYScroll = TemporaryVramAddress.FineYScroll;
					CurrentVramAddress.NametableY = TemporaryVramAddress.NametableY;
					CurrentVramAddress.CoarseYScroll = TemporaryVramAddress.CoarseYScroll;
				}
			}

			void loadBackgroundShifters()
			{
				BgShifters.PatternLo = (ushort)((BgShifters.PatternLo & 0xFF00) | NextTileData.Lsb);
				BgShifters.PatternHi = (ushort)((BgShifters.PatternHi & 0xFF00) | NextTileData.Msb);

				BgShifters.AttribLo = (ushort)((BgShifters.AttribLo & 0xFF00) | ((NextTileData.Attrib & 0b01) != 0 ? 0xFF : 0x00));
				BgShifters.AttribHi = (ushort)((BgShifters.AttribHi & 0xFF00) | ((NextTileData.Attrib & 0b10) != 0 ? 0xFF : 0x00));
			}

			void updateShifters()
			{
				if (RegisterMask.ShowBackground)
				{
					BgShifters.PatternLo <<= 1;
					BgShifters.PatternHi <<= 1;
					BgShifters.AttribLo <<= 1;
					BgShifters.AttribHi <<= 1;
				}

				if (RegisterMask.ShowSprites && Cycle >= 1 && Cycle < 258)
				{
					for (var i = 0; i < SpritesOnScanline; i++)
					{
						if (CurrentSprites[i].X > 0)
						{
							CurrentSprites[i].X--;
						}
						else
						{
							SpriteShifters[i].PatternLo <<= 1;
							SpriteShifters[i].PatternHi <<= 1;
						}
					}
				}
			}

			if (Scanline == 0 && Cycle == 0)
			{
				NametablesDirty = false;
				PatternTablesDirty = false;
				PaletteDirty = false;
			}

			if (Scanline >= -1 && Scanline < 240)
			{
				if (Scanline == 0 && Cycle == 0 && IsOddFrame && (RegisterMask.ShowBackground || RegisterMask.ShowSprites))
				{
					Cycle = 1;
				}

				if (Scanline == -1 && Cycle == 1)
				{
					RegisterStatus.VerticalBlank = false;
					RegisterStatus.Sprite0Hit = false;
					RegisterStatus.SpriteOverflow = false;

					for (var i = 0; i < SpriteShifters.Length; i++)
					{
						SpriteShifters[i].PatternLo = 0;
						SpriteShifters[i].PatternHi = 0;
					}
				}

				if ((Cycle >= 2 && Cycle < 258) || (Cycle >= 321 && Cycle < 338))
				{
					updateShifters();

					switch ((Cycle - 1) % 8)
					{
						case 0:
							loadBackgroundShifters();
							NextTileData.Id = InternalRead((ushort)(0x2000 | (CurrentVramAddress & 0x0FFF)));
							break;

						case 2:
							NextTileData.Attrib = InternalRead((ushort)(
								0x23C0 |
								(CurrentVramAddress.NametableY ? (1 << 11) : 0) |
								(CurrentVramAddress.NametableX ? (1 << 10) : 0) |
								((CurrentVramAddress.CoarseYScroll >> 2) << 3) |
								(CurrentVramAddress.CoarseXScroll >> 2)));

							if ((CurrentVramAddress.CoarseYScroll & 0x02) != 0) NextTileData.Attrib >>= 4;
							if ((CurrentVramAddress.CoarseXScroll & 0x02) != 0) NextTileData.Attrib >>= 2;
							NextTileData.Attrib &= 0x03;
							break;

						case 4:
							NextTileData.Lsb = InternalRead((ushort)(RegisterControl.BgPatternBaseAddress + (NextTileData.Id << 4) + CurrentVramAddress.FineYScroll + 0));
							break;

						case 6:
							NextTileData.Msb = InternalRead((ushort)(RegisterControl.BgPatternBaseAddress + (NextTileData.Id << 4) + CurrentVramAddress.FineYScroll + 8));
							break;

						case 7:
							incrementScrollX();
							break;
					}
				}

				if (Cycle == 256)
				{
					incrementScrollY();
				}

				if (Cycle == 257)
				{
					loadBackgroundShifters();
					transferAddressX();
				}

				if (Cycle == 338 || Cycle == 340)
				{
					NextTileData.Id = InternalRead((ushort)(0x2000 | (CurrentVramAddress & 0x0FFF)));
				}

				if (Scanline == -1 && Cycle >= 280 && Cycle < 305)
				{
					transferAddressY();
				}

				if (Cycle == 257 && Scanline >= 0)
				{
					for (var i = 0; i < CurrentSprites.Length; i++)
						CurrentSprites[i] = new();

					SpritesOnScanline = 0;

					for (var i = 0; i < SpriteShifters.Length; i++)
					{
						SpriteShifters[i].PatternLo = 0;
						SpriteShifters[i].PatternHi = 0;
					}

					Sprite0HitPossible = false;

					var oamEntry = 0;
					while (oamEntry < 64 && SpritesOnScanline < 9)
					{
						var spriteY = OamData[(oamEntry * 4) + 0];
						var diff = Scanline - spriteY;
						if (diff >= 0 && diff < (RegisterControl.EnableLargeSprites ? 16 : 8) && SpritesOnScanline < 8)
						{
							if (oamEntry == 0) Sprite0HitPossible = true;

							CurrentSprites[SpritesOnScanline] = new()
							{
								Y = spriteY,
								TileIndex = OamData[(oamEntry * 4) + 1],
								Attributes = OamData[(oamEntry * 4) + 2],
								X = OamData[(oamEntry * 4) + 3]
							};
							SpritesOnScanline++;
						}
						oamEntry++;
					}

					RegisterStatus.SpriteOverflow = SpritesOnScanline >= 8;
				}
			}

			if (Cycle == 340)
			{
				for (var i = 0; i < SpritesOnScanline; i++)
				{
					var sprite = CurrentSprites[i];

					(byte lo, byte hi) patternBits;
					(ushort lo, ushort hi) patternAddress;

					if (!RegisterControl.EnableLargeSprites)
					{
						patternAddress.lo = (ushort)(
							RegisterControl.SprPatternBaseAddress |
							(sprite.TileIndex << 4) |
							(sprite.VerticalFlip ? 7 - (Scanline - sprite.Y) : (Scanline - sprite.Y)));
					}
					else
					{
						if (sprite.VerticalFlip)
						{
							patternAddress.lo = (ushort)(
								(sprite.TileIndex & 0x01) << 12 |
								((sprite.TileIndex & 0xFE) + ((Scanline - sprite.Y < 8) ? 1 : 0)) << 4 |
								(7 - (Scanline - sprite.Y) & 0x07));
						}
						else
						{
							patternAddress.lo = (ushort)(
								(sprite.TileIndex & 0x01) << 12 |
								((sprite.TileIndex & 0xFE) + ((Scanline - sprite.Y < 8) ? 0 : 1)) << 4 |
								(Scanline - sprite.Y) & 0x07);
						}
					}

					patternAddress.hi = (ushort)(patternAddress.lo + 8);

					patternBits.lo = InternalRead(patternAddress.lo);
					patternBits.hi = InternalRead(patternAddress.hi);

					if (sprite.HorizontalFlip)
					{
						/* https://stackoverflow.com/a/2602885 */
						byte flip(byte value)
						{
							value = (byte)((value & 0xF0) >> 4 | (value & 0x0F) << 4);
							value = (byte)((value & 0xCC) >> 2 | (value & 0x33) << 2);
							value = (byte)((value & 0xAA) >> 1 | (value & 0x55) << 1);
							return value;
						}

						patternBits.lo = flip(patternBits.lo);
						patternBits.hi = flip(patternBits.hi);
					}

					SpriteShifters[i].PatternLo = patternBits.lo;
					SpriteShifters[i].PatternHi = patternBits.hi;
				}
			}

			if (Scanline == 240)
			{
				/* nothing! */
			}

			if (Scanline >= 241 && Scanline < 261)
			{
				if (Scanline == 241 && Cycle == 1)
				{
					RegisterStatus.VerticalBlank = true;

					if (RegisterControl.EnableNmi)
						NmiOccured = true;
				}
			}

			(byte bgPixel, byte bgPalette) = (0, 0);

			if (RegisterMask.ShowBackground && (RegisterMask.ShowBgLeftBorder || Cycle >= 9))
			{
				var mask = 0x8000 >> FineXScroll;

				var patternLo = (BgShifters.PatternLo & mask) != 0 ? 1 : 0;
				var patternHi = (BgShifters.PatternHi & mask) != 0 ? 1 : 0;

				var paletteLo = (BgShifters.AttribLo & mask) != 0 ? 1 : 0;
				var paletteHi = (BgShifters.AttribHi & mask) != 0 ? 1 : 0;

				bgPixel = (byte)((patternHi << 1) | patternLo);
				bgPalette = (byte)((paletteHi << 1) | paletteLo);
			}

			(byte sprPixel, byte sprPalette, bool sprPriority) = (0, 0, false);

			if (RegisterMask.ShowSprites && (RegisterMask.ShowSprLeftBorder || Cycle >= 9))
			{
				Sprite0Rendered = false;

				for (var i = 0; i < SpritesOnScanline; i++)
				{
					if (CurrentSprites[i].X == 0)
					{
						var patternLo = (SpriteShifters[i].PatternLo & 0x80) != 0 ? 1 : 0;
						var patternHi = (SpriteShifters[i].PatternHi & 0x80) != 0 ? 1 : 0;
						sprPixel = (byte)((patternHi << 1) | patternLo);

						sprPalette = (byte)(CurrentSprites[i].PaletteIndex + 0x04);
						sprPriority = !CurrentSprites[i].IsBehindBackground;

						if (sprPixel != 0)
						{
							if (i == 0) Sprite0Rendered = true;
							break;
						}
					}
				}
			}

			(byte finalPixel, byte finalPalette) = (0, 0);

			if (bgPixel == 0 && sprPixel != 0)
			{
				finalPixel = sprPixel;
				finalPalette = sprPalette;
			}
			else if (bgPixel != 0 && sprPixel == 0)
			{
				finalPixel = bgPixel;
				finalPalette = bgPalette;
			}
			else if (bgPixel != 0 && sprPixel != 0)
			{
				if (sprPriority)
				{
					finalPixel = sprPixel;
					finalPalette = sprPalette;
				}
				else
				{
					finalPixel = bgPixel;
					finalPalette = bgPalette;
				}

				if (Sprite0HitPossible && Sprite0Rendered && Cycle >= 1 && Cycle < 258)
					RegisterStatus.Sprite0Hit = true;
			}

			var fbOffset = ((Scanline * 256) + Cycle) * 4;
			if (fbOffset >= 0 && fbOffset < framebuffer.Length)
			{
				var colorOffset = ((InternalRead((ushort)(0x3F00 + (finalPalette << 2) + finalPixel)) & 0x3F) + (RegisterMask.Emphasis * 0x40)) * 3;
				framebuffer[fbOffset + 0] = PaletteColors[colorOffset + 0];
				framebuffer[fbOffset + 1] = PaletteColors[colorOffset + 1];
				framebuffer[fbOffset + 2] = PaletteColors[colorOffset + 2];
				framebuffer[fbOffset + 3] = 255;
			}

			Cycle++;

			if ((RegisterMask.ShowBackground || RegisterMask.ShowSprites) && Cycle == 260 && Scanline < 240)
				nes.Cartridge?.Mapper?.OnEndOfScanline();

			if (Cycle >= 341)
			{
				Cycle = 0;
				Scanline++;

				if (Scanline >= 261)
				{
					Scanline = -1;
					IsOddFrame = !IsOddFrame;

					TransferFramebuffer?.Invoke(this, new() { Data = [.. framebuffer] });

					return true;
				}
			}

			return false;
		}
	}

	public class BgShifter
	{
		public ushort PatternLo { get; set; }
		public ushort PatternHi { get; set; }
		public ushort AttribLo { get; set; }
		public ushort AttribHi { get; set; }

		public static BgShifter Empty => new() { PatternLo = 0, PatternHi = 0, AttribLo = 0, AttribHi = 0 };
	}

	public class NextTileData
	{
		public byte Id { get; set; }
		public byte Attrib { get; set; }
		public byte Lsb { get; set; }
		public byte Msb { get; set; }

		public static NextTileData Empty => new() { Id = 0, Attrib = 0, Lsb = 0, Msb = 0 };
	}

	public class FramebufferEventArgs : EventArgs
	{
		public byte[] Data { get; set; } = [];
	}
}
