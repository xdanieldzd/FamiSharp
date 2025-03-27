using FamiSharp.Utilities;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL2;
using Hexa.NET.OpenGL;
using Hexa.NET.SDL2;
using HexaGen.Runtime;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using SDLEvent = Hexa.NET.SDL2.SDLEvent;
using SDLWindow = Hexa.NET.SDL2.SDLWindow;

namespace FamiSharp
{
	public unsafe class Application : IDisposable
	{
		/* "Hello, brand new world ..." */

		const uint initFlags = SDL.SDL_INIT_EVENTS | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_AUDIO | SDL.SDL_INIT_GAMECONTROLLER;
		const uint windowFlags = (uint)(SDLWindowFlags.Resizable | SDLWindowFlags.Opengl | SDLWindowFlags.Hidden);

		enum ExitCode : int
		{
			NoError = 0,
			SdlInitFailed = -1,
			CreateWindowFailed = -2,
			AudioHandlerInitFailed = -3,
			GlInitFailed = -4,
			GlVersionFailed = -5,
			ImGuiContextFailed = -6,
			ImGuiImplSdl2Failed = -7,
			ImGuiImplGlFailed = -8,
			UnknownFailure = -69
		}

		public static ProductInformation ProductInformation => ProductInformation.GetProductInfo();
		public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProductInformation.Name);

		public virtual string Title
		{
			get;
			set => SDL.SetWindowTitle(sdlWindow, field = value);
		} = nameof(Application);

		public virtual int Width
		{
			get;
			set => SDL.SetWindowSize(sdlWindow, field = value, Height);
		} = 640;

		public virtual int Height
		{
			get;
			set => SDL.SetWindowSize(sdlWindow, Width, field = value);
		} = 480;

		public int SwapInterval
		{
			get;
			set => GL.SwapInterval(field = value);
		}

		public Vector3 BackgroundColor
		{
			get;
			set { field = value; GL.ClearColor(value.X, value.Y, value.Z, 1f); }
		} = new(0x3E / 255f, 0x4F / 255f, 0x65 / 255f); /* ❤️ 🧲 ❤️ */

		public bool EscToExit { get; set; }

		public string ConfigurationFilename { get; set; } = "Config.json";

		public string ConfigurationPath => Path.Combine(DataDirectory, ConfigurationFilename);

		public float Framerate => guiIo.Framerate;
		public AudioHandler AudioHandler { get; private set; } = new();

		private static Lazy<GL>? gl;
		public static GL GL => gl!.Value;

		public static GLInfo? GLInfo { get; private set; }

		public event Action<KeycodeEventArgs>? KeyDown, KeyUp;
		public event Action<DeltaTimeEventArgs>? Update, RenderApplication, RenderGUI;
		public event Action? Load, InitializeGUI, Shutdown;

		SDLWindow* sdlWindow;
		uint sdlWindowId;
		SDLGLContext glContext;
		ImGuiContextPtr guiContext;
		ImGuiIOPtr guiIo;
		ImGuiStylePtr guiStyle;

		bool initSdlSuccess, initOpenGlSuccess, initGuiSuccess, isRunning;
		bool disposed;

		public Application(Version? glVersion = default)
		{
			InitializeSDL();
			InitializeOpenGL(glVersion);
			InitializeImGui();

			isRunning = initSdlSuccess && initOpenGlSuccess && initGuiSuccess;

			if (!isRunning)
				FatalError("Failed to initialize application", ExitCode.UnknownFailure);

			/* "Fire, fire, light the fire ..." */

			OnInitializeGUI();
			OnLoad();

			SDL.SetWindowPosition(sdlWindow, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK);
			SDL.ShowWindow(sdlWindow);
		}

		~Application()
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
				/* Dispose managed resources */

				AudioHandler.Dispose();
			}

			/* Free unmanaged resources */

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

			disposed = true;

			/* "It's okay, we're all going down anyway ..." */
		}

		private void InitializeSDL()
		{
			if (SDL.Init(initFlags) != 0)
				FatalError($"Failed to initialize SDL: {SDL.GetErrorS()}", ExitCode.SdlInitFailed);

			SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");

			sdlWindow = SDL.CreateWindow(Title, 32, 32, Width, Height, windowFlags);
			if (sdlWindow == null)
				FatalError($"Failed to create SDL window: {SDL.GetErrorS()}", ExitCode.CreateWindowFailed);

			sdlWindowId = SDL.GetWindowID(sdlWindow);

			if (!AudioHandler.Initialize(2, 44100, 1024))
				FatalError($"Failed to initialize audio handler: {SDL.GetErrorS()}", ExitCode.AudioHandlerInitFailed);

			initSdlSuccess = true;
		}

		private void InitializeOpenGL(Version? version = default)
		{
			if (version != default)
			{
				SDL.GLSetAttribute(SDLGLattr.GlContextMajorVersion, version.Major);
				SDL.GLSetAttribute(SDLGLattr.GlContextMinorVersion, version.Minor);
			}

			glContext = SDL.GLCreateContext(sdlWindow);
			if (glContext.IsNull)
				FatalError($"Failed to create OpenGL context: {SDL.GetErrorS()}", ExitCode.GlInitFailed);

			gl = new(() => new(new BindingsContext(sdlWindow, glContext)));
			GLInfo = new GLInfo(gl.Value);

			if (version != default)
			{
				if (GLInfo.ContextVersion != version)
					FatalError($"Failed to set OpenGL context version: Requested {version}, got {GLInfo.ContextVersion}", ExitCode.GlVersionFailed);
			}

			GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1f);
			GL.SwapInterval(SwapInterval);

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
					if (EscToExit && e.Key.Keysym.Sym == (int)SDLKeyCode.Escape) Exit();
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

		public int ShowMessageBox(string title, string message, SDLMessageBoxFlags flags)
		{
			return SDL.ShowSimpleMessageBox((uint)flags, title, message, sdlWindow);
		}

		private void FatalError(string error, ExitCode code)
		{
			Console.WriteLine($"Fatal error! {error}");

			ShowMessageBox("Fatal Error", $"{error}\n\nApplication will now exit; exit code {(int)code} ({code}).", SDLMessageBoxFlags.Error);
			SDL.Quit();

			Environment.Exit((int)code);
		}
	}

	public sealed class ProductInformation(string name, string ver, string desc, string cpr)
	{
		public string Name { get; } = name;
		public string Version { get; } = ver;
		public string Description { get; } = desc;
		public string Copyright { get; } = cpr;

		internal static ProductInformation GetProductInfo()
		{
			if (string.IsNullOrEmpty(Environment.ProcessPath)) return new("Application Name", "0.0.0.0", "No description.", "No copyright.");
			var fileVersionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
			return new ProductInformation(fileVersionInfo.ProductName!, fileVersionInfo.ProductVersion!, fileVersionInfo.Comments!, fileVersionInfo.LegalCopyright!);
		}
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

	public unsafe sealed class GLInfo
	{
		public string Renderer { get; private set; } = string.Empty;
		public string Vendor { get; private set; } = string.Empty;
		public string Version { get; private set; } = string.Empty;
		public string ShadingLanguageVersion { get; private set; } = string.Empty;
		public int NumExtensions { get; private set; }
		public string[] Extensions { get; private set; } = [];
		public Version ContextVersion { get; private set; }
		public int MaxTextureSize { get; private set; }

		public GLInfo(GL gl)
		{
			Renderer = GetString(gl, GLStringName.Renderer);
			Vendor = GetString(gl, GLStringName.Vendor);
			Version = GetString(gl, GLStringName.Version);
			ShadingLanguageVersion = GetString(gl, GLStringName.ShadingLanguageVersion);

			var extensions = new List<string>();
			NumExtensions = GetInteger(gl, GLGetPName.NumExtensions);
			for (var i = 0; i < NumExtensions; i++)
			{
				var str = GetString(gl, GLStringName.Extensions, i);
				if (!string.IsNullOrWhiteSpace(str)) extensions.Add(str);
			}
			Extensions = [.. extensions];

			ContextVersion = new(GetInteger(gl, GLGetPName.MajorVersion), GetInteger(gl, GLGetPName.MinorVersion));
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
}
