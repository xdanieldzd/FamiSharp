using Hexa.NET.ImGui;
using System.Numerics;
using System.Text;

namespace FamiSharp.Utilities
{
	/*
	 * C# adaption/modification of ImGuiTextSelect -- https://github.com/AidanSun05/ImGuiTextSelect
	 * Licensed under MIT license: Copyright (c) 2024-2025 Aidan Sun and the ImGuiTextSelect contributors
	 * Based on revision 58fa8d4238b553829ebfae2cc453a8a2feb20b6a -- https://github.com/AidanSun05/ImGuiTextSelect/tree/58fa8d4238b553829ebfae2cc453a8a2feb20b6a
	 */

	public class ImGuiTextSelect(Func<int, string> getLineAtIdx, Func<int> getNumLines)
	{
		readonly CursorPos selectStart = CursorPos.Zero, selectEnd = CursorPos.Zero;

		readonly static List<char[]> ranges =
		[
			[.. Enumerable.Range(0x20, 0x2F - 0x20).Select(static x => (char)x)],
			[.. Enumerable.Range(0x3A, 0x40 - 0x3A).Select(static x => (char)x)],
			[.. Enumerable.Range(0x5B, 0x60 - 0x5B).Select(static x => (char)x)],
			[.. Enumerable.Range(0x7B, 0xBF - 0x7B).Select(static x => (char)x)],
		];

		static int Midpoint(int a, int b) => a + (b - a) / 2;
		static bool IsBoundary(char c) => ranges.Any(r => r.Contains(c));

		static float SubstringSizeX(string s, int start, int length = -1)
		{
			if (string.IsNullOrWhiteSpace(s)) return 0f;

			if (length == -1) length = s.Length;
			else length = Math.Min(length, s.Length);

			return ImGui.CalcTextSize(s.Substring(start, length)).X;
		}

		static int GetCharIndex(string s, float cursorPosX, int start, int end)
		{
			if (cursorPosX < 0) return 0;

			if (string.IsNullOrWhiteSpace(s)) return 0;
			if (end < start) return s.Length;

			var midIdx = Midpoint(start, end);
			var widthToMid = SubstringSizeX(s, 0, midIdx + 1);
			var widthToMidEx = SubstringSizeX(s, 0, midIdx);

			if (cursorPosX < widthToMidEx) return GetCharIndex(s, cursorPosX, start, midIdx - 1);
			else if (cursorPosX > widthToMid) return GetCharIndex(s, cursorPosX, midIdx + 1, end);
			else return midIdx;
		}

		static int GetCharIndex(string s, float cursorPosX)
		{
			return GetCharIndex(s, cursorPosX, 0, s.Length);
		}

		static float GetScrollDelta(float v, float min, float max)
		{
			var deltaScale = 10f * ImGui.GetIO().DeltaTime;
			var maxDelta = 100f;

			if (v < min) return Math.Max(-(min - v), -maxDelta) * deltaScale;
			else if (v > max) return Math.Min(v - max, maxDelta) * deltaScale;
			else return 0f;
		}

		private Selection GetSelection()
		{
			var startBeforeEnd = selectStart.Y < selectEnd.Y || (selectStart.Y == selectEnd.Y && selectStart.X < selectEnd.X);

			var startX = startBeforeEnd ? selectStart.X : selectEnd.X;
			var endX = startBeforeEnd ? selectEnd.X : selectStart.X;

			var startY = Math.Min(selectStart.Y, selectEnd.Y);
			var endY = Math.Max(selectStart.Y, selectEnd.Y);

			return new(startX, startY, endX, endY);
		}

		private void HandleMouseDown(Vector2 cursorPosStart)
		{
			var textHeight = ImGui.GetTextLineHeightWithSpacing();
			var mousePos = ImGui.GetMousePos() - cursorPosStart;
			var numLines = getNumLines();

			var y = (int)Math.Min(Math.Floor(mousePos.Y / textHeight), numLines - 1);
			if (y < 0) return;

			var currentLine = getLineAtIdx(y);
			var x = GetCharIndex(currentLine, mousePos.X);

			var mouseClicks = ImGui.GetMouseClickedCount(ImGuiMouseButton.Left);
			if (mouseClicks > 0)
			{
				if (mouseClicks % 3 == 0)
				{
					var atLastLine = y == numLines - 1;
					(selectStart.X, selectStart.Y) = (0, y);
					(selectEnd.X, selectEnd.Y) = (atLastLine ? currentLine.Length : 0, atLastLine ? y : y + 1);
				}
				else if (mouseClicks % 2 == 0)
				{
					var startIt = x;
					var endIt = x;

					if (startIt < currentLine.Length)
					{
						var isCurrentBoundary = IsBoundary(currentLine[startIt]);
						for (var startInv = 0; startInv <= x; startInv++)
						{
							if (IsBoundary(currentLine[startIt]) != isCurrentBoundary) break;
							(selectStart.X, selectStart.Y) = (x - startInv, y);
							startIt--;
						}

						for (var end = x; end <= currentLine.Length; end++)
						{
							(selectEnd.X, selectEnd.Y) = (end, y);
							if (endIt >= 0 && endIt < currentLine.Length && IsBoundary(currentLine[endIt]) != isCurrentBoundary) break;
							endIt++;
						}
					}
				}
				else if (ImGuiP.IsKeyDown(ImGuiKey.ModShift))
				{
					if (selectStart.IsInvalid()) (selectStart.X, selectStart.Y) = (0, 0);
					(selectEnd.X, selectEnd.Y) = (x, y);
				}
				else
				{
					(selectStart.X, selectStart.Y) = (x, y);
					(selectEnd.X, selectEnd.Y) = (-1, -1);
				}
			}
			else if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
			{
				(selectEnd.X, selectEnd.Y) = (x, y);
			}
		}

		private static void HandleScrolling()
		{
			var windowMin = ImGui.GetWindowPos();
			var windowMax = windowMin + ImGui.GetWindowSize();

			var currentWindow = ImGuiP.GetCurrentWindow();
			var activeWindow = ImGui.GetCurrentContext().ActiveIdWindow;

			var scrollXID = ImGuiP.GetWindowScrollbarID(currentWindow, ImGuiAxis.X);
			var scrollYID = ImGuiP.GetWindowScrollbarID(currentWindow, ImGuiAxis.Y);
			var activeID = ImGuiP.GetActiveID();
			var scrollbarsActive = activeID == scrollXID || activeID == scrollYID;

			if (activeWindow.IsNull || activeWindow.ID != currentWindow.ID || scrollbarsActive) return;

			var mousePos = ImGui.GetMousePos();
			var scrollXDelta = GetScrollDelta(mousePos.X, windowMin.X, windowMax.X);
			var scrollYDelta = GetScrollDelta(mousePos.Y, windowMin.Y, windowMax.Y);

			if (Math.Abs(scrollXDelta) > 0.0f) ImGuiP.SetScrollX(ImGui.GetScrollX() + scrollXDelta);
			if (Math.Abs(scrollYDelta) > 0.0f) ImGuiP.SetScrollY(ImGui.GetScrollY() + scrollYDelta);
		}

		private void DrawSelection(Vector2 cursorPosStart)
		{
			if (!HasSelection()) return;

			var (startX, startY, endX, endY) = GetSelection();

			var numLines = getNumLines();
			if (startX >= numLines || endY >= numLines) return;

			for (var i = startY; i <= endY; i++)
			{
				var line = getLineAtIdx(i);

				var newlineHeight = ImGui.CalcTextSize(" ").X;
				var textHeight = ImGui.GetTextLineHeightWithSpacing();

				var minX = i == startY ? SubstringSizeX(line, 0, startX) : 0;
				var maxX = i == endY ? SubstringSizeX(line, 0, endX) : SubstringSizeX(line, 0) + newlineHeight;

				var minY = i * textHeight;
				var maxY = (i + 1) * textHeight;

				var rectMin = cursorPosStart + new Vector2(minX, minY);
				var rectMax = cursorPosStart + new Vector2(maxX, maxY);

				var color = ImGui.GetColorU32(ImGuiCol.TextSelectedBg);
				ImGui.GetWindowDrawList().AddRectFilled(rectMin, rectMax, color);
			}
		}

		public bool HasSelection() => !selectStart.IsInvalid() && !selectEnd.IsInvalid();

		public void Copy()
		{
			if (!HasSelection()) return;

			var (startX, startY, endX, endY) = GetSelection();

			var selectedText = new StringBuilder();

			for (var i = startY; i <= endY; i++)
			{
				var subStart = i == startY ? startX : 0;
				var line = getLineAtIdx(i);

				var stringStart = subStart;
				var stringEnd = subStart;
				if (i == endY) stringEnd += endX - subStart;
				else stringEnd = line.Length;

				var lineToAdd = line[stringStart..stringEnd];
				if (!lineToAdd.EndsWith('\n')) lineToAdd += '\n';
				selectedText.Append(lineToAdd);
			}

			ImGui.SetClipboardText(selectedText.ToString());
		}

		public void SelectAll()
		{
			var lastLineIdx = getNumLines() - 1;
			var lastLine = getLineAtIdx(lastLineIdx);

			(selectStart.X, selectStart.Y) = (0, 0);
			(selectEnd.X, selectEnd.Y) = (lastLine.Length, lastLineIdx);
		}

		public void Update()
		{
			var cursorPosStart = ImGui.GetWindowPos() + ImGui.GetCursorStartPos();

			var hovered = ImGui.IsWindowHovered();
			if (hovered)
			{
				var framePadding = ImGui.GetStyle().FramePadding;
				var cursorScreenPos = ImGui.GetWindowPos() - framePadding;
				if (ImGui.IsMouseHoveringRect(cursorScreenPos, cursorScreenPos + ImGui.GetWindowSize()))
					ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
			}

			if (ImGuiP.IsMouseDown(ImGuiMouseButton.Left))
			{
				if (hovered) HandleMouseDown(cursorPosStart);
				else HandleScrolling();
			}

			DrawSelection(cursorPosStart);

			if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.A))) SelectAll();
			else if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.C))) Copy();
		}

		internal sealed class CursorPos(int x, int y)
		{
			public int X { get; set; } = x;
			public int Y { get; set; } = y;

			public bool IsInvalid() => X == -1 || Y == -1;

			public static CursorPos Zero => new(0, 0);
		}

		internal sealed class Selection(int startX, int startY, int endX, int endY)
		{
			public int StartX { get; set; } = startX;
			public int StartY { get; set; } = startY;
			public int EndX { get; set; } = endX;
			public int EndY { get; set; } = endY;

			public void Deconstruct(out int startX, out int startY, out int endX, out int endY)
			{
				startX = StartX;
				startY = StartY;
				endX = EndX;
				endY = EndY;
			}

			public static Selection Empty => new(0, 0, 0, 0);
		}
	}
}
