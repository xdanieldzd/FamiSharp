using Hexa.NET.OpenGL;
using System.Runtime.InteropServices;

namespace FamiSharp
{
	public sealed class OpenGLTexture : BaseDisposable
	{
		const GLTextureMinFilter defaultMinFilter = GLTextureMinFilter.Nearest;
		const GLTextureMagFilter defaultMagFilter = GLTextureMagFilter.Nearest;
		const GLTextureWrapMode defaultWrapModeS = GLTextureWrapMode.Repeat;
		const GLTextureWrapMode defaultWrapModeT = GLTextureWrapMode.Repeat;

		const GLInternalFormat defaultInternalFormat = GLInternalFormat.Rgba8;
		const GLPixelFormat defaultPixelFormat = GLPixelFormat.Rgba;
		const GLPixelType defaultPixelType = GLPixelType.UnsignedByte;

		readonly GL gl;
		readonly (byte r, byte g, byte b, byte a) initialColors = (0, 0, 0, 255);

		public uint Handle { get; }
		public (int Width, int Height) Size { get; }

		public OpenGLTexture(GL gl, int width, int height) : this(gl, width, height, 0, 0, 0, 255) { }

		public OpenGLTexture(GL gl, int width, int height, byte r, byte g, byte b, byte a)
		{
			this.gl = gl;

			(Handle, Size) = (gl.GenTexture(), (width, height));

			Initialize(Fill(initialColors = (r, g, b, a)));
		}

		private void ChangeTextureParams(Action action)
		{
			gl.GetIntegerv(GLGetPName.Texture2D, out int lastTextureSet);
			if (Handle != lastTextureSet) gl.BindTexture(GLTextureTarget.Texture2D, Handle);
			action?.Invoke();
			gl.BindTexture(GLTextureTarget.Texture2D, (uint)lastTextureSet);
		}

		private void Initialize(byte[] data)
		{
			ChangeTextureParams(() =>
			{
				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pointer = handle.AddrOfPinnedObject();
				gl.TexImage2D(GLTextureTarget.Texture2D, 0, defaultInternalFormat, Size.Width, Size.Height, 0, defaultPixelFormat, defaultPixelType, pointer);
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)defaultMinFilter);
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)defaultMagFilter);
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapS, (int)defaultWrapModeS);
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapT, (int)defaultWrapModeT);
				handle.Free();
			});
		}

		public void SetTextureFilter(GLTextureMinFilter textureMinFilter, GLTextureMagFilter textureMagFilter)
		{
			ChangeTextureParams(() =>
			{
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)textureMinFilter);
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)textureMagFilter);
			});
		}

		public void SetTextureWrapMode(GLTextureWrapMode textureWrapModeS, GLTextureWrapMode textureWrapModeT)
		{
			ChangeTextureParams(() =>
			{
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapS, (int)textureWrapModeS);
				gl.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapT, (int)textureWrapModeT);
			});
		}

		private byte[] Fill((byte r, byte g, byte b, byte a) color) => [.. Enumerable.Repeat(color, Size.Width * Size.Height).SelectMany(c => new[] { c.r, c.g, c.b, c.a })];

		public void Update(byte[] data)
		{
			ChangeTextureParams(() =>
			{
				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pointer = handle.AddrOfPinnedObject();
				gl.TexSubImage2D(GLTextureTarget.Texture2D, 0, 0, 0, Size.Width, Size.Height, defaultPixelFormat, defaultPixelType, pointer);
				handle.Free();
			});
		}

		public void Clear() => Update(Fill(initialColors));

		protected override void DisposeUnmanaged()
		{
			if (gl.IsTexture(Handle))
				gl.DeleteTexture(Handle);
		}
	}
}
