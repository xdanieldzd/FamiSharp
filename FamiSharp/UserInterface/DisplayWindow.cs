using Hexa.NET.ImGui;
using System.Numerics;

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

			var borderSize = new Vector2(ImGui.GetStyle().ChildBorderSize);

			ImGui.SetNextWindowContentSize(textureSize + borderSize * 2f);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			if (ImGui.Begin(Title, ImGuiWindowFlags.NoNav))
			{
				var drawList = ImGui.GetWindowDrawList();
				var screenPos = ImGui.GetCursorScreenPos();

				var pos = new Vector2[4];
				var uvs = new Vector2[4];

				pos[0] = screenPos + new Vector2(0f, 0f) + borderSize;
				pos[1] = screenPos + new Vector2(0f, textureSize.Y) + borderSize;
				pos[2] = screenPos + new Vector2(textureSize.X, textureSize.Y) + borderSize;
				pos[3] = screenPos + new Vector2(textureSize.X, 0f) + borderSize;

				uvs[0] = new Vector2(0f, 0f);
				uvs[1] = new Vector2(0f, 1f);
				uvs[2] = new Vector2(1f, 1f);
				uvs[3] = new Vector2(1f, 0f);

				drawList.AddImageQuad(
					texture.Handle,
					pos[0], pos[1], pos[2], pos[3],
					uvs[0], uvs[1], uvs[2], uvs[3]);

				ImGui.End();
			}

			ImGui.PopStyleVar();
		}
	}
}
