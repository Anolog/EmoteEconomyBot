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
        private float m_OldAverage;

        public const float AMOUNT_OF_TIME_SECONDS = 30;
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

        public int GetID()
        {
            return m_ID;
        }

        public float GetValue()
        {
            return m_CurrentValue;
        }

        public float GetLowestValue()
        {
            return m_LowestValue;
        }

        public float GetHighestValue()
        {
            return m_HighestValue;
        }

        public float GetAverage()
        {
            return m_Average;
        }
        
        public float GetAmountBought()
        {
            return m_AmountBought;
        }

        public float GetAmountSold()
        {
            return m_AmountSold;
        }

        public void SetAmountOfEmotesSaid(int aAmount)
        {
            m_AmountOfEmotesSaid = aAmount;
        }

        public int GetAmountOFEmotesSaid()
        {
            return m_AmountOfEmotesSaid;
        }

        //TODO UPDATE STUFF FOR TAKING OVER HIGHEST VALUE, LOWEST VALUE, ETC....

        //Need to call this after you call the record values function
        public void ValueUpdate()
        {
            Console.WriteLine(m_EmoteName + " - Emote value being updated.");

            //Change highest and lowest val

            if (m_CurrentValue > m_HighestValue)
            {
                m_HighestValue = m_CurrentValue;
            }

            if (m_CurrentValue < m_LowestValue)
            {
                m_LowestValue = m_CurrentValue;
            }

            if (m_CurrentValue <= 0.01f)
            {
                m_CurrentValue = 0.01f;
            }

            m_OldAverage = m_Average;

            CalculateAverage();

            if (m_Average == 0)
            {
                if (m_CurrentValue <= 0.01f)
                {
                    ValLessZero();
                    return;
                }

                if (m_OldAverage != 0)
                {
                    m_CurrentValue -= (m_OldAverage / m_CurrentValue);
                    ValLessZero();
                    return;
                }

                m_CurrentValue -= 0.01f;
                ValLessZero();
                return;
            }

            float valChange = (m_OldAverage / m_Average) / m_CurrentValue;

            //If it's less than the average take away
            if (m_OldAverage > m_Average && m_OldAverage != 0)
            {
                valChange = m_OldAverage - m_Average;
                valChange *= -1;
            }

            m_CurrentValue += valChange;
            ValLessZero();

            return;
        }

        public void ValLessZero()
        {
            if (m_CurrentValue <= 0)
            {
                m_CurrentValue = 0.01f;
            }
        }
    }


}
