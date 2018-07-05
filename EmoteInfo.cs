using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmotePrototypev1
{
    class EmoteInfo
    {
        //Info from/to database
        private int m_ID;
        private string m_EmoteName;
        private float m_CurrentValue;
        private float m_LowestValue;
        private float m_HighestValue;
        private float m_AmountBought;
        private float m_AmountSold;
        private float m_Average;

        //public const float AMOUNT_OF_TIME_SECONDS = 60.0f;
        //This is the amount of emotes said between the the AMOUNT_OF_TIME_SECONDS
        private int m_AmountOfEmotesSaid;

        public EmoteInfo(int aID, string aName, float aCurrentVal, float aLowestVal, float aHighestVal, float aBought, float aSold, float aAverage)
        {
            m_ID = aID;
            m_EmoteName = aName;
            m_CurrentValue = aCurrentVal;
            m_LowestValue = aLowestVal;
            m_HighestValue = aHighestVal;
            m_AmountBought = aBought;
            m_AmountSold = aSold;
            m_Average = aAverage;
        }

        public void CalculateAverage()
        {
            m_Average = (float)m_AmountOfEmotesSaid / AMOUNT_OF_TIME_SECONDS;
        }

        public string GetName()
        {
            return m_EmoteName;
        }

        public void SetAmountOfEmotesSaid(int aAmount)
        {
            m_AmountOfEmotesSaid = aAmount;
        }

        public int GetAmountOFEmotesSaid()
        {
            return m_AmountOfEmotesSaid;
        }

        //currentvalue = ((amountsaid / average) * currentvalue ) + currentvalue
    }
}
