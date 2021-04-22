using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;

namespace AtemAudioMonitorSwitcher
{
	public class AtemAudioInputSource : IBMDSwitcherFairlightAudioSourceCallback
	{
		public IBMDSwitcherFairlightAudioInput m_audioInput;
		public _BMDSwitcherFairlightAudioInputType inputType;
		public _BMDSwitcherExternalPortType portType;
		public IBMDSwitcherFairlightAudioSource m_audioSource;
		public _BMDSwitcherFairlightAudioSourceType sourceType;

		public int lineIndex = 0;
		public long inputId;
		public long sourceId;

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

		public String getInputType()
        {
			return inputType.ToString().Substring(34);
        }

		public String getPortType()
		{
			return portType.ToString().Substring(27);
        }

		public String getSourceType()
		{
			return sourceType.ToString().Substring(35);
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
				Console.CursorTop = lineIndex + 2;
				Console.CursorLeft = 0;

				// draw -50 to 0 db - lets use 25 characters for this, so each character is 2db.
				int characters = 50 + (int)Math.Round(levels);
				if (characters < 0) characters = 0;
				characters = (int)Math.Floor(characters / 2f);

				Console.Write(string.Format("[{0,6}/{1,6}] ", inputId, sourceId));

				m_audioSource.IsMixedIn(out int mixedIn);
				if (mixedIn == 0)
                {
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("off ");
				} else
                {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.Write("REC ");
				}

				Console.ForegroundColor = ConsoleColor.Green;
				for (int y=0; y<25; y++)
                {
					if (y == 19)
                    {
						Console.ForegroundColor = ConsoleColor.Yellow;
                    }
					if (y == 22)
					{
						Console.ForegroundColor = ConsoleColor.Red;
					}
					if (y <= characters)
                    {
						Console.Write("=");
                    } else
                    {
						Console.Write(" ");
                    }
				}
				Console.ForegroundColor = ConsoleColor.White;

				Console.Write(string.Format("{0,5:00.00} dB {1} {2} {3}", levels,
					getPortType(),
					getInputType(),
					getSourceType()));
			}
			milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}
	}
}