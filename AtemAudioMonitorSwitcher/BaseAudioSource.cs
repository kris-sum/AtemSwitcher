using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;

namespace AtemAudioMonitorSwitcher
{
	public class BaseAudioSource : IBMDSwitcherFairlightAudioSourceCallback
	{
		public IBMDSwitcherFairlightAudioInput m_audioInput;
		public _BMDSwitcherFairlightAudioInputType inputType;
		public _BMDSwitcherExternalPortType portType;
		public IBMDSwitcherFairlightAudioSource m_audioSource;
		public _BMDSwitcherFairlightAudioSourceType sourceType;
		public long inputId;
		public long sourceId;
		public double pan;

		protected void populateProperties() { 
			this.m_audioInput.GetId(out inputId);
            this.m_audioInput.GetCurrentExternalPortType(out portType);
            this.m_audioInput.GetType(out inputType);
            this.m_audioSource.GetId(out sourceId);
            this.m_audioSource.GetSourceType(out sourceType);
            this.m_audioSource.GetPan(out pan);
		}

		public virtual void Notify(_BMDSwitcherFairlightAudioSourceEventType eventType)
        {
            throw new NotImplementedException();
        }

        public virtual void OutputLevelNotification(uint numLevels, ref double levels, uint numPeakLevels, ref double peakLevels)
        {
            throw new NotImplementedException();
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

		public void drawVUMeter(double levels)
        {
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
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("REC ");
			}

			Console.ForegroundColor = ConsoleColor.Green;
			for (int y = 0; y < 25; y++)
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
				}
				else
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
    }
}
