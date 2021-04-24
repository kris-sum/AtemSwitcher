
AtemAudioMonitorSwitcher.exe 192.168.250.81

AtemAudioMonitorSwitcher.exe monitor 192.168.250.81

AtemAudioMonitorSwitcher.exe autoswitch 192.168.250.81 --mappings 1301/-255=1 1301/-256=2 1302/-65280=3

---

https://note.com/taku_min/n/n985beda711f4
Takumin
2020/07/07 00:10


ATEM mini / PRO is Fairlight audio, so the root object for audio control is

`IBMDSwitcherAudioMixer` not, `IBMDSwitcherFairlightAudioMixer`

Audio Input and Audio Source

And while  `IBMDSwitcherAudioMixer` could simply create an AudioInput iterator and then enum it to find the Input, these models with FairlightMixer have an AudioSource iterator for each Input after iterating the AudioInput. You can get objects that you can touch variously as audio channels for the first time by creating and enum. You can see this by actually getting the object in VS2019. There are no good methods or properties attached to AudioInput.


    internal class AtemAudioMixer
    {
            private IBMDSwitcherFairlightAudioMixer m_audiomixer;
            private List<IBMDSwitcherFairlightAudioInput> m_allInputs; // all inputs
            private List<IBMDSwitcherFairlightAudioSource> m_allSources; // all sources (iterated each input)
            public AtemAudioMixer(IBMDSwitcher switcher)
        {
                this.m_audiomixer = (IBMDSwitcherFairlightAudioMixer)switcher;
                Guid audioIteratorIID = typeof(IBMDSwitcherFairlightAudioInputIterator).GUID;
                IntPtr audioIteratorPtr;
                IBMDSwitcherFairlightAudioInputIterator audioIterator = null;
                m_audiomixer.CreateIterator(ref audioIteratorIID, out audioIteratorPtr);
                if (audioIteratorPtr != null)
                {
                    audioIterator = (IBMDSwitcherFairlightAudioInputIterator)Marshal.GetObjectForIUnknown(audioIteratorPtr);
                }

                m_allInputs = new List<IBMDSwitcherFairlightAudioInput>();
                m_allSources = new List<IBMDSwitcherFairlightAudioSource>();
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
                    while(true)
                {
                        IBMDSwitcherFairlightAudioSource audioSource;
                        audioSourceIterator.Next(out audioSource);
                        if (audioSource == null)
                        {
                            break;
                        }
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


Calling code

            IBMDSwitcherDiscovery discovery = new CBMDSwitcherDiscovery();
			IBMDSwitcher switcher;
			_BMDSwitcherConnectToFailure failureReason;
			discovery.ConnectTo("192.168.xx.xx", out switcher, out failureReason);

            // ...

            AtemAudioMixer audioMixer = new AtemAudioMixer(switcher);
			foreach(var audioInput in audioMixer.GetAudioInputs())
            {
				_BMDSwitcherExternalPortType portType;
				audioInput.GetCurrentExternalPortType(out portType);
				Console.WriteLine("AudioPortType: " + portType.ToString());
			}

			foreach(var audioSource in audioMixer.GetAudioSources())
            {
 				_BMDSwitcherFairlightAudioSourceType sourceType;
				audioSource.GetSourceType(out sourceType);
				Console.WriteLine("AudioSourceType: " + sourceType.ToString());

				double faderGain;
				audioSource.GetFaderGain(out faderGain);
                // 例えばフェーダーのゲインを取ってみる。
				Console.WriteLine("FaderGain: " + faderGain.ToString());
			}

Output

AudioPortType: bmdSwitcherExternalPortTypeHDMI
AudioPortType: bmdSwitcherExternalPortTypeHDMI
AudioPortType: bmdSwitcherExternalPortTypeHDMI
AudioPortType: bmdSwitcherExternalPortTypeHDMI
AudioPortType: bmdSwitcherExternalPortTypeTSJack // <-- マイクジャック1
AudioPortType: bmdSwitcherExternalPortTypeTSJack // <-- マイクジャック2
AudioSourceType: bmdSwitcherFairlightAudioSourceTypeStereo // HDMI 1
FaderGain: 0
AudioSourceType: bmdSwitcherFairlightAudioSourceTypeStereo // HDMI 2
FaderGain: 0
AudioSourceType: bmdSwitcherFairlightAudioSourceTypeStereo // HDMI 3
FaderGain: 0
AudioSourceType: bmdSwitcherFairlightAudioSourceTypeStereo // HDMI 4
FaderGain: 0
AudioSourceType: bmdSwitcherFairlightAudioSourceTypeStereo // Mic 1
FaderGain: 0
AudioSourceType: bmdSwitcherFairlightAudioSourceTypeMono // Mic 2 (mono A)
FaderGain: 0
AudioSourceType: bmdSwitcherFairlightAudioSourceTypeMono // Mic 2 (mono B)
FaderGain: 0