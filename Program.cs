using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Diagnostics;
using TwitchLib;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Api.Models.v5.Users;

namespace EmotePrototypev1
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch m_Timer = Stopwatch.StartNew();
            const int TIMER_RESET = 30 ;
            
            Database m_Database = new Database();
            DataTable m_EmoteData = new DataTable();

            ChatBot m_Bot = new ChatBot(m_Database);

            int loop = 0;

            m_Bot.Initialize();
            m_Timer.Start();

            while (loop == 0)
            {
                //If timer hits the time in MS, then update and reset it 
                if (m_Timer.ElapsedMilliseconds >= TIMER_RESET * 1000)
                {
                    m_Bot.UpdatePricing();
                    List<EmoteInfo> recordEmoteData = m_Bot.GetEmoteInfoList();

                    for (int i = 0; i < recordEmoteData.Count; i++)
                    {
                        recordEmoteData[i].ValueUpdate();
                        m_Database.RecordHistory(recordEmoteData[i]);
                    }

                    m_Timer.Reset();
                }
            }

            m_Timer.Stop();

            //Close the connection if open.
            if (m_Database.m_Connection.Ping() == true)
            {
                m_Database.CloseConnection();
            }
        }

    }
}
