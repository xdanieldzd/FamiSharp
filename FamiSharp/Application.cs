using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL2;
using Hexa.NET.OpenGL;
using Hexa.NET.SDL2;
using HexaGen.Runtime;
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
			errorImguiImplSdl2Failed = -5, errorImguiImplGlFailed = -6;

		private static Lazy<GL>? gl;
		public static GL GL => gl!.Value;

		public event Action<KeycodeEventArgs>? KeyDown, KeyUp;
		public event Action<DeltaTimeEventArgs>? Update, RenderApplication, RenderGUI;
		public event Action? Shutdown;

		string title = string.Empty;
		int width = 640, height = 480;
		Vector3 backgroundColor = Vector3.Zero;

		public string Title
		{
			get => title;
			set { if (initSdlSuccess) SDL.SetWindowTitle(sdlWindow, title = value); }
		}

		public int Width
		{
			get => width;
			set { if (initSdlSuccess) SDL.SetWindowSize(sdlWindow, width = value, height); }
		}

		public int Height
		{
			get => height;
			set { if (initSdlSuccess) SDL.SetWindowSize(sdlWindow, width, height = value); }
		}

		public Vector3 BackgroundColor
		{
			get => backgroundColor;
			set => backgroundColor = value;
		}

		SDLWindow* sdlWindow;
		uint sdlWindowId;
		SDLGLContext glContext;
		ImGuiContextPtr guiContext;
		ImGuiIOPtr guiIo;
		ImGuiStylePtr guiStyle;

		public float GuiFramerate => guiIo.Framerate;

		bool initSdlSuccess = false, initOpenGlSuccess = false, initGuiSuccess = false, isRunning = false;

		bool disposed = false;

		public Application(string title, int width, int height, int swapInterval = 0)
		{
			InitializeSDL();
			InitializeOpenGL(swapInterval);
			InitializeImGui();

			(Title, Width, Height) = (title, width, height);

			SDL.SetWindowPosition(sdlWindow, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK, (int)SDL.SDL_WINDOWPOS_CENTERED_MASK);
			SDL.ShowWindow(sdlWindow);

			isRunning = true;
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

			sdlWindow = SDL.CreateWindow(title, 32, 32, width, height, windowFlags);
			if (sdlWindow == null)
				FatalError($"Failed to create SDL window: {SDL.GetErrorS()}", errorCreateWindowFailed);

			sdlWindowId = SDL.GetWindowID(sdlWindow);

			initSdlSuccess = true;
		}

		private void InitializeOpenGL(int swapInterval)
		{
			glContext = SDL.GLCreateContext(sdlWindow);
			if (glContext.IsNull)
				FatalError($"Failed to create OpenGL context: {SDL.GetErrorS()}", errorGlInitFailed);

			gl = new(() => new(new BindingsContext(sdlWindow, glContext)));

			SDL.GLSetSwapInterval(swapInterval);

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
					OnKeyDown(new KeycodeEventArgs((SDLKeyCode)e.Key.Keysym.Sym, (SDLKeymod)e.Key.Keysym.Mod));
					break;

				case (uint)SDLEventType.Keyup:
					OnKeyUp(new KeycodeEventArgs((SDLKeyCode)e.Key.Keysym.Sym, (SDLKeymod)e.Key.Keysym.Mod));
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
				GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, 1f);
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

		private static void FatalError(string error, int code)
		{
			SDL.Quit();

			Console.WriteLine($"Fatal error: {error}");
			Console.ReadKey();
			Environment.Exit(code);
		}
	}

	internal unsafe class BindingsContext(SDLWindow* window, SDLGLContext context) : IGLContext
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

	public class KeycodeEventArgs(SDLKeyCode keycode, SDLKeymod modifier) : EventArgs
	{
		public SDLKeyCode Keycode { get; set; } = keycode;
		public SDLKeymod Modifier { get; set; } = modifier;
	}
}
