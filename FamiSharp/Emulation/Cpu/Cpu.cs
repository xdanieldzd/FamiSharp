using System.Runtime.CompilerServices;

namespace FamiSharp.Emulation.Cpu
{
	public enum Instruction
	{
		ADC, AND, ASL, BCC, BCS, BEQ, BIT, BMI,
		BNE, BPL, BRK, BVC, BVS, CLC, CLD, CLI,
		CLV, CMP, CPX, CPY, DEC, DEX, DEY, EOR,
		INC, INX, INY, JMP, JSR, LDA, LDX, LDY,
		LSR, NOP, ORA, PHA, PHP, PLA, PLP, ROL,
		ROR, RTI, RTS, SBC, SEC, SED, SEI, STA,
		STX, STY, TAX, TAY, TSX, TXA, TXS, TYA
	}

	public enum AddressingMode
	{
		ACC, ABS, ABX, ABY, IMM, IMP, IND, IZX,
		IZY, REL, ZPG, ZPX, ZPY
	}

	public abstract class Cpu
	{
		const ushort nmiVectorAddress = 0xFFFA;
		const ushort resVectorAddress = 0xFFFC;
		const ushort irqVectorAddress = 0xFFFE;

		static readonly byte[] cycleCounts =
		[
			7, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,	/* 0x */
			2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7, /* 1x */
			6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6, /* 2x */
			2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7, /* 3x */
			6, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6, /* 4x */
			2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7, /* 5x */
			6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6, /* 6x */
			2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7, /* 7x */
			2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4, /* 8x */
			2, 6, 2, 6, 4, 4, 4, 4, 2, 5, 2, 5, 5, 5, 5, 5, /* 9x */
			2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4, /* Ax */
			2, 5, 2, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4, /* Bx */
			2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6, /* Cx */
			2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7, /* Dx */
			2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6, /* Ex */
			2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 7, 7  /* Fx */
		];

		static readonly Dictionary<AddressingMode, Func<Cpu, bool>> addressingModeMap = new()
		{
			{ AddressingMode.ACC, AddressingModeACC }, { AddressingMode.ABS, AddressingModeABS }, { AddressingMode.ABX, AddressingModeABX }, { AddressingMode.ABY, AddressingModeABY },
			{ AddressingMode.IMM, AddressingModeIMM }, { AddressingMode.IMP, AddressingModeIMP }, { AddressingMode.IND, AddressingModeIND }, { AddressingMode.IZX, AddressingModeIZX },
			{ AddressingMode.IZY, AddressingModeIZY }, { AddressingMode.REL, AddressingModeREL }, { AddressingMode.ZPG, AddressingModeZPG }, { AddressingMode.ZPX, AddressingModeZPX },
			{ AddressingMode.ZPY, AddressingModeZPY }
		};

		static readonly Dictionary<Instruction, Func<Cpu, bool>> instructionMap = new()
		{
			{ Instruction.ADC, InstructionADC }, { Instruction.AND, InstructionAND }, { Instruction.ASL, InstructionASL }, { Instruction.BCC, InstructionBCC },
			{ Instruction.BCS, InstructionBCS }, { Instruction.BEQ, InstructionBEQ }, { Instruction.BIT, InstructionBIT }, { Instruction.BMI, InstructionBMI },
			{ Instruction.BNE, InstructionBNE }, { Instruction.BPL, InstructionBPL }, { Instruction.BRK, InstructionBRK }, { Instruction.BVC, InstructionBVC },
			{ Instruction.BVS, InstructionBVS }, { Instruction.CLC, InstructionCLC }, { Instruction.CLD, InstructionCLD }, { Instruction.CLI, InstructionCLI },
			{ Instruction.CLV, InstructionCLV }, { Instruction.CMP, InstructionCMP }, { Instruction.CPX, InstructionCPX }, { Instruction.CPY, InstructionCPY },
			{ Instruction.DEC, InstructionDEC }, { Instruction.DEX, InstructionDEX }, { Instruction.DEY, InstructionDEY }, { Instruction.EOR, InstructionEOR },
			{ Instruction.INC, InstructionINC }, { Instruction.INX, InstructionINX }, { Instruction.INY, InstructionINY }, { Instruction.JMP, InstructionJMP },
			{ Instruction.JSR, InstructionJSR }, { Instruction.LDA, InstructionLDA }, { Instruction.LDX, InstructionLDX }, { Instruction.LDY, InstructionLDY },
			{ Instruction.LSR, InstructionLSR }, { Instruction.NOP, InstructionNOP }, { Instruction.ORA, InstructionORA }, { Instruction.PHA, InstructionPHA },
			{ Instruction.PHP, InstructionPHP }, { Instruction.PLA, InstructionPLA }, { Instruction.PLP, InstructionPLP }, { Instruction.ROL, InstructionROL },
			{ Instruction.ROR, InstructionROR }, { Instruction.RTI, InstructionRTI }, { Instruction.RTS, InstructionRTS }, { Instruction.SBC, InstructionSBC },
			{ Instruction.SEC, InstructionSEC }, { Instruction.SED, InstructionSED }, { Instruction.SEI, InstructionSEI }, { Instruction.STA, InstructionSTA },
			{ Instruction.STX, InstructionSTX }, { Instruction.STY, InstructionSTY }, { Instruction.TAX, InstructionTAX }, { Instruction.TAY, InstructionTAY },
			{ Instruction.TSX, InstructionTSX }, { Instruction.TXA, InstructionTXA }, { Instruction.TXS, InstructionTXS }, { Instruction.TYA, InstructionTYA }
		};

		public ushort PC { get; set; }
		public byte S { get; set; }
		public ProcessorStatus P { get; set; } = new();
		public byte A { get; set; }
		public byte X { get; set; }
		public byte Y { get; set; }

		public byte Opcode { get; private set; }
		public Instruction Instruction { get; private set; } = Instruction.NOP;
		public AddressingMode AddressingMode { get; private set; } = AddressingMode.IMP;

		public ushort Address { get; private set; }
		public byte Data { get; private set; }
		public int Cycles { get; private set; }

		public abstract byte Read(ushort address);
		public abstract void Write(ushort address, byte value);

		public void Reset()
		{
			PC = (ushort)(Read(resVectorAddress + 1) << 8 | Read(resVectorAddress + 0));

			A = X = Y = 0;
			S = 0xFD;
			P = 0;

			Opcode = 0;
			Instruction = Instruction.NOP;
			AddressingMode = AddressingMode.IMP;

			Address = 0;
			Data = 0;

			Cycles = 8;
		}

		public void IRQ()
		{
			if (!P.I)
			{
				PushPC();
				PushP();
				P.I = true;

				PC = (ushort)(Read(irqVectorAddress + 1) << 8 | Read(irqVectorAddress + 0));
				Cycles = 7;
			}
		}

		public void NMI()
		{
			PushPC();
			PushP();
			P.I = true;

			PC = (ushort)(Read(nmiVectorAddress + 1) << 8 | Read(nmiVectorAddress + 0));
			Cycles = 8;
		}

		public void Tick()
		{
			if (Cycles == 0)
			{
				Opcode = Read(PC++);
				(Instruction, AddressingMode) = Decode(Opcode);
				Cycles = cycleCounts[Opcode];

				var addressingPenalty = addressingModeMap[AddressingMode](this);
				var instructionPenalty = instructionMap[Instruction](this);
				if (addressingPenalty && instructionPenalty) Cycles++;
			}

			Cycles--;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte Fetch()
		{
			if (AddressingMode != AddressingMode.ACC)
				Data = Read(Address);
			return Data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static (Instruction, AddressingMode) Decode(byte opcode)
		{
			/* https://www.masswerk.at/6502/6502_instruction_set.html#layout */

			var (a, b, c) = (opcode >> 5 & 0b111, opcode >> 2 & 0b111, opcode >> 0 & 0b11);
			var instruction = Instruction.NOP;
			var addressingMode = AddressingMode.IMP;

			switch (c)
			{
				case 0b01:
					switch (a)
					{
						case 0: instruction = Instruction.ORA; break;
						case 1: instruction = Instruction.AND; break;
						case 2: instruction = Instruction.EOR; break;
						case 3: instruction = Instruction.ADC; break;
						case 4: instruction = Instruction.STA; break;
						case 5: instruction = Instruction.LDA; break;
						case 6: instruction = Instruction.CMP; break;
						case 7: instruction = Instruction.SBC; break;
					}

					switch (b)
					{
						case 0: addressingMode = AddressingMode.IZX; break;
						case 1: addressingMode = AddressingMode.ZPG; break;
						case 2: addressingMode = AddressingMode.IMM; break;
						case 3: addressingMode = AddressingMode.ABS; break;
						case 4: addressingMode = AddressingMode.IZY; break;
						case 5: addressingMode = AddressingMode.ZPX; break;
						case 6: addressingMode = AddressingMode.ABY; break;
						case 7: addressingMode = AddressingMode.ABX; break;
					}
					break;

				case 0b10:
					switch (a)
					{
						case 0: instruction = Instruction.ASL; break;
						case 1: instruction = Instruction.ROL; break;
						case 2: instruction = Instruction.LSR; break;
						case 3: instruction = Instruction.ROR; break;
						case 4: instruction = b == 2 ? Instruction.TXA : b == 6 ? Instruction.TXS : Instruction.STX; break;
						case 5: instruction = b == 2 ? Instruction.TAX : b == 6 ? Instruction.TSX : Instruction.LDX; break;
						case 6: instruction = b == 2 ? Instruction.DEX : Instruction.DEC; break;
						case 7: instruction = b == 2 ? Instruction.NOP : Instruction.INC; break;
					}

					switch (b)
					{
						case 0: addressingMode = AddressingMode.IMM; break;
						case 1: addressingMode = AddressingMode.ZPG; break;
						case 2: addressingMode = AddressingMode.ACC; break;
						case 3: addressingMode = AddressingMode.ABS; break;
						case 4: addressingMode = AddressingMode.IMP; break;
						case 5: addressingMode = a == 4 || a == 5 ? AddressingMode.ZPY : AddressingMode.ZPX; break;
						case 6: addressingMode = AddressingMode.IMP; break;
						case 7: addressingMode = a == 5 ? AddressingMode.ABY : AddressingMode.ABX; break;
					}
					break;

				case 0b00:
					switch (b)
					{
						case 0: addressingMode = AddressingMode.IMM; break;
						case 1: addressingMode = AddressingMode.ZPG; break;
						case 2: addressingMode = AddressingMode.IMP; break;
						case 3: addressingMode = AddressingMode.ABS; break;
						case 4: addressingMode = AddressingMode.REL; break;
						case 5: addressingMode = AddressingMode.ZPX; break;
						case 6: addressingMode = AddressingMode.IMP; break;
						case 7: addressingMode = AddressingMode.ABX; break;
					}

					if (b == 0 || b == 1 || b == 3 || b == 5 || b == 7)
					{
						switch (a)
						{
							case 0: instruction = Instruction.NOP; break;
							case 1: instruction = Instruction.BIT; break;
							case 2: instruction = Instruction.JMP; break;
							case 3: instruction = Instruction.JMP; break;
							case 4: instruction = Instruction.STY; break;
							case 5: instruction = Instruction.LDY; break;
							case 6: instruction = Instruction.CPY; break;
							case 7: instruction = Instruction.CPX; break;
						}
					}
					else if (b == 2)
					{
						switch (a)
						{
							case 0: instruction = Instruction.PHP; break;
							case 1: instruction = Instruction.PLP; break;
							case 2: instruction = Instruction.PHA; break;
							case 3: instruction = Instruction.PLA; break;
							case 4: instruction = Instruction.DEY; break;
							case 5: instruction = Instruction.TAY; break;
							case 6: instruction = Instruction.INY; break;
							case 7: instruction = Instruction.INX; break;
						}
					}
					else if (b == 4)
					{
						switch (a)
						{
							case 0: instruction = Instruction.BPL; break;
							case 1: instruction = Instruction.BMI; break;
							case 2: instruction = Instruction.BVC; break;
							case 3: instruction = Instruction.BVS; break;
							case 4: instruction = Instruction.BCC; break;
							case 5: instruction = Instruction.BCS; break;
							case 6: instruction = Instruction.BNE; break;
							case 7: instruction = Instruction.BEQ; break;
						}
					}
					else if (b == 6)
					{
						switch (a)
						{
							case 0: instruction = Instruction.CLC; break;
							case 1: instruction = Instruction.SEC; break;
							case 2: instruction = Instruction.CLI; break;
							case 3: instruction = Instruction.SEI; break;
							case 4: instruction = Instruction.TYA; break;
							case 5: instruction = Instruction.CLV; break;
							case 6: instruction = Instruction.CLD; break;
							case 7: instruction = Instruction.SED; break;
						}
					}

					switch ((a, b))
					{
						case (0, 0): instruction = Instruction.BRK; addressingMode = AddressingMode.IMP; break;
						case (1, 0): instruction = Instruction.JSR; addressingMode = AddressingMode.ABS; break;
						case (2, 0): instruction = Instruction.RTI; addressingMode = AddressingMode.IMP; break;
						case (3, 0): instruction = Instruction.RTS; addressingMode = AddressingMode.IMP; break;
						case (3, 3): addressingMode = AddressingMode.IND; break;
					}
					break;
			}

			return (instruction, addressingMode);
		}

		public (byte?[], string) Disassemble(ushort address)
		{
			(byte?, byte?) getOperands(AddressingMode mode, ref ushort address) => mode switch
			{
				AddressingMode.ABS or AddressingMode.ABX or AddressingMode.ABY or AddressingMode.IND => (Read(address++), Read(address++)),
				AddressingMode.IMM or AddressingMode.ZPG or AddressingMode.ZPX or AddressingMode.ZPY or AddressingMode.IZX or AddressingMode.IZY or AddressingMode.REL => (Read(address++), null),
				_ => (null, null),
			};

			static string disasmOperands(AddressingMode mode, byte? operand1, byte? operand2, ushort address) => mode switch
			{
				AddressingMode.ACC => "A",
				AddressingMode.ABS => $"${operand2:X2}{operand1:X2}",
				AddressingMode.ABX => $"${operand2:X2}{operand1:X2},X",
				AddressingMode.ABY => $"${operand2:X2}{operand1:X2},Y",
				AddressingMode.IMM => $"#${operand1:X2}",
				AddressingMode.IMP => string.Empty,
				AddressingMode.IND => $"(${operand2:X2}{operand1:X2})",
				AddressingMode.IZX => $"(${operand1:X2},X)",
				AddressingMode.IZY => $"(${operand1:X2}),Y",
				AddressingMode.REL => $"${address + (sbyte?)operand1:X4}",
				AddressingMode.ZPG => $"${operand1:X2}",
				AddressingMode.ZPX => $"${operand1:X2},X",
				AddressingMode.ZPY => $"${operand1:X2},Y",
				_ => string.Empty
			};

			var opcode = Read(address++);
			var (instruction, addressingMode) = Decode(opcode);
			var bytes = new byte?[] { opcode, null, null };

			var (operand1, operand2) = getOperands(addressingMode, ref address);
			if (operand1.HasValue) bytes[1] = (byte)operand1;
			if (operand2.HasValue) bytes[2] = (byte)operand2;

			return (bytes, $"{instruction} {disasmOperands(addressingMode, operand1, operand2, address)}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsZero(int value) => (value & 0x00FF) == 0;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsNegative(int value) => (value & 0x0080) != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PushPC()
		{
			Write((ushort)(0x0100 + S), (byte)((PC >> 8) & 0x00FF)); S--;
			Write((ushort)(0x0100 + S), (byte)((PC >> 0) & 0x00FF)); S--;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PushP(bool brk = false, bool bit5 = false)
		{
			Write((ushort)(0x0100 + S), (byte)(P | (brk ? 1 << 4 : 0) | (bit5 ? 1 << 5 : 0))); S--;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PopPC()
		{
			S++; PC = Read((ushort)(0x0100 + S));
			S++; PC |= (ushort)(Read((ushort)(0x0100 + S)) << 8);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PopP()
		{
			S++; P = Read((ushort)(0x0100 + S));
		}

		/* Accumulator */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeACC(Cpu p)
		{
			p.Data = p.A;
			return false;
		}

		/* Absolute */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeABS(Cpu p)
		{
			var lo = p.Read(p.PC++);
			var hi = p.Read(p.PC++);
			p.Address = (ushort)(hi << 8 | lo);
			return false;
		}

		/* X-Indexed Absolute */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeABX(Cpu p)
		{
			var lo = p.Read(p.PC++);
			var hi = p.Read(p.PC++);
			p.Address = (ushort)((hi << 8 | lo) + p.X);
			return (p.Address & 0xFF00) != hi << 8;
		}

		/* Y-Indexed Absolute */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeABY(Cpu p)
		{
			var lo = p.Read(p.PC++);
			var hi = p.Read(p.PC++);
			p.Address = (ushort)((hi << 8 | lo) + p.Y);
			return (p.Address & 0xFF00) != hi << 8;
		}

		/* Immediate */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeIMM(Cpu p)
		{
			p.Address = p.PC++;
			return false;
		}

		/* Implied */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeIMP(Cpu _)
		{
			return false;
		}

		/* Indirect */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeIND(Cpu p)
		{
			var lo = p.Read(p.PC++);
			var hi = p.Read(p.PC++);
			var pointer = (ushort)(hi << 8 | lo);

			/* Page boundary bug */
			if (lo == 0xFF)
				p.Address = (ushort)(p.Read((ushort)(pointer & 0xFF00)) << 8 | p.Read((ushort)(pointer + 0)));
			else
				p.Address = (ushort)(p.Read((ushort)(pointer + 1)) << 8 | p.Read((ushort)(pointer + 0)));

			return false;
		}

		/* X-Indexed Zero Page Indirect */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeIZX(Cpu p)
		{
			var ofs = p.Read(p.PC++);
			var lo = p.Read((ushort)(ofs + p.X + 0 & 0x00FF));
			var hi = p.Read((ushort)(ofs + p.X + 1 & 0x00FF));
			p.Address = (ushort)(hi << 8 | lo);
			return false;
		}

		/* Zero Page Indirect Y-Indexed */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeIZY(Cpu p)
		{
			var ofs = p.Read(p.PC++);
			var lo = p.Read((ushort)(ofs + 0 & 0x00FF));
			var hi = p.Read((ushort)(ofs + 1 & 0x00FF));
			p.Address = (ushort)((hi << 8 | lo) + p.Y);
			return (p.Address & 0xFF00) != hi << 8;
		}

		/* Relative */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeREL(Cpu p)
		{
			p.Address = p.Read(p.PC++);
			if (IsNegative(p.Address)) p.Address |= 0xFF00;
			return false;
		}

		/* Zero Page */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeZPG(Cpu p)
		{
			p.Address = p.Read(p.PC++);
			p.Address &= 0x00FF;
			return false;
		}

		/* X-Indexed Zero Page */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeZPX(Cpu p)
		{
			p.Address = (ushort)(p.Read(p.PC++) + p.X);
			p.Address &= 0x00FF;
			return false;
		}

		/* Y-Indexed Zero Page */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool AddressingModeZPY(Cpu p)
		{
			p.Address = (ushort)(p.Read(p.PC++) + p.Y);
			p.Address &= 0x00FF;
			return false;
		}

		private static void ImplementationBranch(Cpu p, bool condition)
		{
			if (condition)
			{
				p.Cycles++;
				p.Address = (ushort)(p.PC + (short)p.Address);
				if ((p.Address & 0xFF00) != (p.PC & 0xFF00)) p.Cycles++;
				p.PC = p.Address;
			}
		}

		private static void ImplementationCompare(Cpu p, byte value)
		{
			p.Fetch();
			var temp = value - p.Data;
			p.P.C = value >= p.Data;
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(temp);
		}

		/* Add with carry */
		private static bool InstructionADC(Cpu p)
		{
			p.Fetch();

			var temp = p.A + p.Data + (ushort)(p.P.C ? 1 : 0);

			p.P.C = (temp & 0xFF00) != 0;
			p.P.Z = IsZero(temp);
			p.P.V = (~(p.A ^ p.Data) & (p.A ^ (ushort)temp) & 0x0080) != 0;
			p.P.N = IsNegative(temp);
			p.A = (byte)(temp & 0x00FF);

			return true;
		}

		/* And with accumulator */
		private static bool InstructionAND(Cpu p)
		{
			p.Fetch();
			p.A &= p.Data;
			p.P.Z = IsZero(p.A);
			p.P.N = IsNegative(p.A);
			return true;
		}

		/* Arithmetic shift left */
		private static bool InstructionASL(Cpu p)
		{
			p.Fetch();
			p.P.C = (p.Data & 0x80) != 0;

			var temp = (ushort)(p.Data << 1);
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(temp);

			if (p.AddressingMode == AddressingMode.ACC) p.A = (byte)(temp & 0xFF);
			else p.Write(p.Address, (byte)(temp & 0xFF));

			return false;
		}

		/* Branch if carry clear */
		private static bool InstructionBCC(Cpu p)
		{
			ImplementationBranch(p, !p.P.C);
			return false;
		}

		/* Branch if carry set */
		private static bool InstructionBCS(Cpu p)
		{
			ImplementationBranch(p, p.P.C);
			return false;
		}

		/* Branch if equal (= zero set) */
		private static bool InstructionBEQ(Cpu p)
		{
			ImplementationBranch(p, p.P.Z);
			return false;
		}

		/* Bit test */
		private static bool InstructionBIT(Cpu p)
		{
			p.Fetch();
			var temp = p.A & p.Data;
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(p.Data);
			p.P.V = (p.Data & 0x0040) != 0;
			return false;
		}

		/* Branch if minus (= negative set) */
		private static bool InstructionBMI(Cpu p)
		{
			ImplementationBranch(p, p.P.N);
			return false;
		}

		/* Branch if not equal (= zero clear) */
		private static bool InstructionBNE(Cpu p)
		{
			ImplementationBranch(p, !p.P.Z);
			return false;
		}

		/* Branch if plus (= negative clear) */
		private static bool InstructionBPL(Cpu p)
		{
			ImplementationBranch(p, !p.P.N);
			return false;
		}

		/* Break */
		private static bool InstructionBRK(Cpu p)
		{
			p.PC++;

			p.PushPC();
			p.PushP(true, false);
			p.P.I = true;

			p.PC = (ushort)(p.Read(irqVectorAddress + 1) << 8 | p.Read(irqVectorAddress + 0));
			return false;
		}

		/* Branch if overflow clear */
		private static bool InstructionBVC(Cpu p)
		{
			ImplementationBranch(p, !p.P.V);
			return false;
		}

		/* Branch if overflow set */
		private static bool InstructionBVS(Cpu p)
		{
			ImplementationBranch(p, p.P.V);
			return false;
		}

		/* Clear carry flag */
		private static bool InstructionCLC(Cpu p)
		{
			p.P.C = false;
			return false;
		}

		/* Clear decimal flag */
		private static bool InstructionCLD(Cpu p)
		{
			p.P.D = false;
			return false;
		}

		/* Clear interrupt disable flag */
		private static bool InstructionCLI(Cpu p)
		{
			p.P.I = false;
			return false;
		}

		/* Clear overflow flag */
		private static bool InstructionCLV(Cpu p)
		{
			p.P.V = false;
			return false;
		}

		/* Compare with accumulator */
		private static bool InstructionCMP(Cpu p)
		{
			ImplementationCompare(p, p.A);
			return true;
		}

		/* Compare with X */
		private static bool InstructionCPX(Cpu p)
		{
			ImplementationCompare(p, p.X);
			return false;
		}

		/* Compare with Y */
		private static bool InstructionCPY(Cpu p)
		{
			ImplementationCompare(p, p.Y);
			return false;
		}

		/* Decrement */
		private static bool InstructionDEC(Cpu p)
		{
			p.Fetch();
			var temp = p.Data - 1;
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(temp);
			p.Write(p.Address, (byte)(temp & 0xFF));
			return false;
		}

		/* Decrement X */
		private static bool InstructionDEX(Cpu p)
		{
			p.X--;
			p.P.Z = IsZero(p.X);
			p.P.N = IsNegative(p.X);
			return false;
		}

		/* Decrement Y */
		private static bool InstructionDEY(Cpu p)
		{
			p.Y--;
			p.P.Z = IsZero(p.Y);
			p.P.N = IsNegative(p.Y);
			return false;
		}

		/* Exclusive or with accumulator */
		private static bool InstructionEOR(Cpu p)
		{
			p.Fetch();
			p.A = (byte)(p.A ^ p.Data);
			p.P.Z = IsZero(p.A);
			p.P.N = IsNegative(p.A);
			return true;
		}

		/* Increment */
		private static bool InstructionINC(Cpu p)
		{
			p.Fetch();
			var temp = p.Data + 1;
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(temp);
			p.Write(p.Address, (byte)(temp & 0xFF));
			return false;
		}

		/* Increment X */
		private static bool InstructionINX(Cpu p)
		{
			p.X++;
			p.P.Z = IsZero(p.X);
			p.P.N = IsNegative(p.X);
			return false;
		}

		/* Increment Y */
		private static bool InstructionINY(Cpu p)
		{
			p.Y++;
			p.P.Z = IsZero(p.Y);
			p.P.N = IsNegative(p.Y);
			return false;
		}

		/* Jump */
		private static bool InstructionJMP(Cpu p)
		{
			p.PC = p.Address;
			return false;
		}

		/* Jump to subroutine */
		private static bool InstructionJSR(Cpu p)
		{
			p.PC--;
			p.PushPC();
			p.PC = p.Address;
			return false;
		}

		/* Load accumulator */
		private static bool InstructionLDA(Cpu p)
		{
			p.Fetch();
			p.A = p.Data;
			p.P.Z = IsZero(p.A);
			p.P.N = IsNegative(p.A);
			return true;
		}

		/* Load X */
		private static bool InstructionLDX(Cpu p)
		{
			p.Fetch();
			p.X = p.Data;
			p.P.Z = IsZero(p.X);
			p.P.N = IsNegative(p.X);
			return true;
		}

		/* Load Y */
		private static bool InstructionLDY(Cpu p)
		{
			p.Fetch();
			p.Y = p.Data;
			p.P.Z = IsZero(p.Y);
			p.P.N = IsNegative(p.Y);
			return true;
		}

		/* Logical shift right */
		private static bool InstructionLSR(Cpu p)
		{
			p.Fetch();
			p.P.C = (p.Data & 0x0001) != 0;

			var temp = p.Data >> 1;
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(temp);

			if (p.AddressingMode == AddressingMode.ACC) p.A = (byte)(temp & 0xFF);
			else p.Write(p.Address, (byte)(temp & 0xFF));

			return false;
		}

		/* No operation */
		private static bool InstructionNOP(Cpu _)
		{
			// TODO illegal opcodes
			return false;
		}

		/* Or with accumulator */
		private static bool InstructionORA(Cpu p)
		{
			p.Fetch();
			p.A = (byte)(p.A | p.Data);
			p.P.Z = IsZero(p.A);
			p.P.N = IsNegative(p.A);
			return true;
		}

		/* Push accumulator */
		private static bool InstructionPHA(Cpu p)
		{
			p.Write((ushort)(0x0100 + p.S), p.A);
			p.S--;
			return false;
		}

		/* Push processor status */
		private static bool InstructionPHP(Cpu p)
		{
			p.PushP(true, true);
			return false;
		}

		/* Pop accumulator */
		private static bool InstructionPLA(Cpu p)
		{
			p.S++;
			p.A = p.Read((ushort)(0x0100 + p.S));
			p.P.Z = IsZero(p.A);
			p.P.N = IsNegative(p.A);
			return false;
		}

		/* Pop processor status */
		private static bool InstructionPLP(Cpu p)
		{
			p.PopP();
			return false;
		}

		/* Rotate left */
		private static bool InstructionROL(Cpu p)
		{
			p.Fetch();

			var temp = (ushort)(p.Data << 1 | (ushort)(p.P.C ? 1 : 0));
			p.P.C = (temp & 0xFF00) != 0;
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(temp);

			if (p.AddressingMode == AddressingMode.ACC) p.A = (byte)(temp & 0xFF);
			else p.Write(p.Address, (byte)(temp & 0xFF));

			return false;
		}

		/* Rotate right */
		private static bool InstructionROR(Cpu p)
		{
			p.Fetch();

			var temp = (ushort)(p.Data >> 1 | (ushort)(p.P.C ? 1 : 0) << 7);
			p.P.C = (p.Data & 0x0001) != 0;
			p.P.Z = IsZero(temp);
			p.P.N = IsNegative(temp);

			if (p.AddressingMode == AddressingMode.ACC) p.A = (byte)(temp & 0xFF);
			else p.Write(p.Address, (byte)(temp & 0xFF));

			return false;
		}

		/* Return from interrupt */
		private static bool InstructionRTI(Cpu p)
		{
			p.PopP();
			p.PopPC();
			return false;
		}

		/* Return from subroutine */
		private static bool InstructionRTS(Cpu p)
		{
			p.PopPC();
			p.PC++;
			return false;
		}

		/* Subtract with carry */
		private static bool InstructionSBC(Cpu p)
		{
			p.Fetch();

			var temp = p.A + (p.Data ^ 0x00FF) + (ushort)(p.P.C ? 1 : 0);

			p.P.C = (temp & 0xFF00) != 0;
			p.P.Z = IsZero(temp);
			p.P.V = ((temp ^ p.A) & ((ushort)temp ^ (p.Data ^ 0x00FF)) & 0x0080) != 0;
			p.P.N = IsNegative(temp);
			p.A = (byte)(temp & 0x00FF);

			return true;
		}

		/* Set carry flag */
		private static bool InstructionSEC(Cpu p)
		{
			p.P.C = true;
			return false;
		}

		/* Set decimal flag */
		private static bool InstructionSED(Cpu p)
		{
			p.P.D = true;
			return false;
		}

		/* Set interrupt disable flag */
		private static bool InstructionSEI(Cpu p)
		{
			p.P.I = true;
			return false;
		}

		/* Store accumulator */
		private static bool InstructionSTA(Cpu p)
		{
			p.Write(p.Address, p.A);
			return false;
		}

		/* Store X */
		private static bool InstructionSTX(Cpu p)
		{
			p.Write(p.Address, p.X);
			return false;
		}

		/* Store Y */
		private static bool InstructionSTY(Cpu p)
		{
			p.Write(p.Address, p.Y);
			return false;
		}

		/* Transfer accumulator to X */
		private static bool InstructionTAX(Cpu p)
		{
			p.X = p.A;
			p.P.Z = IsZero(p.X);
			p.P.N = IsNegative(p.X);
			return false;
		}

		/* Transfer accumulator to Y */
		private static bool InstructionTAY(Cpu p)
		{
			p.Y = p.A;
			p.P.Z = IsZero(p.Y);
			p.P.N = IsNegative(p.Y);
			return false;
		}

		/* Transfer stack pointer to X */
		private static bool InstructionTSX(Cpu p)
		{
			p.X = p.S;
			p.P.Z = IsZero(p.X);
			p.P.N = IsNegative(p.X);
			return false;
		}

		/* Transfer X to accumulator */
		private static bool InstructionTXA(Cpu p)
		{
			p.A = p.X;
			p.P.Z = IsZero(p.A);
			p.P.N = IsNegative(p.A);
			return false;
		}

		/* Transfer X to stack pointer */
		private static bool InstructionTXS(Cpu p)
		{
			p.S = p.X;
			return false;
		}

		/* Transfer Y to accumulator */
		private static bool InstructionTYA(Cpu p)
		{
			p.A = p.Y;
			p.P.Z = IsZero(p.A);
			p.P.N = IsNegative(p.A);
			return false;
		}
	}

	public class ProcessorStatus()
	{
		const byte bitNegative = 1 << 7;
		const byte bitOverflow = 1 << 6;
		const byte bitDecimal = 1 << 3;
		const byte bitInterrupt = 1 << 2;
		const byte bitZero = 1 << 1;
		const byte bitCarry = 1 << 0;

		public bool N { get; set; }
		public bool V { get; set; }
		public bool D { get; set; }
		public bool I { get; set; }
		public bool Z { get; set; }
		public bool C { get; set; }

		public ProcessorStatus(ProcessorStatus status) : this()
		{
			N = status.N;
			V = status.V;
			D = status.D;
			I = status.I;
			Z = status.Z;
			C = status.C;
		}

		public static implicit operator byte(ProcessorStatus status) => (byte)(
			(status.N ? bitNegative : 0) |
			(status.V ? bitOverflow : 0) |
			(status.D ? bitDecimal : 0) |
			(status.I ? bitInterrupt : 0) |
			(status.Z ? bitZero : 0) |
			(status.C ? bitCarry : 0));

		public static implicit operator ProcessorStatus(byte status) => new()
		{
			N = (status & bitNegative) != 0,
			V = (status & bitOverflow) != 0,
			D = (status & bitDecimal) != 0,
			I = (status & bitInterrupt) != 0,
			Z = (status & bitZero) != 0,
			C = (status & bitCarry) != 0
		};

		public override bool Equals(object? obj)
		{
			if (obj == null || obj is not ProcessorStatus processorStatus)
				return false;

			if (N != processorStatus.N || V != processorStatus.V ||
				D != processorStatus.D || I != processorStatus.I ||
				Z != processorStatus.Z || C != processorStatus.C)
				return false;

			return true;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(N, V, D, I, Z, C);
		}

		public override string ToString()
		{
			return
				(N ? "N" : "n") +
				(V ? "V" : "v") +
				"-" +
				"-" +
				(D ? "D" : "d") +
				(I ? "I" : "i") +
				(Z ? "Z" : "z") +
				(C ? "C" : "c");
		}
	}
}
