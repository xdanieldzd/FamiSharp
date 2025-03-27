using Hexa.NET.OpenGL;
using System.Runtime.InteropServices;
using System.Numerics;

namespace FamiSharp
{
	public sealed class OpenGLTexture : IDisposable
	{
		const GLTextureMinFilter defaultMinFilter = GLTextureMinFilter.Nearest;
		const GLTextureMagFilter defaultMagFilter = GLTextureMagFilter.Nearest;
		const GLTextureWrapMode defaultWrapModeS = GLTextureWrapMode.Repeat;
		const GLTextureWrapMode defaultWrapModeT = GLTextureWrapMode.Repeat;

		readonly GL gl;

		public uint Handle { get; }
		public Vector2 Size { get; } = Vector2.Zero;

		(byte r, byte g, byte b, byte a) initialColors = (0, 0, 0, 255);

		bool disposed;

		public OpenGLTexture(GL gl, int width, int height) : this(gl, width, height, 0, 0, 0, 255) { }

		public OpenGLTexture(GL gl, int width, int height, byte r, byte g, byte b, byte a)
		{
			this.gl = gl;

			(Handle, Size) = (gl.GenTexture(), new(width, height));

			Initialize(Fill(initialColors = (r, g, b, a)));
		}

		~OpenGLTexture()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				if (gl.IsTexture(Handle))
					gl.DeleteTexture(Handle);
			}

			disposed = true;
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
				gl.TexImage2D(GLTextureTarget.Texture2D, 0, GLInternalFormat.Rgba8, (int)Size.X, (int)Size.Y, 0, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, pointer);
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

		private byte[] Fill((byte r, byte g, byte b, byte a) color) => [.. Enumerable.Repeat(color, (int)(Size.X * Size.Y)).SelectMany(c => new[] { c.r, c.g, c.b, c.a })];

		public void Update(byte[] data)
		{
			ChangeTextureParams(() =>
			{
				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pointer = handle.AddrOfPinnedObject();
				gl.TexSubImage2D(GLTextureTarget.Texture2D, 0, 0, 0, (int)Size.X, (int)Size.Y, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, pointer);
				handle.Free();
			});
		}

		public void Clear() => Update(Fill(initialColors));
	}
}
