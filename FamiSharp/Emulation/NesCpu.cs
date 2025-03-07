namespace FamiSharp.Emulation
{
	public class NesCpu(NesSystem nes) : Cpu.Cpu
	{
		public override byte Read(ushort address) => nes.Read(address);
		public override void Write(ushort address, byte value) => nes.Write(address, value);
	}
}
