using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using BMDSwitcherAPI;

namespace AtemAudioMonitorSwitcher
{
    public class AtemSwitcherInput : IBMDSwitcherInputCallback
    {
        AtemSwitcher parentSwitcher;
        IBMDSwitcherInput m_switcherInput;

        public long inputId;
        public int isPreviewTallied;
        public int isProgramTallied;

        public AtemSwitcherInput(AtemSwitcher switcher, IBMDSwitcherInput switcherInput)
        {
            parentSwitcher = switcher;
            m_switcherInput = switcherInput;
            m_switcherInput.GetInputId(out inputId);
            informPreviewOrProgram();
        }

        public IBMDSwitcherInput GetInput()
        {
            return m_switcherInput;
        }

        public void Notify(_BMDSwitcherInputEventType eventType)
        {
            switch (eventType) {
                case _BMDSwitcherInputEventType.bmdSwitcherInputEventTypeIsPreviewTalliedChanged:
                case _BMDSwitcherInputEventType.bmdSwitcherInputEventTypeIsProgramTalliedChanged:
                    informPreviewOrProgram();
                    break;
            }
        }
        public void informPreviewOrProgram()
        {
            // isPreviewTallied returns 2?
            m_switcherInput.IsPreviewTallied(out isPreviewTallied);
            if (isPreviewTallied >= 1)
            {
                parentSwitcher.setPreviewSwitcherInput(m_switcherInput);
            }
            m_switcherInput.IsProgramTallied(out isProgramTallied);
            if (isProgramTallied >= 1)
            {
                parentSwitcher.setProgramSwitcherInput(m_switcherInput);
            }
        }

    }
}
