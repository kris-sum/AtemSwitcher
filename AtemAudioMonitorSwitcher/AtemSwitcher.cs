using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;

namespace AtemAudioMonitorSwitcher
{
	public class AtemSwitcher
	{
		private IBMDSwitcher switcher;
		private IBMDSwitcherFairlightAudioMixer audioMixer;

		private List<AtemSwitcherInput> m_switcherInputs; // our own object,
		private List<IBMDSwitcherFairlightAudioInput> m_allInputs; // all audio inputs
		private List<IBMDSwitcherFairlightAudioSource> m_allSources; // all audio sources (iterated each input)
		private List<AtemAudioInputSource> m_audioInputSources; // our own object, combination of the above two inputs

		private IBMDSwitcherInput previewSwitcherInput;
		private IBMDSwitcherInput programSwitcherInput;

		public static AtemSwitcher Connect(String ipAddress)
		{
			Console.WriteLine("Connecting to "+ipAddress+" ...");
			IBMDSwitcherDiscovery discovery = new CBMDSwitcherDiscovery();
			_BMDSwitcherConnectToFailure failureReason = 0;
			try
			{
				discovery.ConnectTo(ipAddress, out IBMDSwitcher switcher, out failureReason);
				Console.WriteLine("Connected");
				AtemSwitcher atem = new AtemSwitcher(switcher);
				atem.fetchAudioInputs();
				atem.fetchSwitcherInputs();
				return atem;
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
				return null;
			}
		}

		public AtemSwitcher(IBMDSwitcher switcher) => this.switcher = switcher;

		public List<IBMDSwitcherFairlightAudioInput> GetAudioInputs()
		{
			return m_allInputs;
		}
		public List<IBMDSwitcherFairlightAudioSource> GetAudioSources()
		{
			return m_allSources;
		}
		public List<AtemSwitcherInput> GetSwitcherInputs()
		{
			return m_switcherInputs;
		}
		public List<AtemAudioInputSource> GetAudioInputSources()
		{
			return m_audioInputSources;
		}
		public void setPreviewSwitcherInput(IBMDSwitcherInput input) {
			previewSwitcherInput = input;
		}
		public void setProgramSwitcherInput(IBMDSwitcherInput input)
		{
			programSwitcherInput = input;
		}

		public void fetchAudioInputs()
		{
			audioMixer = (IBMDSwitcherFairlightAudioMixer)switcher;

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
			m_audioInputSources = new List<AtemAudioInputSource>();

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
					AtemAudioInputSource audioInputSource = new AtemAudioInputSource(audioInput, audioSource, sourceCount);
					m_audioInputSources.Add(audioInputSource);
					// add callback to monitor audio level
					audioSource.AddCallback(audioInputSource);
					sourceCount++;
					m_allSources.Add(audioSource);
				}
			}
		}

		public void startAudioMonitoring()
        {
			audioMixer = (IBMDSwitcherFairlightAudioMixer)switcher;
			audioMixer.SetAllLevelNotificationsEnabled(1);
		}

		public void stopAudioMonitoring()
        {
			audioMixer = (IBMDSwitcherFairlightAudioMixer)switcher;
			audioMixer.SetAllLevelNotificationsEnabled(0);
		}

		public void fetchSwitcherInputs()
		{
			m_switcherInputs = new List<AtemSwitcherInput>();

			foreach(IBMDSwitcherInput i in SwitcherInputs.Where((i, ret) => {
				i.GetPortType(out _BMDSwitcherPortType type);
				return type == _BMDSwitcherPortType.bmdSwitcherPortTypeExternal;
			}).ToList())
            {
				AtemSwitcherInput switcherInput = new AtemSwitcherInput(this, i);
				i.AddCallback(switcherInput);
				m_switcherInputs.Add(switcherInput);
			};
		}

		public void transitionTo(long switcherInputId, int duration = 200)
		{
			this.programSwitcherInput.GetInputId(out long currentProgramId);
			transitionFromTo(currentProgramId, switcherInputId, duration);
		}

		public void transitionFromTo(long prevSwitcherInputId, long switcherInputId, int duration = 200)
        {
			Console.WriteLine("Transitioning to " + switcherInputId +" from " + prevSwitcherInputId);
			// Get reference to various objects
			IBMDSwitcherMixEffectBlock me0 = this.MixEffectBlocks.First();
			IBMDSwitcherTransitionParameters me0TransitionParams = me0 as IBMDSwitcherTransitionParameters;
			
			//this.programSwitcherInput.GetInputId(out long currentProgramId);
			me0.SetPreviewInput(prevSwitcherInputId);
			me0.SetProgramInput(switcherInputId);
			/*
			me0TransitionParams.SetNextTransitionSelection(_BMDSwitcherTransitionSelection.bmdSwitcherTransitionSelectionBackground);
			me0TransitionParams.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleMix);
			*/
			//me0.PerformAutoTransition();
			me0.PerformCut();
		}

		protected IEnumerable<IBMDSwitcherInput> SwitcherInputs
		{
			get
			{
				// Create an input iterator
				switcher.CreateIterator(typeof(IBMDSwitcherInputIterator).GUID, out IntPtr inputIteratorPtr);
				IBMDSwitcherInputIterator inputIterator = Marshal.GetObjectForIUnknown(inputIteratorPtr) as IBMDSwitcherInputIterator;
				if (inputIterator == null)
					yield break;
				// Scan through all inputs
				while (true)
				{
					inputIterator.Next(out IBMDSwitcherInput input);
					if (input != null)
						yield return input;
					else
						yield break;
				}
			}
		}

		protected IEnumerable<IBMDSwitcherMixEffectBlock> MixEffectBlocks
		{
			get
			{
				// Create a mix effect block iterator
				switcher.CreateIterator(typeof(IBMDSwitcherMixEffectBlockIterator).GUID, out IntPtr meIteratorPtr);
				IBMDSwitcherMixEffectBlockIterator meIterator = Marshal.GetObjectForIUnknown(meIteratorPtr) as IBMDSwitcherMixEffectBlockIterator;
				if (meIterator == null)
					yield break;

				// Iterate through all mix effect blocks
				while (true)
				{
					meIterator.Next(out IBMDSwitcherMixEffectBlock me);

					if (me != null)
						yield return me;
					else
						yield break;
				}
			}
		}

	}
}