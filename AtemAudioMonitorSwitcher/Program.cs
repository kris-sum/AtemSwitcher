using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;
using System.Threading;

namespace AtemAudioMonitorSwitcher
{
    class Program
    {
        static void Main(string[] args)
        {
            IBMDSwitcherDiscovery discovery = new CBMDSwitcherDiscovery();

			AtemSwitcher atem;
			// Connect to switcher
			_BMDSwitcherConnectToFailure failureReason = 0;
			try
			{
				discovery.ConnectTo("192.168.250.81", out IBMDSwitcher switcher, out failureReason);
				Console.WriteLine("Connected to ATEM switcher");
				atem = new AtemSwitcher(switcher);
			} catch (COMException) {
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
				Console.Write("Press ENTER to exit...");
				Console.ReadLine();
				return;
			}
			AutoResetEvent evt = new AutoResetEvent(false);
			Console.WriteLine("Found Fairlight audio mixer");
			Console.Write("Entering monitor mode - CTRL-C to exit");
			atem.fetchAudioInputs();
			evt.WaitOne();
            Console.ReadLine();
        }
    }



	internal class AtemSwitcher 
	{
		private IBMDSwitcher switcher;

		private IBMDSwitcherFairlightAudioMixer audioMixer;
		public AtemSwitcher(IBMDSwitcher switcher) => this.switcher = switcher;

		private List<IBMDSwitcherFairlightAudioInput> m_allInputs; // all inputs
		private List<IBMDSwitcherFairlightAudioSource> m_allSources; // all sources (iterated each input)
		public void fetchAudioInputs()
		{
			audioMixer = (IBMDSwitcherFairlightAudioMixer)switcher;
			audioMixer.SetAllLevelNotificationsEnabled(1);

			Guid audioIteratorIID = typeof(IBMDSwitcherFairlightAudioInputIterator).GUID;
			IntPtr audioIteratorPtr;
			IBMDSwitcherFairlightAudioInputIterator audioIterator = null;
			audioMixer.CreateIterator(ref audioIteratorIID, out audioIteratorPtr);
			if (audioIteratorPtr != null)
			{
				audioIterator = (IBMDSwitcherFairlightAudioInputIterator)Marshal.GetObjectForIUnknown(audioIteratorPtr);
			}

			m_allInputs = new List<IBMDSwitcherFairlightAudioInput>();
			m_allSources = new List<IBMDSwitcherFairlightAudioSource>();

			int sourceCount = 0;

			while (true)
			{
				IBMDSwitcherFairlightAudioInput audioInput;
				audioIterator.Next(out audioInput);
				if (audioInput == null)
				{
					break;
				}
				m_allInputs.Add(audioInput);
				

				// added all sources 
				Guid audioSourceIteratorIID = typeof(IBMDSwitcherFairlightAudioSourceIterator).GUID;
				IntPtr audioSourceIteratorPtr;
				IBMDSwitcherFairlightAudioSourceIterator audioSourceIterator = null;
				audioInput.CreateIterator(ref audioSourceIteratorIID, out audioSourceIteratorPtr);
				if (audioSourceIteratorPtr != null)
				{
					audioSourceIterator = (IBMDSwitcherFairlightAudioSourceIterator)Marshal.GetObjectForIUnknown(audioSourceIteratorPtr);
				}
				while (true)
				{
					IBMDSwitcherFairlightAudioSource audioSource;
					audioSourceIterator.Next(out audioSource);
					if (audioSource == null)
					{
						break;
					}
					audioSource.AddCallback(new AtemAudioSource(audioInput, audioSource, sourceCount)); // add callback to monitor audio level
					sourceCount++;
					m_allSources.Add(audioSource);
				}
			}

		}
		public List<IBMDSwitcherFairlightAudioInput> GetAudioInputs()
		{
			return m_allInputs;
		}

		public List<IBMDSwitcherFairlightAudioSource> GetAudioSources()
		{
			return m_allSources;
		}
    }

	internal class AtemAudioSource : IBMDSwitcherFairlightAudioSourceCallback
	{
		IBMDSwitcherFairlightAudioInput m_audioInput;
		IBMDSwitcherFairlightAudioSource m_audioSource;
		int lineIndex = 0;
		long inputId;
		_BMDSwitcherFairlightAudioInputType inputType;
		long sourceId;
		_BMDSwitcherFairlightAudioSourceType sourceType;
		long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

		public AtemAudioSource(IBMDSwitcherFairlightAudioInput audioInput, IBMDSwitcherFairlightAudioSource audioSource, int index)
        {
			m_audioInput = audioInput;
			m_audioSource = audioSource;
			lineIndex = index;
			this.m_audioInput.GetId(out inputId);
			this.m_audioInput.GetType(out inputType);
			this.m_audioSource.GetId(out sourceId);
			this.m_audioSource.GetSourceType(out sourceType);
		}

		public void Notify(_BMDSwitcherFairlightAudioSourceEventType eventType)
		{
			//throw new NotImplementedException();
		}

		public void OutputLevelNotification(uint numLevels, ref double levels, uint numPeakLevels, ref double peakLevels)
		{
			if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - milliseconds < 80)
            {
				return;
            }
			if (numLevels > 0 && levels != double.NegativeInfinity)
			{
				//Console.ForegroundColor = BorderColor;
				Console.CursorTop = lineIndex + 4;
				Console.CursorLeft = 0;
				//Console.Write(s);
				//Console.ResetColor();

				//
				// draw -50 to 0 - lets use 25 characters for this, so each character is 2db.
				//
				int characters = 50 + (int) Math.Round(levels);
				if (characters < 0) characters = 0;

				characters = (int)Math.Floor(characters / 2f);

				Console.Write(string.Concat(Enumerable.Repeat('=', characters))); 
				if (characters < 25) { 
					Console.Write(string.Concat(Enumerable.Repeat(' ', 25 - characters)));
				}

				Console.Write(string.Format(" | {0,5:00.00} dB | input {1}({2})/{3}({4})", levels, inputType.ToString().Substring(34), inputId, sourceType.ToString().Substring(35), sourceId));
			}
			milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			//throw new NotImplementedException();
		}

	}
}
