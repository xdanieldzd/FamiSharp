using FamiSharp.Emulation;
using Hexa.NET.ImGui;
using Hexa.NET.OpenGL;

namespace FamiSharp.UserInterface
{
	public class PatternTableWindow : WindowBase
	{
		public override string Title => "Pattern Tables";

		const float zoom = 2f;
		const int numPatternTables = 2;

		readonly static (int x, int y) patternTableSize = (16, 16);

		readonly static int[][] patternArrangements =
		[
			// 8x8
			[
				0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
				0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
			],
			// 8x16
			[
				0x00, 0x02, 0x04, 0x06, 0x08, 0x0A, 0x0C, 0x0E, 0x10, 0x12, 0x14, 0x16, 0x18, 0x1A, 0x1C, 0x1E,
				0x01, 0x03, 0x05, 0x07, 0x09, 0x0B, 0x0D, 0x0F, 0x11, 0x13, 0x15, 0x17, 0x19, 0x1B, 0x1D, 0x1F
			],
			// 16x16
			[
				0x00, 0x01, 0x04, 0x05, 0x08, 0x09, 0x0C, 0x0D, 0x10, 0x11, 0x14, 0x15, 0x18, 0x19, 0x1C, 0x1D,
				0x02, 0x03, 0x06, 0x07, 0x0A, 0x0B, 0x0E, 0x0F, 0x12, 0x13, 0x16, 0x17, 0x1A, 0x1B, 0x1E, 0x1F
			]
		];

		uint hoveredHighlightColor = 0, selectedHighlightColor = 0, hoveredBorderColor = 0, selectedBorderColor = 0;
		(int pt, int x, int y) hoveredTile = (-1, -1, -1), selectedTile = (0, 0, 0);
		int patternArrangementIdx = 0, paletteIdx = 0;

		readonly byte[][] patternTablesTextureData = new byte[numPatternTables][];
		readonly OpenGLTexture[] patternTableTextures = new OpenGLTexture[numPatternTables];

		public PatternTableWindow() : base() { }

		protected override void InitializeWindow(object? userData)
		{
			if (userData is not NesSystem) return;

			hoveredHighlightColor = 0x7F000000 | (ImGui.GetColorU32(ImGuiCol.Border) & 0x00FFFFFF);
			selectedHighlightColor = 0x7F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);
			hoveredBorderColor = 0x7F00FF00;
			selectedBorderColor = 0x7F0000FF;

			for (var i = 0; i < numPatternTables; i++)
			{
				patternTablesTextureData[i] = new byte[patternTableSize.x * 8 * patternTableSize.y * 8 * 4];
				Array.Fill<byte>(patternTablesTextureData[i], 0xFF);

				patternTableTextures[i] = new(patternTableSize.x * 8, patternTableSize.y * 8);
				patternTableTextures[i].SetTextureFilter(GLTextureMinFilter.Nearest, GLTextureMagFilter.Nearest);
				patternTableTextures[i].SetTextureWrapMode(GLTextureWrapMode.Repeat, GLTextureWrapMode.Repeat);
			}
		}

		protected override void DrawWindow(object? userData)
		{
			if (userData is not NesSystem nes) return;

			if (!ImGui.Begin(Title, ref isWindowOpen))
			{
				ImGui.End();
				return;
			}

			hoveredTile = (-1, -1, -1);

			var drawList = ImGui.GetWindowDrawList();
			var tileSize = new Vector2(8f * zoom, 8f * zoom);

			if (nes.Ppu.PatternTablesDirty || nes.Ppu.PaletteDirty)
				UpdatePatternTableTextures(nes, paletteIdx, patternArrangementIdx);

			if (ImGui.BeginTable("patterntables", 3))
			{
				ImGui.TableNextRow();
				for (var i = 0; i < numPatternTables; i++)
				{
					ImGui.TableSetColumnIndex(i);

					var imagePos = ImGui.GetCursorScreenPos();
					ImGui.Image(patternTableTextures[i].Handle, patternTableTextures[i].Size * zoom);

					/* Prevent window from being dragged around if inside pattern table view */
					ImGui.SetCursorScreenPos(imagePos);
					ImGui.InvisibleButton($"##patterns{i}-dummybutton", new(patternTableSize.x * 8f * zoom, patternTableSize.y * 8f * zoom));

					for (var x = 0; x < patternTableSize.x; x++)
					{
						for (var y = 0; y < patternTableSize.y; y++)
						{
							var tilePos = imagePos + new Vector2(x * 8f * zoom, y * 8f * zoom);
							var isHovering = ImGui.IsMouseHoveringRect(tilePos, tilePos + tileSize);
							if (isHovering)
							{
								hoveredTile = (i, x, y);
								if (ImGuiP.IsMouseDown(ImGuiMouseButton.Left))
									selectedTile = hoveredTile;
							}

							if (selectedTile == (i, x, y))
							{
								drawList.AddRectFilled(tilePos, tilePos + tileSize, selectedHighlightColor);
								drawList.AddRect(tilePos, tilePos + tileSize, selectedBorderColor);
							}

							if (hoveredTile != (-1, -1, -1) && hoveredTile == (i, x, y))
							{
								drawList.AddRectFilled(tilePos, tilePos + tileSize, hoveredHighlightColor);
								drawList.AddRect(tilePos, tilePos + tileSize, hoveredBorderColor);
							}
						}
					}
				}

				ImGui.TableSetColumnIndex(2);

				ImGui.SeparatorText("Tile Info");

				var tileAddress = (selectedTile.pt * 0x1000) + ((patternTableSize.x * selectedTile.y) + selectedTile.x) * 0x10;
				var tileAddressString = $"{tileAddress:X4}";
				var tileIndex = (selectedTile.y * 0x10) + selectedTile.x;
				var tileIndexString = $"{tileIndex:X2}";
				ImGui.InputText("PPU Address", ref tileAddressString, 5, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.CharsHexadecimal);
				ImGui.InputText("Tile Index", ref tileIndexString, 3, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.CharsHexadecimal);

				ImGui.Dummy(new(0f, 2.5f));

				var tileZoomSize = new Vector2(8f * 8f, 8f * 8f);
				var tileZoomUv0 = new Vector2(selectedTile.x / (float)patternTableSize.x, selectedTile.y / (float)patternTableSize.y);
				var tileZoomUv1 = new Vector2((selectedTile.x + 1) / (float)patternTableSize.x, (selectedTile.y + 1) / (float)patternTableSize.y);
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - tileZoomSize.X) / 2f);
				ImGui.Image(patternTableTextures[selectedTile.pt].Handle, tileZoomSize, tileZoomUv0, tileZoomUv1);

				ImGui.SeparatorText("Table Arrangement");
				if (ImGui.BeginTable("table-arrangement", 3, ImGuiTableFlags.SizingStretchSame))
				{
					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);
					ImGui.RadioButton("8x8", ref patternArrangementIdx, 0);
					ImGui.TableSetColumnIndex(1);
					ImGui.RadioButton("8x16", ref patternArrangementIdx, 1);
					ImGui.TableSetColumnIndex(2);
					ImGui.RadioButton("16x16", ref patternArrangementIdx, 2);
					ImGui.EndTable();
				}

				ImGui.SeparatorText("Palette");
				ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
				ImGui.SliderInt("##palette-number", ref paletteIdx, 0, 7, $"#{paletteIdx} ({(paletteIdx < 4 ? "Background" : "Sprites")} #{paletteIdx & 0b011})");

				ImGui.EndTable();
			}

			ImGui.End();
		}

		private void UpdatePatternTableTextures(NesSystem nes, int paletteIdx, int arrangementIdx)
		{
			void drawTile(int patternBase, int textureBase)
			{
				var ptIndex = (patternBase >> 12) & 0b1;
				for (var pY = 0; pY < 8; pY++)
				{
					var pLsb = nes.Ppu.InternalRead((ushort)(patternBase | pY | 0));
					var pMsb = nes.Ppu.InternalRead((ushort)(patternBase | pY | 8));
					for (var pX = 0; pX < 8; pX++)
					{
						var patternLo = (pLsb & 0x80) != 0 ? 1 : 0;
						var patternHi = (pMsb & 0x80) != 0 ? 1 : 0;
						pLsb <<= 1;
						pMsb <<= 1;

						var colorOffset = (nes.Ppu.InternalRead((ushort)(0x3F00 | (paletteIdx << 2) | (patternHi << 1) | patternLo)) & 0x3F) * 3;
						var textureOffset = textureBase | (((pY * patternTableSize.x * 8) | pX) << 2);
						for (var i = 0; i < 3; i++)
							patternTablesTextureData[ptIndex][textureOffset + i] = nes.Ppu.PaletteColors[colorOffset + i];
					}
				}
			}

			var patternArrangement = patternArrangements[arrangementIdx];
			var indexMaskHi = 0x100 - patternArrangement.Length;
			var indexMaskLo = patternArrangement.Length - 1;

			for (var ptIndex = 0; ptIndex < numPatternTables; ptIndex++)
			{
				for (var ptY = 0; ptY < patternTableSize.y; ptY++)
				{
					for (var ptX = 0; ptX < patternTableSize.x; ptX++)
					{
						var tileIndex =
							(ptY << 4 | ptX) & indexMaskHi |                    /* index high bits */
							patternArrangement[(ptY << 4 | ptX) & indexMaskLo]; /* index low bits, through arrangement table */
						drawTile((ptIndex << 12) | (tileIndex << 4), ((ptY << 7) | ptX) << 5);
					}
				}
				patternTableTextures[ptIndex].Update(patternTablesTextureData[ptIndex]);
			}
		}
	}
}
