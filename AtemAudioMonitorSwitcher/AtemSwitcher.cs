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

		private List<IBMDSwitcherInput> m_switcherInputs;
		private List<IBMDSwitcherFairlightAudioInput> m_allInputs; // all audio inputs
		private List<IBMDSwitcherFairlightAudioSource> m_allSources; // all audio sources (iterated each input)
		private List<AtemAudioInputSource> m_audioInputSources; // our own object, combination of the above two inputs

		public AtemSwitcher(IBMDSwitcher switcher) => this.switcher = switcher;

		public List<IBMDSwitcherFairlightAudioInput> GetAudioInputs()
		{
			return m_allInputs;
		}

		public List<IBMDSwitcherFairlightAudioSource> GetAudioSources()
		{
			return m_allSources;
		}

		public List<IBMDSwitcherInput> GetSwitcherInputs()
		{
			return m_switcherInputs;
		}
		public List<AtemAudioInputSource> GetAudioInputSources()
		{
			return m_audioInputSources;
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
			m_switcherInputs = SwitcherInputs.Where((i, ret) =>
			{
				i.GetPortType(out _BMDSwitcherPortType type);
				return type == _BMDSwitcherPortType.bmdSwitcherPortTypeExternal;
			}).ToList();
		}

		public IEnumerable<IBMDSwitcherInput> SwitcherInputs
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
	}
}