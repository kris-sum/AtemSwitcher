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
				int characters = 50 + (int)Math.Round(levels);
				if (characters < 0) characters = 0;

				characters = (int)Math.Floor(characters / 2f);

				Console.Write(string.Format("[{0,6}/{1,6}] ", inputId, sourceId));

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
				/*
				Console.Write(string.Concat(Enumerable.Repeat('=', characters)));
				if (characters < 25)
				{
					Console.Write(string.Concat(Enumerable.Repeat(' ', 25 - characters)));
				}*/
				Console.Write(string.Format("{0,5:00.00} dB {1} {2}", levels, inputType.ToString().Substring(34), sourceType.ToString().Substring(35)));
			}
			milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}
	}
}