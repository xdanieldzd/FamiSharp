using Hexa.NET.OpenGL;
using System.Runtime.InteropServices;

namespace FamiSharp
{
	public sealed class OpenGLTexture : IDisposable
	{
		const GLTextureMinFilter defaultMinFilter = GLTextureMinFilter.Nearest;
		const GLTextureMagFilter defaultMagFilter = GLTextureMagFilter.Nearest;
		const GLTextureWrapMode defaultWrapModeS = GLTextureWrapMode.Repeat;
		const GLTextureWrapMode defaultWrapModeT = GLTextureWrapMode.Repeat;

		public uint Handle { get; } = 0;
		public Vector2 Size { get; } = Vector2.Zero;

		(byte r, byte g, byte b, byte a) initialColors = (0, 0, 0, 255);

		bool disposed = false;

		public OpenGLTexture(int width, int height) : this(width, height, 0, 0, 0, 255) { }

		public OpenGLTexture(int width, int height, byte r, byte g, byte b, byte a)
		{
			(Handle, Size) = (Application.GL.GenTexture(), new(width, height));

			initialColors = (r, g, b, a);

			var data = new byte[width * height * 4];
			for (var i = 0; i < data.Length; i += 4)
			{
				data[i + 0] = r;
				data[i + 1] = g;
				data[i + 2] = b;
				data[i + 3] = a;
			}
			Initialize(data);
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
				if (Application.GL.IsTexture(Handle))
					Application.GL.DeleteTexture(Handle);
			}

			disposed = true;
		}

		private void ChangeTextureParams(Action action)
		{
			Application.GL.GetIntegerv(GLGetPName.Texture2D, out int lastTextureSet);
			if (Handle != lastTextureSet) Application.GL.BindTexture(GLTextureTarget.Texture2D, Handle);
			action?.Invoke();
			Application.GL.BindTexture(GLTextureTarget.Texture2D, (uint)lastTextureSet);
		}

		private void Initialize(byte[] data)
		{
			ChangeTextureParams(() =>
			{
				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pointer = handle.AddrOfPinnedObject();
				Application.GL.TexImage2D(GLTextureTarget.Texture2D, 0, GLInternalFormat.Rgba8, (int)Size.X, (int)Size.Y, 0, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, pointer);
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)defaultMinFilter);
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)defaultMagFilter);
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapS, (int)defaultWrapModeS);
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapT, (int)defaultWrapModeT);
				handle.Free();
			});
		}

		public void SetTextureFilter(GLTextureMinFilter textureMinFilter, GLTextureMagFilter textureMagFilter)
		{
			ChangeTextureParams(() =>
			{
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)textureMinFilter);
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)textureMagFilter);
			});
		}

		public void SetTextureWrapMode(GLTextureWrapMode textureWrapModeS, GLTextureWrapMode textureWrapModeT)
		{
			ChangeTextureParams(() =>
			{
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapS, (int)textureWrapModeS);
				Application.GL.TexParameteri(GLTextureTarget.Texture2D, GLTextureParameterName.WrapT, (int)textureWrapModeT);
			});
		}

		public void Update(byte[] data)
		{
			ChangeTextureParams(() =>
			{
				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pointer = handle.AddrOfPinnedObject();
				Application.GL.TexSubImage2D(GLTextureTarget.Texture2D, 0, 0, 0, (int)Size.X, (int)Size.Y, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, pointer);
				handle.Free();
			});
		}

		public void Clear()
		{
			var data = new byte[(int)(Size.X * Size.Y * 4)];
			for (var i = 0; i < data.Length; i += 4)
			{
				data[i + 0] = initialColors.r;
				data[i + 1] = initialColors.g;
				data[i + 2] = initialColors.b;
				data[i + 3] = initialColors.a;
			}
			Update(data);
		}
	}
}
