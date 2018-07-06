﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
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
        public Stopwatch m_Timer = Stopwatch.StartNew();

        readonly ConnectionCredentials m_Credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        string m_BotChannel = "monkascountbot";

        //Used for emote tracking
        public const float AMOUNT_OF_TIME_SECONDS = 60.0f;

        //Index with the string being the username for the twitch client.
        Dictionary<string, TwitchClient> m_ClientList = new Dictionary<string, TwitchClient>();

        //Index is emote name
        Dictionary<string, EmoteInfo> m_EmoteInfo = new Dictionary<string, EmoteInfo>();
        //Get a list of keys
        List<string> m_EmoteNamesInDB = new List<string>();

        Database m_Database;


        public ChatBot(Database aDatabase)
        {
            m_Database = aDatabase;

            //Probably should actually use the tuple instead of calling this twice...
            m_EmoteNamesInDB = m_Database.GetEmoteInfo().Item1;
            m_EmoteInfo = m_Database.GetEmoteInfo().Item2;
        }

        public void Initialize()
        {
            //Initializing the clients
            List<string> channelNames = m_Database.SelectChannelsToBeIn();

            foreach (string name in channelNames)
            {
                TwitchClient twitchClient = CreateClient(name);

                m_ClientList.Add(name, twitchClient);
            }

        }

        public Dictionary<string, EmoteInfo> GetEmoteInfo()
        {
            return m_EmoteInfo;
        }

        public List<EmoteInfo> GetEmoteInfoList()
        {
            List<EmoteInfo> returnVal = new List<EmoteInfo>();

            for (int i = 0; i < m_EmoteInfo.Count; i++)
            {
                returnVal.Add((m_EmoteInfo[m_EmoteNamesInDB[i]]));
            }

            return returnVal;
        }

        public void UpdatePricing()
        {
            Console.WriteLine("Updating price of emotes");
            return;
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
            //m_ClientList[0].SendMessage("monkascountbot", m_ClientList[0].TwitchUsername);

            string[] chatMessage = e.ChatMessage.Message.Split(' ', '\t');

            //If the client that the message recieved is the main bot
            if (e.ChatMessage.Channel == m_BotChannel)
            {
                if (e.ChatMessage.Message.StartsWith("!Enable"))
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

                            m_ClientList[e.ChatMessage.Channel].SendMessage(m_BotChannel, e.ChatMessage.Username + " is already active!");

                            return;
                        }

                        //not in the chatinfo db, put them into it
                        if (inOtherDB == false)
                        {
                            Console.WriteLine(e.ChatMessage.Username + " is not in the DB chatinfo");
                            int id = m_Database.GetUserID(e.ChatMessage.Username);

                            m_Database.InsertUserToChatDB(e.ChatMessage.Username, id);

                            m_ClientList[e.ChatMessage.Channel].SendMessage(m_BotChannel, e.ChatMessage.Username + " is now active!");

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

                        m_ClientList[e.ChatMessage.Channel].SendMessage(m_BotChannel, e.ChatMessage.Username + " is now registered and enabled!");

                    }

                    //Add it to the current client list
                    //Use the username that the message is recieved from and create a client with the username as well
                    m_ClientList.Add(e.ChatMessage.Username, CreateClient(e.ChatMessage.Username));

                    return;
                }

                if (e.ChatMessage.Message.StartsWith("!EconomyRegister"))
                {
                    RegisterCommand(e);
                }
            }

            //Every other channel
            else
            {
                if (e.ChatMessage.Message.StartsWith("!EconomyRegister"))
                {
                    RegisterCommand(e);
                }

                //CHAT MESSAGE FOR TRADING
                // 0             1      2   3      4
                // !EconomyTrade Emote1 for Emote2 amount
                //TODO:
                //FIX THIS TO MAKE SURE THAT IT HAS TO BE X LENGTH MINIMUM AND THAT THE WORD IS ACTULLY THE CORRECT WORD
                if (e.ChatMessage.Message.StartsWith("!EconomyTrade") && chatMessage.Length >= 5)
                {
                    bool transaciton = m_Database.TradeEmote(e.ChatMessage.Username, chatMessage[1], chatMessage[3], Convert.ToInt32(chatMessage[4]));

                    //Determine if fail or sucess
                    if (transaciton == true)
                    {
                        m_ClientList[e.ChatMessage.Channel].SendMessage(e.ChatMessage.Channel, e.ChatMessage.Username + ", your transaction was sucessful!");
                    }

                    return;
                }

                HandleEmotes(chatMessage);
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

       private void RegisterCommand(OnMessageReceivedArgs e)
       {
            //Check if they are in the database or not and inserts them
            if (m_Database.InsertUserToDB_BoolCheck(e.ChatMessage.Username) == true)
            {
                m_ClientList[e.ChatMessage.Channel].SendMessage(m_BotChannel, e.ChatMessage.Username + " is now registered!");
                return;
            }

            else
            {
                m_ClientList[e.ChatMessage.Channel].SendMessage(m_BotChannel, e.ChatMessage.Username + " is already registered!");
                return;
            }
        }

        private void HandleEmotes(string[] aChatMessage)
        {
            //split message up and pass it into here
            //loop through words
            //loop through emote names
            //if it is the same, then add one to the counter

            for (int i = 0; i < aChatMessage.Length; i++)
            {
                for (int j = 0; j < m_EmoteNamesInDB.Count; j++)
                {
                    if (aChatMessage[i] == m_EmoteNamesInDB[j])
                    {
                        Console.WriteLine("Emote detected");
                        m_EmoteInfo[m_EmoteNamesInDB[j]].SetAmountOfEmotesSaid(
                        m_EmoteInfo[m_EmoteNamesInDB[j]].GetAmountOFEmotesSaid() + 1);
                    }
                }
            }
        }
    }
}
