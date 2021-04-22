using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.Linq;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;
using System.Threading;

namespace AtemAudioMonitorSwitcher
{
    class AutoSwitch
    {
        AtemSwitcher atem;

        // AtemAudioSource.inputId, AtemAudioSource.sourceId, AtemSwitcherInput.inputId
        List<Tuple<long, long, long>> inputSwitchMapping = new List<Tuple<long, long, long>>();

        List<AutoSwitchMonitor> monitors = new List<AutoSwitchMonitor>();

        public AutoSwitch(AtemSwitcher switcher)
        {
            atem = switcher;
        }

        public void setOptions(Program.AutoSwitchVerbOption opts)
        {
            foreach (String mapping in opts.Mappings)
            {
                String[] io = mapping.Split('=');
                if (io.Length != 2)
                {
                    throw new Exception("Invalid mapping (splitting on = sign for) " + mapping + " didnt result in 2 sides");
                }
                String[] input = io.First().Split('/');
                if (input.Length != 2)
                {
                    throw new Exception("Invalid mapping (splitting on / sign for) " + io.First() + " didnt result in 2 ids");
                }
                this.inputSwitchMapping.Add(new Tuple<long, long, long>(
                    (long)Convert.ToDouble(input.First()),
                    (long)Convert.ToDouble(input.Last()),
                    (long)Convert.ToDouble(io.Last())
                    ));
            }
        }

        public void run()
        {
            foreach (AtemAudioInputSource inputSource in atem.GetAudioInputSources())
            {
                inputSource.m_audioSource.RemoveCallback(inputSource);
            }
            foreach (Tuple<long, long, long> mapping in this.inputSwitchMapping) {
                Boolean found = false;
                // add our own callback to all the inputs
                foreach (AtemAudioInputSource inputSource in atem.GetAudioInputSources())
                {
                    if (mapping.Item1 == inputSource.inputId &&
                        mapping.Item2 == inputSource.sourceId)
                    {
                        AutoSwitchMonitor switcherMonitor = new AutoSwitchMonitor(inputSource.m_audioInput, inputSource.m_audioSource);
                        inputSource.m_audioSource.AddCallback(switcherMonitor);
                        this.monitors.Add(switcherMonitor);
                        found = true;
                        Console.WriteLine("+ Monitoring input " + mapping.Item1.ToString() + "/" + mapping.Item2.ToString() +" to trigger " + mapping.Item3.ToString());
                    }
                }
                if (!found)
                {
                    throw new Exception("Unable to find inputId/sourceId matching " + mapping.Item1.ToString() + "/" + mapping.Item2.ToString());
                }
            }

            atem.startAudioMonitoring();

            // loop through inputs, checking volume levels. 
            while (true)
            {
                foreach (AutoSwitchMonitor monitor in this.monitors)
                {
                    Console.Write(monitor.audioLevels.ToString() + " ");
                }
                Console.WriteLine();
                Thread.Sleep(200);
            }
        }
    }

    internal class AutoSwitchMonitor : IBMDSwitcherFairlightAudioSourceCallback
    {
        public IBMDSwitcherFairlightAudioInput m_audioInput;
        public IBMDSwitcherFairlightAudioSource m_audioSource;

        public int lineIndex = 0;
        public long inputId;
        public long sourceId;

        public double audioLevels;

        long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public AutoSwitchMonitor(IBMDSwitcherFairlightAudioInput audioInput, IBMDSwitcherFairlightAudioSource audioSource)
        {
            m_audioInput = audioInput;
            m_audioSource = audioSource;
            this.m_audioInput.GetId(out inputId);
            this.m_audioSource.GetId(out sourceId);
        }

        public void Notify(_BMDSwitcherFairlightAudioSourceEventType eventType)
        {
            // throw new NotImplementedException();
        }

        public void OutputLevelNotification(uint numLevels, ref double levels, uint numPeakLevels, ref double peakLevels)
        {
            // throw new NotImplementedException();
            audioLevels = levels;
        }
    }
}
