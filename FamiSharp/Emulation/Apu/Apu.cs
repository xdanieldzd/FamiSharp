namespace FamiSharp.Emulation.Apu
{
	// TODO

	public class Apu(int sampleRate)
	{
		readonly int cyclesPerSample = 1789773 / sampleRate;

		public int Cycle { get; private set; }

		public event EventHandler<SamplesEventArgs>? TransferSamples;

		/* 0x4015 write -- channel enable */
		public bool EnableDMC { get; private set; }
		public bool EnableNoise { get; private set; }
		public bool EnableTriangle { get; private set; }
		public bool EnablePulse2 { get; private set; }
		public bool EnablePulse1 { get; private set; }

		/* 0x4015 read -- APU status */
		public bool DMCInterruptFlag { get; private set; }
		public bool FrameInterruptFlag { get; private set; }

		//

		// TEMPORARY
		int tempSineWaveCounter;

		public void Reset()
		{
			Cycle = 0;

			EnableDMC = EnableNoise = EnableTriangle = EnablePulse2 = EnablePulse1 = false;
			DMCInterruptFlag = FrameInterruptFlag = false;
		}

		public byte ExternalRead(ushort address)
		{
			// TODO

			var value = (byte)0;

			switch (address & 0x001F)
			{
				case 0x0015:
					/* SND_CHN */
					FrameInterruptFlag = false;
					//
					break;
			}

			return value;
		}

		public void ExternalWrite(ushort address, byte value)
		{
			// TODO

			switch (address & 0x001F)
			{
				case 0x0015:
					/* SND_CHN */
					EnableDMC = (value & 0x10) != 0;
					EnableNoise = (value & 0x08) != 0;
					EnableTriangle = (value & 0x04) != 0;
					EnablePulse2 = (value & 0x02) != 0;
					EnablePulse1 = (value & 0x01) != 0;
					break;
			}
		}

		public void Tick()
		{
			//

			Cycle++;

			if (Cycle >= cyclesPerSample)
			{
				var sample = GenerateSample(tempSineWaveCounter++);
				Cycle -= cyclesPerSample;

				TransferSamples?.Invoke(this, new() { Samples = [sample, sample] });
			}
		}

		private static short GenTestSineWave(int amplitude, int sampleNumber, int sampleRate)
		{
			/* https://stackoverflow.com/a/45002609 */

			var time = sampleNumber / (double)sampleRate;
			return (short)(amplitude * Math.Sin(2f * Math.PI * 440f * time));
		}

		private short GenerateSample(int sampleNumber)
		{
			return GlobalVariables.OutputApuSineTest ? GenTestSineWave(2500, sampleNumber, sampleRate) : (short)0;
		}
	}

	public class SamplesEventArgs : EventArgs
	{
		public short[] Samples { get; set; } = [];
	}
}
