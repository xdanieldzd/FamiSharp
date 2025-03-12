using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL2;
using Hexa.NET.OpenGL;
using Hexa.NET.SDL2;
using HexaGen.Runtime;
using System.Diagnostics;
using SDLEvent = Hexa.NET.SDL2.SDLEvent;
using SDLWindow = Hexa.NET.SDL2.SDLWindow;

namespace FamiSharp
{
	public unsafe class Application : IDisposable
	{
		const uint initFlags = SDL.SDL_INIT_EVENTS | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_AUDIO | SDL.SDL_INIT_GAMECONTROLLER;
		const uint windowFlags = (uint)(SDLWindowFlags.Resizable | SDLWindowFlags.Opengl | SDLWindowFlags.Hidden);

		const int
			errorSdlInitFailed = -1, errorCreateWindowFailed = -2, errorGlInitFailed = -3, errorImguiContextFailed = -4,
			errorImguiImplSdl2Failed = -5, errorImguiImplGlFailed = -6, errorUnknownInitFailure = -69;

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

		public string ConfigurationFilename { get; set; } = "Config.json";

		public string ConfigurationPath => Path.Combine(DataDirectory, ConfigurationFilename);

		public float Framerate => guiIo.Framerate;

		private static Lazy<GL>? gl;
		public static GL GL => gl!.Value;

		public event Action<KeycodeEventArgs>? KeyDown, KeyUp;
		public event Action<DeltaTimeEventArgs>? Update, RenderApplication, RenderGUI;
		public event Action? Load, Shutdown;

		SDLWindow* sdlWindow;
		uint sdlWindowId;
		SDLGLContext glContext;
		ImGuiContextPtr guiContext;
		ImGuiIOPtr guiIo;
		ImGuiStylePtr guiStyle;

		bool initSdlSuccess, initOpenGlSuccess, initGuiSuccess, isRunning;
		bool disposed;

		public Application()
		{
			InitializeSDL();
			InitializeOpenGL();
			InitializeImGui();

			SDL.SetWindowPosition(sdlWindow, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK);
			SDL.ShowWindow(sdlWindow);

			isRunning = initSdlSuccess && initOpenGlSuccess && initGuiSuccess;

			if (!isRunning)
				FatalError("Failed to initialize application", errorUnknownInitFailure);

			OnLoad();
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
			}

			/* Free unmanaged resources */

			if (initGuiSuccess)
			{
				ImGuiImplOpenGL3.Shutdown();
				ImGuiImplSDL2.Shutdown();
				ImGui.DestroyContext();
			}

			if (initOpenGlSuccess)
				GL.Dispose();

			if (initSdlSuccess)
			{
				SDL.DestroyWindow(sdlWindow);
				SDL.Quit();
			}

			disposed = true;
		}

		private void InitializeSDL()
		{
			if (SDL.Init(initFlags) != 0)
				FatalError($"Failed to initialize SDL: {SDL.GetErrorS()}", errorSdlInitFailed);

			SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");

			sdlWindow = SDL.CreateWindow(Title, 32, 32, Width, Height, windowFlags);
			if (sdlWindow == null)
				FatalError($"Failed to create SDL window: {SDL.GetErrorS()}", errorCreateWindowFailed);

			sdlWindowId = SDL.GetWindowID(sdlWindow);

			initSdlSuccess = true;
		}

		private void InitializeOpenGL()
		{
			glContext = SDL.GLCreateContext(sdlWindow);
			if (glContext.IsNull)
				FatalError($"Failed to create OpenGL context: {SDL.GetErrorS()}", errorGlInitFailed);

			gl = new(() => new(new BindingsContext(sdlWindow, glContext)));

			GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1f);
			GL.SwapInterval(SwapInterval);

			initOpenGlSuccess = true;
		}

		private void InitializeImGui()
		{
			guiContext = ImGui.CreateContext();
			if (guiContext.IsNull)
				FatalError("Failed to create ImGui context", errorImguiContextFailed);

			ImGui.SetCurrentContext(guiContext);

			guiIo = ImGui.GetIO();
			guiIo.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;

			ImGui.StyleColorsDark();

			guiStyle = ImGui.GetStyle();
			guiStyle.WindowBorderSize = 0f;

			ImGuiImplSDL2.SetCurrentContext(guiContext);
			if (!ImGuiImplSDL2.InitForOpenGL(new SDLWindowPtr((Hexa.NET.ImGui.Backends.SDL2.SDLWindow*)sdlWindow), (void*)glContext.Handle))
				FatalError("Failed to initialize ImGui Impl SDL2", errorImguiImplSdl2Failed);

			ImGuiImplOpenGL3.SetCurrentContext(guiContext);
			if (!ImGuiImplOpenGL3.Init((byte*)null))
				FatalError("Failed to initialize ImGui Impl OpenGL3", errorImguiImplGlFailed);

			initGuiSuccess = true;
		}

		public virtual void OnLoad() => Load?.Invoke();
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

		private void FatalError(string error, int code)
		{
			ShowMessageBox("Fatal Error", $"{error}\nExit code {code}", SDLMessageBoxFlags.Error);

			SDL.Quit();

			Console.WriteLine($"Fatal error: {error}");
			Environment.Exit(code);
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
