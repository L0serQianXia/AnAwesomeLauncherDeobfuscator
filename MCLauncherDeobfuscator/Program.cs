using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Reflection;
using System.IO;

namespace MCLauncherDeobfuscator
{
	class Program
	{
		static void Main(string[] args)
		{
			string path = "WPFLauncher.exe"; // It is an awesome Minecraft launcher, isn't it?
			if (!File.Exists("WPFLauncher.exe"))
			{
				Console.WriteLine("Please input the file path:");
				path = Console.ReadLine();
			}


			Console.WriteLine("Starting...");
			ModuleContext modCtx = ModuleDef.CreateModuleContext();
			ModuleDefMD module = ModuleDefMD.Load(@path, modCtx);
			Assembly asm = Assembly.LoadFrom(@path);

			int times = 0;
			DecryptStrings(ref times, module, asm);
			Console.WriteLine("Finished. Decrypted {0} strings", times);


			Console.WriteLine("Saving...");
			path = path.Replace(".exe", ".deobf.exe");
			module.Write(@path);
		

			Console.WriteLine("Finished.");
			Console.Read(); // wait for the user
		}

		private static void DecryptStrings(ref int times, ModuleDefMD module, Assembly asm)
		{
			// Start foreach the codes
			foreach (TypeDef type in module.Types)
			{
				foreach (MethodDef method in type.Methods)
				{
					// check if the method has any instruction.
					if(!method.HasBody)
					{
						continue;
					}
					for (int i = 0; i < method.Body.Instructions.Count; i++)
					{
						Instruction inst = method.Body.Instructions[i];
						// find Ldcs
						if (inst.OpCode.Equals(OpCodes.Ldstr))
						{

							Instruction next = method.Body.Instructions[i + 1];     // next is decryption number?
							Instruction mayCall = method.Body.Instructions[i + 2];  // next next is call?

							// check the opcode
							if (!next.OpCode.Equals(OpCodes.Ldc_I4) || !mayCall.OpCode.Equals(OpCodes.Call))
							{
								continue;
							}

							MethodDef methodCall = (MethodDef)mayCall.Operand;
							// invoke the decryption method to decrypt the string
							string result = (string)asm.ManifestModule.ResolveMethod(methodCall.MDToken.ToInt32()).Invoke(null, new Object[] { inst.Operand, next.Operand });

							// replace the old string with the decrypted string
							inst.Operand = result;
							next.OpCode = OpCodes.Nop;
							mayCall.OpCode = OpCodes.Nop;
							times++;
						}
					}
				}
			}
		}
	}
}
