using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;
using System.Threading;
using CommandLine;

namespace AtemAudioMonitorSwitcher
{
    class Program
    {

		[Verb("list", isDefault: true, HelpText = "List switcher inputs")]
		public class DefaultVerbOption
		{
			[Value(0, Required = true, HelpText = "Switcher IP address")]
			public String IP { get; set; }
		}

		[Verb("monitor", HelpText = "monitor line levels")]
		public class MonitorVerbOption
		{
			[Value(0, Required = true, HelpText = "Switcher IP address")]
			public String IP { get; set; }
		}

		[Verb("autoswitch", HelpText = "auto switch between inputs based on line levels")]
		public class AutoSwitchVerbOption
		{
			[Value(0, Required = true, HelpText = "Switcher IP address")]
			public String IP { get; set; }

			[Option("mappings", HelpText = "Mapping of inputs in the form inputId/sourceId=outputSwitcherInputId , e.g. 1301/-255=1 1301/-256=2 ")]
			public IEnumerable<string> Mappings { get; set; } //sequence
		}


		static void Main(string[] args)
        {
			CommandLine.Parser.Default.ParseArguments<DefaultVerbOption, MonitorVerbOption, AutoSwitchVerbOption>(args)
			  .MapResult(
				(DefaultVerbOption opts) => RunListInputs(opts),
				(MonitorVerbOption opts) => RunMonitorLineLevels(opts),
				(AutoSwitchVerbOption opts) => RunAutoSwitch(opts),
				errors => 1);
		}

		static int RunListInputs(DefaultVerbOption opts)
		{
			AtemSwitcher atem = AtemSwitcher.Connect(opts.IP);
			if (atem == null)
            {
				return 1;
            }

			dumpInputsToConsole(atem);
			return 0;
		}
		static int RunMonitorLineLevels(MonitorVerbOption opts)
        {
			AtemSwitcher atem = AtemSwitcher.Connect(opts.IP);
			if (atem == null)
			{
				return 1;
			}
			AutoResetEvent evt = new AutoResetEvent(false);
			try
			{
				atem.startAudioMonitoring();
				evt.WaitOne();
				return 0;
			} catch (Exception e)
            {
				Console.WriteLine(e.Message);
				return 1;
			}
		}

		static int RunAutoSwitch(AutoSwitchVerbOption opts)
        {
			AtemSwitcher atem = AtemSwitcher.Connect(opts.IP);
			if (atem == null)
			{
				return 1;
			}
			AutoResetEvent evt = new AutoResetEvent(false);
			try
			{
				AutoSwitch autoswitch = new AutoSwitch(atem);
				autoswitch.setOptions(opts);
				autoswitch.run();
				evt.WaitOne();
				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return 1;
			}
		}

		protected static void dumpInputsToConsole(AtemSwitcher atem)
        {
			Console.WriteLine("Switcher Inputs:");
			Console.WriteLine("[id] name (PREVIEW/PROGRAM)");
			foreach (AtemSwitcherInput switcherInput in atem.GetSwitcherInputs())
			{
				String name;
				switcherInput.GetInput().GetShortName(out name);
				String programPreview = "";
				if (switcherInput.isPreviewTallied == 1) { programPreview = "(PREVIEW)"; }
				if (switcherInput.isProgramTallied == 1) { programPreview = "(PROGRAM)"; }
				Console.WriteLine("[" + switcherInput.inputId.ToString() + "] " + name + " " + programPreview);
			}
			Console.WriteLine("");

			Console.WriteLine("Audio inputs:");
			Console.WriteLine("[input id / source id] (off/REC) port / type");
			foreach (AtemAudioInputSource audioSource in atem.GetAudioInputSources()) {

				audioSource.m_audioSource.IsMixedIn(out int mixedIn);
				String mixedInString = "off";
				if (mixedIn == 1)
                {
					mixedInString = "REC";
                }

				Console.WriteLine(string.Format("[{0,6}/{1,6}] {2} {3} / {4} {5} ",
					audioSource.inputId,
					audioSource.sourceId,
					mixedInString,
					audioSource.getPortType(),
					audioSource.getInputType(),
					audioSource.getSourceType()
					));
			}
		}

	}


}
