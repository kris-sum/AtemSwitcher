using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;

namespace AtemAudioMonitorSwitcher
{
	public class AtemAudioInputSource : BaseAudioSource 
	{
		public int lineIndex = 0;
		long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

		public AtemAudioInputSource(IBMDSwitcherFairlightAudioInput audioInput, IBMDSwitcherFairlightAudioSource audioSource, int index)
		{
			m_audioInput = audioInput;
			m_audioSource = audioSource;
			lineIndex = index;
			this.m_audioInput.GetCurrentExternalPortType(out portType);
			this.m_audioInput.GetId(out inputId);
			this.m_audioInput.GetType(out inputType);
			this.m_audioSource.GetId(out sourceId);
			this.m_audioSource.GetSourceType(out sourceType);
		}

		public override void Notify(_BMDSwitcherFairlightAudioSourceEventType eventType)
		{
			//throw new NotImplementedException();
		}

		public override void OutputLevelNotification(uint numLevels, ref double levels, uint numPeakLevels, ref double peakLevels)
		{
			if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - milliseconds < 80)
			{
				return;
			}
			if (numLevels > 0 && levels != double.NegativeInfinity)
			{
				Console.CursorTop = lineIndex + 2;
				Console.CursorLeft = 0;
				Console.WriteLine();
				Console.CursorTop = lineIndex + 2;

				drawVUMeter(levels);
			}
			milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}
	}
}