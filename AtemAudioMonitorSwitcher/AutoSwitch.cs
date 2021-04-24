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

        double sampleThreshold; // how loud must the level be before we treat it as valid?
        int sampleInterval; // how often should we sample the levels (ms)
        int sampleCount; // how many samples do we want before we switch inputs?

        long previouslySwitchedToInput = -1; // what was the input that we switched too last?
        List<AutoSwitchMonitor> loudestMonitors = new List<AutoSwitchMonitor>(); // what inputs have been the loudest during our sampling?

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
            this.sampleThreshold = opts.sampleThreshold;
            this.sampleInterval = opts.sampleInterval;
            this.sampleCount = opts.sampleCount;
        }

        public void run()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
            setupMonitoringCallbacks();
            atem.startAudioMonitoring();

            // loop through inputs, checking volume levels. 
            while (true)
            {
                // determine loudest
                AutoSwitchMonitor loudestMonitor = null;
                foreach (AutoSwitchMonitor monitor in this.monitors)
                {
                    if (monitor.audioLevels != double.NegativeInfinity)
                    {
                        Console.CursorTop = monitor.lineIndex + 1;
                        Console.CursorLeft = 0;
                        Console.WriteLine();
                        Console.CursorTop = monitor.lineIndex + 1;
                        monitor.drawVUMeter(monitor.audioLevels);
                    }
                    if (monitor.audioLevels < sampleThreshold)
                    {
                        continue;
                    }
                    if (loudestMonitor == null || monitor.audioLevels.CompareTo(loudestMonitor.audioLevels) > 0)
                    {
                        loudestMonitor = monitor;
                    }
                }
                if (loudestMonitor != null) {
                    // add to sample array
                    loudestMonitors.Add(loudestMonitor);
                    if (loudestMonitors.Count > sampleCount)
                    {
                        loudestMonitors.RemoveAt(0);
                    }
                }

                bool isContiguous = false;
                // look at samples
                if (loudestMonitors.Count == sampleCount)
                {
                    // are all the inputs the same?
                    String loudestContiguous = null;
                    isContiguous = true;
                    foreach (AutoSwitchMonitor monitor in this.loudestMonitors)
                    {
                        if (loudestContiguous == null)
                        {
                            loudestContiguous = monitor.label;
                        }
                        if (loudestContiguous.CompareTo(monitor.label) != 0)
                        {
                            isContiguous = false;
                            break;
                        }
                    }
                }
                Console.SetCursorPosition(0, (this.inputSwitchMapping.Count * 2) + 3);
                if (isContiguous)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                }
                foreach (AutoSwitchMonitor monitor in this.loudestMonitors)
                {
                    Console.Write(monitor.label + " ");
                }
                Console.SetCursorPosition(0, (this.inputSwitchMapping.Count * 2) + 4);
                Console.ResetColor();

                if (loudestMonitors.Count == sampleCount && isContiguous)
                {
                    if (loudestMonitors.First().switcherInputId != previouslySwitchedToInput)
                    {
                        if (previouslySwitchedToInput == -1)
                        {
                            atem.transitionTo(loudestMonitors.First().switcherInputId, 0);
                        }
                        else
                        {
                            atem.transitionFromTo(previouslySwitchedToInput, loudestMonitors.First().switcherInputId, 0);
                        }
                        previouslySwitchedToInput = loudestMonitors.First().switcherInputId;
                    }
                }

                Thread.Sleep(sampleInterval);
            }
        }
        protected void setupMonitoringCallbacks()
        {
            foreach (AtemAudioInputSource inputSource in atem.GetAudioInputSources())
            {
                inputSource.m_audioSource.RemoveCallback(inputSource);
            }
            int count = 0;
            foreach (Tuple<long, long, long> mapping in this.inputSwitchMapping)
            {
                Boolean found = false;
                // add our own callback to all the inputs
                foreach (AtemAudioInputSource inputSource in atem.GetAudioInputSources())
                {
                    if (mapping.Item1 == inputSource.inputId &&
                        mapping.Item2 == inputSource.sourceId)
                    {
                        AutoSwitchMonitor switcherMonitor = new AutoSwitchMonitor(inputSource.m_audioInput, inputSource.m_audioSource, mapping.Item3);
                        switcherMonitor.lineIndex = inputSwitchMapping.Count + count;
                        switcherMonitor.label = (1 + count).ToString();
                        inputSource.m_audioSource.AddCallback(switcherMonitor);
                        this.monitors.Add(switcherMonitor);
                        found = true;
                        Console.WriteLine("#" + (1 + count) + " " + switcherMonitor.lineIndex  +" Monitoring input " + mapping.Item1.ToString() + "/" + mapping.Item2.ToString() + " to trigger " + mapping.Item3.ToString());
                        count++;
                    }
                }
                if (!found)
                {
                    throw new Exception("Unable to find inputId/sourceId matching " + mapping.Item1.ToString() + "/" + mapping.Item2.ToString());
                }
            }
        }
    }

    internal class AutoSwitchMonitor : BaseAudioSource 
    {
        public int lineIndex = 0;
        public long switcherInputId; // device to switch to
        public double audioLevels = double.NegativeInfinity;
        public string label;

        long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public AutoSwitchMonitor(IBMDSwitcherFairlightAudioInput audioInput, IBMDSwitcherFairlightAudioSource audioSource, long switcherInputId)
        {
            m_audioInput = audioInput;
            m_audioSource = audioSource;
            this.switcherInputId = switcherInputId;
            this.populateProperties();
        }

        public override void Notify(_BMDSwitcherFairlightAudioSourceEventType eventType)
        {
            // throw new NotImplementedException();
        }

        public override void OutputLevelNotification(uint numLevels, ref double levels, uint numPeakLevels, ref double peakLevels)
        {
            // throw new NotImplementedException();
            audioLevels = levels;

        }
    }
}
