using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace Pri.Diligent
{
	class Program
	{
		private static TextWriter Out;
		static void Main(string[] args)
		{
			Out = Console.Out;
			Out.WriteLine("Hello World!");

			using (var session = new TraceEventSession("pri"))
			{
				void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
				{
					Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
					session?.Dispose();
				}

				Console.CancelKeyPress += ConsoleOnCancelKeyPress;
				session.EnableKernelProvider(KernelTraceEventParser.Keywords.Registry);

				session.Source.Kernel.RegistryCreate += KernelParser_RegistryCreate;
				session.Source.Kernel.RegistrySetValue += KernelParser_RegistrySetValue;
				session.Source.Kernel.RegistryDeleteValue += KernelParser_RegistryDeleteValue;
				session.Source.Kernel.RegistryDelete += KernelParser_RegistryDelete;
				session.Source.Kernel.RegistryKCBCreate += KernelParser_RegistryKCBCreate;
				session.Source.Kernel.RegistryKCBDelete += KernelParser_RegistryKCBDelete;
				session.Source.Kernel.RegistryKCBRundownBegin += Kernel_RegistryKCBRundownBegin;
				session.Source.Kernel.RegistryKCBRundownEnd += Kernel_RegistryKCBRundownEnd;

				Out.WriteLine("Collecting events");
				session.Source.Process();
				Out.WriteLine("Stopping collection of events");
			}
		}

		private static void Kernel_RegistryKCBRundownBegin(RegistryTraceData obj)
		{
			Out.WriteLine($"RundownBegin {obj.KeyHandle}, {obj.KeyName}");
			kcbDictionary.TryAdd(obj.KeyHandle, obj.KeyName);
		}

		private static void Kernel_RegistryKCBRundownEnd(RegistryTraceData obj)
		{
			Out.WriteLine($"RundownEnd {obj.KeyHandle}, {obj.KeyName}");
			if (kcbDictionary.ContainsKey(obj.KeyHandle)) kcbDictionary.Remove(obj.KeyHandle);
		}

		static Dictionary<ulong, string> kcbDictionary = new Dictionary<ulong, string>();

		private static void Dump(RegistryTraceData registryTraceData, [CallerMemberName] string memberName = null)
		{
			string fullKeyName;
			if (registryTraceData.KeyHandle == 0)
			{
				fullKeyName = registryTraceData.KeyName;
			}
			else
			{
				if (kcbDictionary.TryGetValue(registryTraceData.KeyHandle, out var baseKeyName))
					fullKeyName = $"{baseKeyName}\\{registryTraceData.KeyName}";
				else
					fullKeyName = $"...\\{registryTraceData.KeyName}";
			}

			if(!string.IsNullOrWhiteSpace(memberName)) Console.WriteLine($"{memberName} {{");
			Out.WriteLine($"\tProcess: {registryTraceData.ProcessName}, Key Name: {fullKeyName}, Value Name: {registryTraceData.ValueName}");
			//Console.WriteLine($"\t{registryTraceData.Dump()}");
			if (!string.IsNullOrWhiteSpace(memberName)) Console.WriteLine("}");
		}

		private static void KernelParser_RegistryKCBDelete(RegistryTraceData obj)
		{
			if(kcbDictionary.ContainsKey(obj.KeyHandle)) kcbDictionary.Remove(obj.KeyHandle);
		}

		private static void KernelParser_RegistryKCBCreate(RegistryTraceData obj)
		{
			kcbDictionary.TryAdd(obj.KeyHandle, obj.KeyName);
		}

		private static void KernelParser_RegistryDelete(RegistryTraceData obj)
		{
			Dump(obj);
		}

		private static void KernelParser_RegistryDeleteValue(RegistryTraceData obj)
		{
			Dump(obj);
		}

		private static void KernelParser_RegistryCreate(RegistryTraceData obj)
		{
			Dump(obj);
		}

		private static void KernelParser_RegistrySetValue(RegistryTraceData obj)
		{
			Dump(obj);
		}
	}
}
