using Hexa.NET.ImGui;

namespace FamiSharp.UserInterface
{
	public abstract class WindowBase
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Required in derived classes, ImGui.Begin requires argument pOpen as reference")]
		protected bool windowOpen;

		bool isFirstOpen = true;
		bool isFocused;

		public bool IsWindowOpen { get => windowOpen; set => windowOpen = value; }

		public abstract string Title { get; }

		public Vector2 InitialWindowSize { get; } = Vector2.Zero;
		public ImGuiCond SizingCondition { get; } = ImGuiCond.None;

		public bool IsFocused
		{
			get => isFocused;
			set { if (value) ImGui.SetWindowFocus(Title); }
		}

		public WindowBase() { }

		public WindowBase(Vector2 size, ImGuiCond condition)
		{
			InitialWindowSize = size;
			SizingCondition = condition;
		}

		public virtual void Draw(object? userData)
		{
			if (!windowOpen) return;

			if (isFirstOpen)
			{
				InitializeWindow(userData);
				isFirstOpen = false;
			}

			ImGui.SetNextWindowSize(InitialWindowSize, SizingCondition);

			DrawWindow(userData);

			if (ImGui.Begin(Title))
			{
				isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
				ImGui.End();
			}
		}

		protected virtual void InitializeWindow(object? userData) { }

		protected abstract void DrawWindow(object? userData);
	}
}
