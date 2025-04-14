using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL2;
using Hexa.NET.OpenGL;
using Hexa.NET.SDL2;
using HexaGen.Runtime;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using SDLEvent = Hexa.NET.SDL2.SDLEvent;
using SDLWindow = Hexa.NET.SDL2.SDLWindow;

namespace FamiSharp
{
	public unsafe class Application : BaseDisposable
	{
		/* "Hello, brand new world ..." */

		const uint initFlags = SDL.SDL_INIT_EVENTS | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_AUDIO | SDL.SDL_INIT_GAMECONTROLLER;

		enum ExitCode : int
		{
			NoError = 0,
			SdlInitFailed = NoError - 1,
			CreateWindowFailed = SdlInitFailed - 1,
			GlInitFailed = CreateWindowFailed - 1,
			GlVersionFailed = GlInitFailed - 1,
			ImGuiContextFailed = GlVersionFailed - 1,
			ImGuiImplSdl2Failed = ImGuiContextFailed - 1,
			ImGuiImplGlFailed = ImGuiImplSdl2Failed - 1,
			UnknownFailure = -64
		}

		internal readonly ApplicationSettings initialAppSettings;

		string title = nameof(Application);
		public string Title
		{
			get => title;
			set => SDL.SetWindowTitle(sdlWindow, title = value);
		}

		(int width, int height) clientSize = (640, 480);
		public (int Width, int Height) ClientSize
		{
			get => clientSize;
			set
			{
				clientSize = value;
				SDL.SetWindowSize(sdlWindow, value.Width, value.Height);
			}
		}

		int swapInterval = 1;
		public int SwapInterval
		{
			get => swapInterval;
			set => GL.SwapInterval(swapInterval = value);
		}

		Vector3 clearColor = Vector3.Zero;
		public Vector3 ClearColor
		{
			get => clearColor;
			set
			{
				clearColor = value;
				GL.ClearColor(value.X, value.Y, value.Z, 1f);
			}
		}

		bool escToExit;
		public bool EscToExit
		{
			get => escToExit;
			set => escToExit = value;
		}

		public float Framerate => guiIo.Framerate;

		private static Lazy<GL>? gl;
		public static GL GL => gl!.Value;

		public event Action<KeycodeEventArgs>? KeyDown, KeyUp;
		public event Action<DeltaTimeEventArgs>? Update, RenderApplication, RenderGUI;
		public event Action? Load, InitializeGUI, Shutdown;

		SDLWindow* sdlWindow;
		uint sdlWindowId;
		readonly SDLSurface* iconSdlSurface;
		SDLGLContext glContext;
		ImGuiContextPtr guiContext;
		ImGuiIOPtr guiIo;
		ImGuiStylePtr guiStyle;

		bool initSdlSuccess, initOpenGlSuccess, initGuiSuccess, isRunning;

		public Application(ApplicationSettings applicationSettings)
		{
			initialAppSettings = applicationSettings;

			title = initialAppSettings.Title;
			clientSize = initialAppSettings.ClientSize;
			swapInterval = initialAppSettings.VSync switch
			{
				VSyncMode.On => 1,
				VSyncMode.Adaptive => -1,
				_ => 0,
			};
			clearColor = initialAppSettings.ClearColor;
			escToExit = initialAppSettings.EscToExit;

			InitializeSDL(title, clientSize.width, clientSize.height, initialAppSettings.WindowFlags | SDLWindowFlags.Hidden);
			InitializeOpenGL(initialAppSettings.OpenGLVersion);
			InitializeImGui();

			if (initialAppSettings.Icon != null)
			{
				iconSdlSurface = SDL.CreateRGBSurfaceWithFormat(0, (int)initialAppSettings.Icon.Width, (int)initialAppSettings.Icon.Height, 32, (uint)SDLPixelFormatEnum.Abgr8888);
				fixed (void* pixelDataPtr = &initialAppSettings.Icon.PixelData[0])
					iconSdlSurface->Pixels = pixelDataPtr;
				SDL.SetWindowIcon(sdlWindow, iconSdlSurface);
			}

			isRunning = initSdlSuccess && initOpenGlSuccess && initGuiSuccess;

			if (!isRunning)
				FatalError("Failed to initialize application", ExitCode.UnknownFailure);

			/* "Fire, fire, light the fire ..." */

			OnInitializeGUI();
			OnLoad();

			SDL.SetWindowPosition(sdlWindow, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK);
			SDL.ShowWindow(sdlWindow);
		}

		private void InitializeSDL(string title, int width, int height, SDLWindowFlags windowFlags)
		{
			if (SDL.Init(initFlags) != 0)
				FatalError($"Failed to initialize SDL: {SDL.GetErrorS()}", ExitCode.SdlInitFailed);

			SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");

			sdlWindow = SDL.CreateWindow(title, 32, 32, width, height, (uint)windowFlags);
			if (sdlWindow == null)
				FatalError($"Failed to create SDL window: {SDL.GetErrorS()}", ExitCode.CreateWindowFailed);

			sdlWindowId = SDL.GetWindowID(sdlWindow);

			initSdlSuccess = true;
		}

		private void InitializeOpenGL(Version version)
		{
			if (version.Major != 0)
			{
				SDL.GLSetAttribute(SDLGLattr.GlContextMajorVersion, version.Major);
				SDL.GLSetAttribute(SDLGLattr.GlContextMinorVersion, version.Minor);
			}

			glContext = SDL.GLCreateContext(sdlWindow);
			if (glContext.IsNull)
				FatalError($"Failed to create OpenGL context: {SDL.GetErrorS()}", ExitCode.GlInitFailed);

			gl = new(() => new(new BindingsContext(sdlWindow, glContext)));

			if (version.Major != 0)
			{
				if (GL.GetContextInfo().Version != version)
					FatalError($"Failed to set OpenGL context version: Requested {version}, got {GL.GetContextInfo().Version}", ExitCode.GlVersionFailed);
			}

			GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1f);
			GL.SwapInterval(swapInterval);

			initOpenGlSuccess = true;
		}

		private void InitializeImGui()
		{
			guiContext = ImGui.CreateContext();
			if (guiContext.IsNull)
				FatalError("Failed to create ImGui context", ExitCode.ImGuiContextFailed);

			ImGui.SetCurrentContext(guiContext);

			guiIo = ImGui.GetIO();
			guiIo.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;

			ImGui.StyleColorsDark();

			guiStyle = ImGui.GetStyle();
			guiStyle.WindowBorderSize = 0f;

			ImGuiImplSDL2.SetCurrentContext(guiContext);
			if (!ImGuiImplSDL2.InitForOpenGL(new SDLWindowPtr((Hexa.NET.ImGui.Backends.SDL2.SDLWindow*)sdlWindow), (void*)glContext.Handle))
				FatalError("Failed to initialize ImGui Impl SDL2", ExitCode.ImGuiImplSdl2Failed);

			ImGuiImplOpenGL3.SetCurrentContext(guiContext);
			if (!ImGuiImplOpenGL3.Init((byte*)null))
				FatalError("Failed to initialize ImGui Impl OpenGL3", ExitCode.ImGuiImplGlFailed);

			initGuiSuccess = true;
		}

		public virtual void OnLoad() => Load?.Invoke();
		public virtual void OnInitializeGUI() => InitializeGUI?.Invoke();
		public virtual void OnKeyDown(KeycodeEventArgs e) => KeyDown?.Invoke(e);
		public virtual void OnKeyUp(KeycodeEventArgs e) => KeyUp?.Invoke(e);
		public virtual void OnUpdate(DeltaTimeEventArgs e) => Update?.Invoke(e);
		public virtual void OnRenderApplication(DeltaTimeEventArgs e) => RenderApplication?.Invoke(e);
		public virtual void OnRenderGUI(DeltaTimeEventArgs e) => RenderGUI?.Invoke(e);
		public virtual void OnShutdown() => Shutdown?.Invoke();

		private void HandleEvent(SDLEvent e)
		{
			switch (e.Type)
			{
				case (uint)SDLEventType.Quit:
				case (uint)SDLEventType.AppTerminating:
					isRunning = false;
					break;

				case (uint)SDLEventType.Windowevent:
					if (e.Window.WindowID == sdlWindowId && (SDLWindowEventID)e.Window.Event == SDLWindowEventID.Close)
						isRunning = false;
					break;

				case (uint)SDLEventType.Keydown:
					if (escToExit && e.Key.Keysym.Sym == (int)SDLKeyCode.Escape) Exit();
					OnKeyDown(new KeycodeEventArgs((SDLEventType)e.Key.Type, (SDLKeyCode)e.Key.Keysym.Sym, (SDLKeymod)e.Key.Keysym.Mod));
					break;

				case (uint)SDLEventType.Keyup:
					OnKeyUp(new KeycodeEventArgs((SDLEventType)e.Key.Type, (SDLKeyCode)e.Key.Keysym.Sym, (SDLKeymod)e.Key.Keysym.Mod));
					break;
			}
		}

		public void Exit()
		{
			isRunning = false;
		}

		public void Run()
		{
			SDLEvent currentEvent = default;

			while (isRunning)
			{
				OnUpdate(new(guiIo.DeltaTime));

				while (SDL.PollEvent(ref currentEvent) != 0)
				{
					ImGuiImplSDL2.ProcessEvent((Hexa.NET.ImGui.Backends.SDL2.SDLEvent*)&currentEvent);

					HandleEvent(currentEvent);
				}

				GL.MakeCurrent();
				GL.Clear(GLClearBufferMask.ColorBufferBit | GLClearBufferMask.DepthBufferBit);

				OnRenderApplication(new(guiIo.DeltaTime));

				ImGuiImplOpenGL3.NewFrame();
				ImGuiImplSDL2.NewFrame();
				ImGui.NewFrame();

				OnRenderGUI(new(guiIo.DeltaTime));

				ImGui.Render();
				ImGui.EndFrame();

				GL.MakeCurrent();
				ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

				if ((guiIo.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
				{
					ImGui.UpdatePlatformWindows();
					ImGui.RenderPlatformWindowsDefault();
				}

				GL.MakeCurrent();
				GL.SwapBuffers();
			}

			OnShutdown();

			Dispose();
		}

		public int ShowMessageBox(string title, string message, SDLMessageBoxFlags flags) => SDL.ShowSimpleMessageBox((uint)flags, title, message, sdlWindow);

		protected void FatalError(string error, int code) => FatalError(error, (ExitCode)code);

		private void FatalError(string error, ExitCode code)
		{
			Console.WriteLine($"Fatal error! {error}");

			ShowMessageBox("Fatal Error", $"{error}\n\nApplication will now exit; exit code {(Enum.IsDefined(code) ? $"{(int)code} ({code})" : $"{(int)code}")}.", SDLMessageBoxFlags.Error);
			SDL.Quit();

			Environment.Exit((int)code);
		}

		protected override void DisposeUnmanaged()
		{
			if (iconSdlSurface != null)
				SDL.FreeSurface(iconSdlSurface);

			if (initGuiSuccess)
			{
				ImGuiImplOpenGL3.Shutdown();
				ImGuiImplSDL2.Shutdown();
				ImGui.DestroyContext();
			}

			if (initOpenGlSuccess)
			{
				GL.Dispose();
				SDL.GLDeleteContext(glContext);
			}

			if (initSdlSuccess)
			{
				SDL.DestroyWindow(sdlWindow);
				SDL.Quit();
			}

			/* "It's okay, we're all going down anyway ..." */
		}
	}

	public enum VSyncMode { On, Off, Adaptive }

	public sealed class ApplicationSettings
	{
		public string Title { get; set; } = string.Empty;
		public (int Width, int Height) ClientSize { get; set; } = (640, 480);
		public RgbaFile? Icon { get; set; }
		public SDLWindowFlags WindowFlags { get; set; } = SDLWindowFlags.Opengl;
		public bool EscToExit { get; set; } = true;
		public Version OpenGLVersion { get; set; } = new();
		public VSyncMode VSync { get; set; } = VSyncMode.On;
		public Vector3 ClearColor { get; set; } = Vector3.Zero;

		public static ApplicationSettings Default => new();
	}

	internal unsafe sealed class BindingsContext(SDLWindow* window, SDLGLContext context) : IGLContext
	{
		public nint Handle => (nint)window;

		public bool IsCurrent => SDL.GLGetCurrentContext() == context;

		public void Dispose() { }

		public nint GetProcAddress(string procName) => (nint)SDL.GLGetProcAddress(procName);
		public bool IsExtensionSupported(string extensionName) => SDL.GLExtensionSupported(extensionName) == SDLBool.True;
		public void MakeCurrent() => SDL.GLMakeCurrent(window, context);
		public void SwapBuffers() => SDL.GLSwapWindow(window);
		public void SwapInterval(int interval) => SDL.GLSetSwapInterval(interval);

		public bool TryGetProcAddress(string procName, out nint procAddress)
		{
			procAddress = (nint)SDL.GLGetProcAddress(procName);
			return procAddress != 0;
		}
	}

	public static class GLExpansion
	{
		static GLContextInfo? contextInfo;

		public static GLContextInfo GetContextInfo(this GL gl)
		{
			contextInfo ??= new(gl);
			return contextInfo;
		}
	}

	public unsafe sealed class GLContextInfo
	{
		public string RendererString { get; private set; } = string.Empty;
		public string VendorString { get; private set; } = string.Empty;
		public string VersionString { get; private set; } = string.Empty;
		public string ShadingLanguageVersionString { get; private set; } = string.Empty;
		public int NumSupportedExtensions { get; private set; }
		public string[] SupportedExtensions { get; private set; } = [];
		public Version Version { get; private set; }
		public int MaxTextureSize { get; private set; }

		public GLContextInfo(GL gl)
		{
			RendererString = GetString(gl, GLStringName.Renderer);
			VendorString = GetString(gl, GLStringName.Vendor);
			VersionString = GetString(gl, GLStringName.Version);
			ShadingLanguageVersionString = GetString(gl, GLStringName.ShadingLanguageVersion);

			var extensions = new List<string>();
			NumSupportedExtensions = GetInteger(gl, GLGetPName.NumExtensions);
			for (var i = 0; i < NumSupportedExtensions; i++)
			{
				var str = GetString(gl, GLStringName.Extensions, i);
				if (!string.IsNullOrWhiteSpace(str)) extensions.Add(str);
			}
			SupportedExtensions = [.. extensions];

			Version = new(GetInteger(gl, GLGetPName.MajorVersion), GetInteger(gl, GLGetPName.MinorVersion));
			MaxTextureSize = GetInteger(gl, GLGetPName.MaxTextureSize);
		}

		private static string GetString(GL gl, GLStringName name, int index = -1)
		{
			var str = string.Empty;
			var ptr = index != -1 ? gl.GetStringi(name, (uint)index) : gl.GetString(name);
			if (ptr != null)
				str = Marshal.PtrToStringUTF8((nint)ptr) ?? string.Empty;
			return str;
		}

		private static int GetInteger(GL gl, GLGetPName pname)
		{
			gl.GetIntegerv(pname, out int value);
			return value;
		}
	}

	public class Resources
	{
		private static Stream? GetEmbeddedResourceStream(string name)
		{
			var assembly = Assembly.GetEntryAssembly();
			name = $"{assembly?.GetName().Name}.{name}";
			return assembly?.GetManifestResourceStream(name);
		}

		public static RgbaFile? GetEmbeddedRgbaFile(string name)
		{
			using var stream = GetEmbeddedResourceStream(name);
			if (stream == null) return null;
			return new RgbaFile(stream);
		}
	}

	public class RgbaFile
	{
		/* RGBA bitmap file format -- https://github.com/bzotto/rgba_bitmap
		 * ".rgba is the dumbest possible image interchange format, now available for your programming pleasure."
		 */

		const string expectedMagic = "RGBA";

		public string MagicNumber { get; protected set; }
		public uint Width { get; protected set; }
		public uint Height { get; protected set; }
		public byte[] PixelData { get; protected set; }

		public RgbaFile(string filename) : this(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }

		public RgbaFile(Stream stream)
		{
			MagicNumber = ReadString(stream, 4);
			Width = ReadUInt32(stream);
			Height = ReadUInt32(stream);
			PixelData = new byte[Width * Height * 4];
			stream.ReadExactly(PixelData);
		}

		public RgbaFile(uint width, uint height, byte[] pixelData)
		{
			MagicNumber = expectedMagic;
			Width = width;
			Height = height;
			PixelData = pixelData;
		}

		public void Save(string filename) => Save(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));

		public void Save(Stream stream)
		{
			WriteString(stream, MagicNumber);
			WriteUInt32(stream, Width);
			WriteUInt32(stream, Height);
			stream.Write(PixelData);
		}

		private static string ReadString(Stream stream, int length) => new([.. Enumerable.Range(0, length).Select(_ => (char)stream.ReadByte())]);
		private static uint ReadUInt32(Stream stream) => (uint)(((stream.ReadByte() & 0xFF) << 24) | ((stream.ReadByte() & 0xFF) << 16) | ((stream.ReadByte() & 0xFF) << 8) | ((stream.ReadByte() & 0xFF) << 0));

		private static void WriteString(Stream stream, string str) => Array.ForEach(str.ToCharArray(), (x) => stream.WriteByte((byte)x));
		private static void WriteUInt32(Stream stream, uint val) { stream.WriteByte((byte)((val >> 24) & 0xFF)); stream.WriteByte((byte)((val >> 16) & 0xFF)); stream.WriteByte((byte)((val >> 8) & 0xFF)); stream.WriteByte((byte)((val >> 0) & 0xFF)); }
	}

	public class DeltaTimeEventArgs(double delta) : EventArgs
	{
		public double Delta { get; set; } = delta;
	}

	public class KeycodeEventArgs(SDLEventType evtType, SDLKeyCode keycode, SDLKeymod modifier) : EventArgs
	{
		public SDLEventType EventType { get; set; } = evtType;
		public SDLKeyCode Keycode { get; set; } = keycode;
		public SDLKeymod Modifier { get; set; } = modifier;
	}

	public abstract class BaseDisposable : IDisposable
	{
		bool isDisposed;

		~BaseDisposable()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void DisposeManaged() { }
		protected virtual void DisposeUnmanaged() { }

		protected void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing) DisposeManaged();
				DisposeUnmanaged();

				isDisposed = true;
			}
		}
	}
}
