using Hexa.NET.SDL2;

namespace FamiSharp.Utilities
{
	public unsafe class AudioHandler : BaseDisposable
	{
		public byte Channels => audioSpec.Channels;
		public int SampleRate => audioSpec.Freq;
		public ushort Format => audioSpec.Format;
		public ushort Samples => audioSpec.Samples;

		SDLAudioSpec audioSpec;
		uint sdlAudioDeviceId;

		bool initSdlAudioSuccess;

		public bool Initialize(byte channels, int freq, ushort samples)
		{
			var requestedSpec = new SDLAudioSpec()
			{
				Channels = channels,
				Freq = freq,
				Format = SDL.AUDIO_S16SYS,
				Samples = samples,
				Callback = null
			};

			var devices = new List<string>();
			var numDevices = SDL.GetNumAudioDevices(0);
			for (var i = 0; i < numDevices; i++)
			{
				var deviceName = SDL.GetAudioDeviceNameS(i, 0);

				/* Workaround for "Steam Streaming Speakers/Microphone" being auto-selected, resulting in no sound... */
				if (!deviceName.Contains("Steam Streaming"))
					devices.Add(deviceName);
			}

			sdlAudioDeviceId = SDL.OpenAudioDevice(devices.FirstOrDefault(), 0, ref requestedSpec, ref audioSpec, 0);

			if (sdlAudioDeviceId != 0)
				SDL.PauseAudioDevice(sdlAudioDeviceId, 0);

			return initSdlAudioSuccess = (sdlAudioDeviceId != 0);
		}

		public void Output(short[] samples)
		{
			fixed (void* ptr = &samples[0])
			{
				SDL.QueueAudio(sdlAudioDeviceId, ptr, (uint)(samples.Length * sizeof(short)));
			}
		}

		protected override void DisposeUnmanaged()
		{
			if (initSdlAudioSuccess)
			{
				SDL.CloseAudioDevice(sdlAudioDeviceId);
			}
		}
	}
}
