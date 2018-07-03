using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
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
            Database m_Database = new Database();
            DataTable m_EmoteData = new DataTable();

            ChatBot m_Bot = new ChatBot(m_Database);

            int loop = 0;

            //Get rid of most of the things below this

            m_Bot.Initialize();

            while (loop == 0)
            {
                Console.ReadLine();
                loop = 1;
            }

            //Close the connection if open.
            if (m_Database.m_Connection.Ping() == true)
            {
                m_Database.CloseConnection();
            }
        }
    }
}
