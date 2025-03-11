namespace FamiSharp.Utilities
{
	public class AverageFramerate
	{
		readonly double[] frameTimes = [];
		int lastIndex;

		public double Average { get; private set; }

		public AverageFramerate(int bufferSize)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 1, nameof(bufferSize));

			frameTimes = new double[bufferSize];
			lastIndex = 0;
		}

		public void Update(double value)
		{
			frameTimes[lastIndex] = value;
			lastIndex = (lastIndex + 1) % frameTimes.Length;

			var sum = 0.0;
			foreach (var frameTime in frameTimes)
				sum += frameTime;

			Average = frameTimes.Length / sum;
		}

		public void Clear()
		{
			Array.Fill(frameTimes, 0);
			lastIndex = 0;

			Average = 0.0;
		}
	}
}
