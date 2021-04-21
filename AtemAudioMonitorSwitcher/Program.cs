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
		public class Options
		{
			[Value(0, Required = true, HelpText = "Switcher IP address")]
			public String IP { get; set; }

			[Option('m', "mode", Default = "list",  Required = false, HelpText = "Mode of operation : [ list | monitor ]")]
			public String Mode { get; set; }
		}

		static void Main(string[] args)
        {
			CommandLine.Parser.Default.ParseArguments<Options>(args)
				.WithParsed(RunOptions)
				.WithNotParsed(HandleParseError);
		}

		static void RunOptions(Options opts)
		{
			IBMDSwitcherDiscovery discovery = new CBMDSwitcherDiscovery();
			AtemSwitcher atem;
			_BMDSwitcherConnectToFailure failureReason = 0;
			try
			{
				discovery.ConnectTo(opts.IP, out IBMDSwitcher switcher, out failureReason);
				Console.WriteLine("Connected");
				atem = new AtemSwitcher(switcher);
			}
			catch (COMException)
			{
				switch (failureReason)
				{
					case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse:
						Console.WriteLine("No response");
						break;
					case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware:
						Console.WriteLine("Incompatible firmware");
						break;
					default:
						Console.WriteLine("Unable to connect: " + failureReason.ToString());
						break;
				}
				return;
			}

			atem.fetchAudioInputs();
			atem.fetchSwitcherInputs();

			switch (opts.Mode)
            {
				case "monitor":
					AutoResetEvent evt = new AutoResetEvent(false);
					monitorAVlevels(atem);
					evt.WaitOne();
					break;
				default:
					dumpInputsToConsole(atem);
					break;
            }
			
		}

		protected static void dumpInputsToConsole(AtemSwitcher atem)
        {
			Console.WriteLine("Switcher Inputs:");
			Console.WriteLine("[id] name");
			foreach (IBMDSwitcherInput input in atem.GetSwitcherInputs())
			{
				String name;
				input.GetShortName(out name);
				long inputId;
				input.GetInputId(out inputId);
				Console.WriteLine("[" + inputId.ToString() + "] " + name);
			}
			Console.WriteLine("");

			Console.WriteLine("Audio inputs:");
			Console.WriteLine("[input id / source id] port / type");
			foreach (AtemAudioInputSource audioSource in atem.GetAudioInputSources()) {
				Console.WriteLine(string.Format("[{0,6}/{1,6}] {2} / {3} {4} ",
					audioSource.inputId,
					audioSource.sourceId,
					audioSource.getPortType(),
					audioSource.getInputType(),
					audioSource.getSourceType()
					));
			}
		}

		protected static void monitorAVlevels(AtemSwitcher atem)
        {
			atem.startAudioMonitoring();
		}

		static void HandleParseError(IEnumerable<Error> errs)
		{
			//handle errors
		}
	}


}
