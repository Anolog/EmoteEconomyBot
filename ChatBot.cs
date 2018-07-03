using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Client;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Api.Models.v5.Users;

namespace EmotePrototypev1
{
    internal class ChatBot
    {
        readonly ConnectionCredentials m_Credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);

        List<TwitchClient> m_ClientList = new List<TwitchClient>();

        Database m_Database;

        public ChatBot(Database aDatabase)
        {
            m_Database = aDatabase;
        }

        public void Initialize()
        {
            //Initializing the clients
            List<string> channelNames = m_Database.SelectChannelsToBeIn();

            foreach (string name in channelNames)
            {
                TwitchClient twitchClient = CreateClient(name);

                m_ClientList.Add(twitchClient);
            }

        }

        private TwitchClient CreateClient(string aChannelName)
        {
            //Create a client from the array we get from the DB
            TwitchClient tc = new TwitchClient();
            tc.Initialize(m_Credentials, aChannelName);
            tc.ChatThrottler = new TwitchLib.Client.Services.MessageThrottler(tc, 20, TimeSpan.FromSeconds(30));
            tc.ChatThrottler.ApplyThrottlingToRawMessages = true;
            tc.ChatThrottler.StartQueue();

            //Setting listening events
            tc.OnLog += Client_OnLog;
            tc.OnConnected += Client_OnConnect;
            tc.OnConnectionError += Client_OnConnectionError;
            tc.OnMessageReceived += Client_OnMessageRecieved;
            tc.OnDisconnected += Client_OnDisconnect;
            tc.OnWhisperReceived += Client_OnWhisperRecieved;
            tc.Connect();

            return tc;
        }

        private void Client_OnWhisperRecieved(object sender, OnWhisperReceivedArgs e)
        {
            return;
        }

        private void Client_OnDisconnect(object sender, OnDisconnectedArgs e)
        {
            return;
        }

        private void Client_OnMessageRecieved(object sender, OnMessageReceivedArgs e)
        {
            m_ClientList[0].SendMessage("monkascountbot", m_ClientList[0].TwitchUsername);


            //If the client that the message recieved is the main bot
            if (e.ChatMessage.Channel == "monkascountbot")
            {
                if (e.ChatMessage.Message == "!Enable")
                {
                    //Check if they are in the DB or not
                    Console.WriteLine("Checking if in DB");
                    bool inDB = m_Database.CheckForUserInDB(e.ChatMessage.Username, "userinfo");

                    //If they are in the db, grab their ID and put them into the chat db
                    if (inDB == true)
                    {
                        Console.WriteLine(e.ChatMessage.Username + " is in the DB userinfo");
                        //Check if in the other DB
                        bool inOtherDB = m_Database.CheckForUserInDB(e.ChatMessage.Username, "chatinfo");

                        //in both databases
                        if (inOtherDB == true)
                        {
                            Console.WriteLine(e.ChatMessage.Username + " is in the DB chatinfo");
                            Console.WriteLine(e.ChatMessage.Username + " is in both of the DB's");

                            //THE BOT SHOULD ALWAYS BE 0
                            m_ClientList[0].SendMessage("monkasbotcount", e.ChatMessage.Username + " is already active!");

                            return;
                        }

                        //not in the chatinfo db, put them into it
                        if (inOtherDB == false)
                        {
                            Console.WriteLine(e.ChatMessage.Username + " is not in the DB chatinfo");
                            int id = m_Database.GetUserID(e.ChatMessage.Username);

                            m_Database.InsertUserToChatDB(e.ChatMessage.Username, id);

                            m_ClientList[0].SendMessage("monkasbotcount", e.ChatMessage.Username + " is now active!");

                        }
                    }

                    //if they are not in the db, put them into both
                    if (inDB == false)
                    {
                        Console.WriteLine(e.ChatMessage.Username + " is not in the DB userinfo");

                        //Create user in one db
                        m_Database.InsertUserToDB(e.ChatMessage.Username);

                        //Grab the id of them
                        int id = m_Database.GetUserID(e.ChatMessage.Username);

                        //put user into other db
                        m_Database.InsertUserToChatDB(e.ChatMessage.Username, id);

                    }

                    //Add it to the current client list
                    m_ClientList.Add(CreateClient(e.ChatMessage.Username));

                    return;
                }

                if (e.ChatMessage.Message == "!EconomyRegister")
                {
                    m_Database.InsertUserToDB(e.ChatMessage.Username);

                    return;
                }
            }

            //Every other channel
            else
            {
                if (e.ChatMessage.Message == "!EconomyRegister")
                {
                    //Need to do some checks if they are already in the DB
                    //Prbably do it in the database side of things
                    m_Database.InsertUserToDB(e.ChatMessage.Username);
                }
            }

            return;
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            throw new NotImplementedException();
        }

        private void Client_OnConnect(object sender, OnConnectedArgs e)
        {
            return;
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            //Implement something here
            return;
        }
    }
}
