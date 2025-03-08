using Hexa.NET.ImGui;

namespace FamiSharp.UserInterface
{
	public class DisplayWindow : WindowBase
	{
		public override string Title => "Display";

		int windowScale = 1;

		public int WindowScale
		{
			get => windowScale;
			set => windowScale = value;
		}

		protected override void DrawWindow(object? userData)
		{
			if (userData is not OpenGLTexture texture) return;

			var textureSize = texture.Size * windowScale;

			var childBorderSize = new Vector2(ImGui.GetStyle().ChildBorderSize);

			ImGui.SetNextWindowContentSize(textureSize + childBorderSize * 2f);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			if (!ImGui.Begin(Title, ImGuiWindowFlags.NoNav))
			{
				ImGui.PopStyleVar();
				ImGui.End();
				return;
			}

			var drawList = ImGui.GetWindowDrawList();
			var screenPos = ImGui.GetCursorScreenPos();

			var pos = new Vector2[4];
			var uvs = new Vector2[4];

			pos[0] = screenPos + new Vector2(0f, 0f) + childBorderSize;
			pos[1] = screenPos + new Vector2(0f, textureSize.Y) + childBorderSize;
			pos[2] = screenPos + new Vector2(textureSize.X, textureSize.Y) + childBorderSize;
			pos[3] = screenPos + new Vector2(textureSize.X, 0f) + childBorderSize;

			uvs[0] = new Vector2(0f, 0f);
			uvs[1] = new Vector2(0f, 1f);
			uvs[2] = new Vector2(1f, 1f);
			uvs[3] = new Vector2(1f, 0f);

			drawList.AddImageQuad(
				texture.Handle,
				pos[0], pos[1], pos[2], pos[3],
				uvs[0], uvs[1], uvs[2], uvs[3]);

			if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && ImGuiP.IsMouseReleased(ImGuiMouseButton.Right))
				ImGui.OpenPopup("context");

			ImGui.PopStyleVar();

			if (ImGui.BeginPopup("context"))
			{
				ImGui.SliderInt("##size", ref windowScale, 1, 3, "%dx");
				ImGui.EndPopup();
			}

			ImGui.End();
		}
	}
}
