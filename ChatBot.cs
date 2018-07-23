using System;
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
        string m_BotChannel = "EmoteEconomyBot";

        //Used for emote tracking
        public const float AMOUNT_OF_TIME_SECONDS = 60.0f;

        //Index with the string being the username for the twitch client.
        Dictionary<string, TwitchClient> m_ClientList = new Dictionary<string, TwitchClient>();

        //Index is emote name
        Dictionary<string, EmoteInfo> m_EmoteInfo = new Dictionary<string, EmoteInfo>();
        //Get a list of keys
        List<string> m_EmoteNamesInDB = new List<string>();

        Database m_Database;

        bool m_WhisperOnlyMode = false;

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
            string[] chatMessage = e.WhisperMessage.Message.Split(' ', '\t');

            HandleMessagesGeneric(chatMessage, m_BotChannel, e.WhisperMessage.Username, true, false);
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

            //Set whisper only mode to what it is in the database, test if here based on the specific channel

            m_WhisperOnlyMode = m_Database.GetWhisperOnly(e.ChatMessage.Channel);

            if (m_WhisperOnlyMode == false && e.ChatMessage.IsModerator == false && e.ChatMessage.IsBroadcaster == false)
            {
                HandleMessagesGeneric(chatMessage, e.ChatMessage.Channel, e.ChatMessage.Username, false, false);
            }

            else if (e.ChatMessage.IsModerator == true || e.ChatMessage.IsBroadcaster == true)
            {
                HandleMessagesGeneric(chatMessage, e.ChatMessage.Channel, e.ChatMessage.Username, false, true);
            }

            HandleEmotes(chatMessage);


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

        private void HandleEmoteValueCommand(string aEmote, OnMessageReceivedArgs e)
        {
            bool emoteExists = false;
            float val = 0;

            for (int i = 0; i < m_EmoteNamesInDB.Count; i++)
            {
                if (m_EmoteNamesInDB[i] == aEmote)
                {
                    emoteExists = true;
                    break;
                }
            }

            if (emoteExists == true)
            {
                val = m_Database.GetEmoteValue(aEmote);

                m_ClientList[e.ChatMessage.Channel].SendMessage(e.ChatMessage.Channel, "The value for " + aEmote + " is: " + val);
                //m_ClientList[m_BotChannel.ToLower()].SendWhisper(e.ChatMessage.Username, "The value for " + aEmote + " is: " + val);
            }

            return;
        }

        private void HandleEmoteValueCommand(string aEmote, string aUser, string aChannelName, bool aWhisper)
        {
            bool emoteExists = false;
            float val = 0;

            for (int i = 0; i < m_EmoteNamesInDB.Count; i++)
            {
                if (m_EmoteNamesInDB[i] == aEmote)
                {
                    emoteExists = true;
                    break;
                }
            }

            if (emoteExists == true)
            {
                val = m_Database.GetEmoteValue(aEmote);

                if (aWhisper == false)
                {
                    //m_ClientList[aChannelName].SendMessage(aChannelName, "The value for " + aEmote + " is: " + val);
                    SendMessageToChatGeneric("The value for " + aEmote + " is: " + val, aChannelName);
                }
                //m_ClientList[m_BotChannel.ToLower()].SendWhisper(e.ChatMessage.Username, "The value for " + aEmote + " is: " + val);

                else if (aWhisper == true)
                {
                    SendWhisperGeneric("The value for " + aEmote + " is: " + val, aUser);
                }
            }

            return;
        }

        private void HandleBuySellCommand(bool aBuying, EmoteInfo aEmoteInfo, float aAmount, OnMessageReceivedArgs e)
        {
            bool val = false;

            if (aAmount <= 0)
            {
                return;
            }

            if (aBuying == true)
            {
                val = m_Database.BuyEmote(aEmoteInfo, aAmount, e.ChatMessage.Username);
            }

            else if (aBuying == false)
            {
                val = m_Database.SellEmote(aEmoteInfo, aAmount, e.ChatMessage.Username);
            }

            if (val == true)
            {
                if (aBuying == true)
                {
                    m_ClientList[e.ChatMessage.Channel].SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.Username + ", you have sucessfully bought " + aAmount + " of " + aEmoteInfo.GetName());
                }

                else if (aBuying == false)
                {
                    m_ClientList[e.ChatMessage.Channel].SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.Username + ", you have sucessfully sold " + aAmount + " of " + aEmoteInfo.GetName());
                }
            }
        }

        private void HandleBuySellCommand(bool aBuying, EmoteInfo aEmoteInfo, float aAmount, string aUser, string aChannelName, bool aWhisper)
        {
            bool val = false;

            if (aAmount <= 0)
            {
                return;
            }

            if (aBuying == true)
            {
                val = m_Database.BuyEmote(aEmoteInfo, aAmount, aUser);
            }

            else if (aBuying == false)
            {
                val = m_Database.SellEmote(aEmoteInfo, aAmount, aUser);
            }

            if (val == true)
            {
                if (aBuying == true)
                {
                    //m_ClientList[aChannelName].SendMessage(aChannelName, "@" + aUser + ", you have sucessfully bought " + aAmount + " of " + aEmoteInfo.GetName());
                    if (aWhisper == false)
                    {
                        SendMessageToChatGeneric("@" + aUser + ", you have sucessfully bought " + aAmount + " of " + aEmoteInfo.GetName(), aChannelName);
                    }

                    else if (aWhisper == true)
                    {
                        SendWhisperGeneric("You have sucessfully bought " + aAmount + " of " + aEmoteInfo.GetName(), aUser);
                    }

                }

                else if (aBuying == false)
                {
                    //m_ClientList[aChannelName].SendMessage(aChannelName, "@" + aUser + ", you have sucessfully sold " + aAmount + " of " + aEmoteInfo.GetName());
                    if (aWhisper == false)
                    {
                        SendMessageToChatGeneric("@" + aUser + ", you have sucessfully sold " + aAmount + " of " + aEmoteInfo.GetName(), aChannelName);
                    }

                    else if (aWhisper == true)
                    {
                        SendWhisperGeneric("You have sucessfully sold " + aAmount + " of " + aEmoteInfo.GetName(), aUser);
                    }
                }
            }
        }

        private void HandleWalletEmoteAmount(string aEmoteName, OnMessageReceivedArgs e)
        {
            float amount = m_Database.GetAmountOfEmote(aEmoteName, e.ChatMessage.Username);

            m_ClientList[m_BotChannel.ToLower()].SendWhisper(e.ChatMessage.Username, "You currently have " + amount + " of " + aEmoteName + ".");
        }

        private void HandleWalletEmoteAmount(string aEmoteName, string aUser)
        {
            float amount = m_Database.GetAmountOfEmote(aEmoteName, aUser);

            m_ClientList[m_BotChannel.ToLower()].SendWhisper(aUser, "You currently have " + amount + " of " + aEmoteName + ".");
        }

        private void HandleMoneyCommand(bool aWhisper, OnMessageReceivedArgs e)
        {
            float moneyAmount = m_Database.GetMoney(e.ChatMessage.Username);

            //Debug
            if (moneyAmount == -1)
            {
                Console.WriteLine("Error: User tried to ask how much money they have but they aren't in the database");
                return;
            }

            if (aWhisper == true)
            {
                m_ClientList[m_BotChannel.ToLower()].SendWhisper(e.ChatMessage.Username, "Your current balance is $" + moneyAmount);
                return;
            }

            else if (aWhisper == false)
            {
                m_ClientList[e.ChatMessage.Channel].SendMessage(e.ChatMessage.Channel, "@" + e.ChatMessage.Username + ", Your current balance is $" + moneyAmount);
                return;
            }
        }

        private void HandleMoneyCommand(bool aWhisper, string aUser, string aChannelName)
        {
            float moneyAmount = m_Database.GetMoney(aUser);

            //Debug
            if (moneyAmount == -1)
            {
                Console.WriteLine("Error: User tried to ask how much money they have but they aren't in the database");
                return;
            }

            if (aWhisper == true)
            {
                m_ClientList[m_BotChannel.ToLower()].SendWhisper(aUser, "Your current balance is $" + moneyAmount);
                return;
            }

            else if (aWhisper == false)
            {
                m_ClientList[aChannelName].SendMessage(aChannelName, "@" + aUser + ", Your current balance is $" + moneyAmount);
                return;
            }
        }

        private void RegisterCommand(OnMessageReceivedArgs e)
        {
            //Check if they are in the database or not and inserts them
            if (m_Database.InsertUserToDB_BoolCheck(e.ChatMessage.Username) == true)
            {
                m_ClientList[e.ChatMessage.Channel].SendMessage(e.ChatMessage.Channel, e.ChatMessage.Username + " is now registered!");
                return;
            }

            else
            {
                m_ClientList[e.ChatMessage.Channel].SendMessage(e.ChatMessage.Channel, e.ChatMessage.Username + " is already registered!");
                return;
            }
        }

        private void RegisterCommand(string aChannelName, string aUser, bool aWhisper)
        {
            //Check if they are in the database or not and inserts them
            if (m_Database.InsertUserToDB_BoolCheck(aUser) == true)
            {
                //m_ClientList[aChannelName].SendMessage(aChannelName, aUser + " is now registered!");
                if (aWhisper == false)
                {
                    SendMessageToChatGeneric(aUser + "is now registered!", aChannelName);
                }

                else if (aWhisper == true)
                {
                    SendWhisperGeneric("You are now registered!", aUser);
                }

                return;
            }

            else
            {
                //m_ClientList[aChannelName].SendMessage(aChannelName, aUser + " is already registered!");
                if (aWhisper == false)
                {
                    SendMessageToChatGeneric(aUser + " is already registered!", aChannelName);
                }

                else if (aWhisper == true)
                {
                    SendWhisperGeneric("You are already registered!", aUser);
                }

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

        private void HandleUserWallet(List<Tuple<string, string>> aWallet, OnMessageReceivedArgs e)
        {
            //Check if error
            if (aWallet[0].Item1 == "Error" && aWallet[0].Item2 == "Error")
            {
                //Send message saying user has nothing
                m_ClientList[m_BotChannel.ToLower()].SendWhisper(e.ChatMessage.Username, "You currently have nothing in your wallet!");
                return;
            }

            else
            {
                //Give message saying what the user has
                StringBuilder sb = new StringBuilder();
                sb.Append("You currently have, ");

                for (int i = 0; i < aWallet.Count(); i++)
                {
                    sb.Append(aWallet[i].Item1);
                    sb.Append(": ");
                    sb.Append(aWallet[i].Item2);
                }

                //Depending how big the limit is for whisper length...
                //Might need to do sb.length/count to see how long it is
                //Then split it up into multiple parts

                string whisperMessage = sb.ToString();
                m_ClientList[m_BotChannel.ToLower()].SendWhisper(e.ChatMessage.Username, whisperMessage);

                return;
            }
        }

        private void SendMessageToChatGeneric(string aChatMessage, string aChannelName)
        {
            m_ClientList[aChannelName].SendMessage(aChannelName, aChatMessage);
        }

        private void SendWhisperGeneric(string aChatMessage, string aUser)
        {
            m_ClientList[m_BotChannel].SendWhisper(aUser, aChatMessage);
        }

        private void SetWhisperMode(string aUsername, bool aWhisper)
        {
            m_Database.UpdateWhisperOnly(aUsername, aWhisper);
        }

        //TODO: 
        //Change the message to send and send it
        //Send to whatever based on the whisper
        //Make every call use the generic chat message call
        private void HandleMessagesGeneric(string[] aChatMessage, string aChannelName, string aUser, bool aWhisper, bool aMod)
        {
            //TODO:
            //Clean up duplicates

            //If the client that the message recieved is the main bot
            //This is not needed for the whisper
            if (aChannelName == m_BotChannel)
            {
                if (aChatMessage[0] == ("!Enable"))
                {
                    //Check if they are in the DB or not
                    Console.WriteLine("Checking if in DB");
                    bool inDB = m_Database.CheckForUserInDB(aUser, "userinfo");

                    //If they are in the db, grab their ID and put them into the chat db
                    if (inDB == true)
                    {
                        Console.WriteLine(aUser + " is in the DB userinfo");
                        //Check if in the other DB
                        bool inOtherDB = m_Database.CheckForUserInDB(aUser, "chatinfo");

                        //in both databases
                        if (inOtherDB == true)
                        {
                            Console.WriteLine(aUser + " is in the DB chatinfo");
                            Console.WriteLine(aUser + " is in both of the DB's");

                            //m_ClientList[aChannelName].SendMessage(m_BotChannel, aUser + " is already active!");
                            SendMessageToChatGeneric(aUser + " is already active!", aChannelName);

                            return;
                        }

                        //not in the chatinfo db, put them into it
                        if (inOtherDB == false)
                        {
                            Console.WriteLine(aUser + " is not in the DB chatinfo");
                            int id = m_Database.GetUserID(aUser);

                            m_Database.InsertUserToChatDB(aUser, id);

                            //m_ClientList[aChannelName].SendMessage(m_BotChannel, aUser + " is now active!");
                            //m_ClientList[aUser].SendMessage(aUser, "Emote Economy Bot is now active in this channel!");
                            SendMessageToChatGeneric(aUser + " is now active!", m_BotChannel);
                            SendMessageToChatGeneric("Emote Economy Bot is now active in this channel!", aUser);
                        }
                    }

                    //if they are not in the db, put them into both
                    if (inDB == false)
                    {
                        Console.WriteLine(aUser + " is not in the DB userinfo");

                        //Create user in one db
                        m_Database.InsertUserToDB(aUser);

                        //Grab the id of them
                        int id = m_Database.GetUserID(aUser);

                        //put user into other db
                        m_Database.InsertUserToChatDB(aUser, id);

                        //m_ClientList[aChannelName].SendMessage(m_BotChannel, aUser + " is now registered and enabled!");
                        SendMessageToChatGeneric(aUser + " is not registered and enabled!", m_BotChannel);

                    }

                    //Add it to the current client list
                    //Use the username that the message is recieved from and create a client with the username as well
                    m_ClientList.Add(aUser, CreateClient(aUser));

                    return;
                }

                if (aChatMessage[0] == "!EconomyRegister")
                {
                    RegisterCommand(aChannelName, aUser, false);
                }
            }

            //Every other channel
            //will have if statement for if it is a whisper or not
            else
            {
                if (aChatMessage[0] == "!EconomyRegister")
                {
                    RegisterCommand(aChannelName, aUser, aWhisper);
                }

                //CHAT MESSAGE FOR TRADING
                // 0             1      2   3      4
                // !EconomyTrade Emote1 for Emote2 amount
                if (aChatMessage[0] == "!EconomyTrade" && aChatMessage.Length >= 5)
                {
                    if (aChatMessage[2] != "for")
                    {
                        Console.Write(aUser + " tried to trade but messed up the wording on for");
                        return;
                    }

                    bool parse = float.TryParse(aChatMessage[4], out float result);

                    //parse failed
                    if (parse == false)
                    {
                        Console.WriteLine(aUser + " attempted to use a non float/int value for the amount to buy");
                        return;
                    }

                    bool transaciton = m_Database.TradeEmote(aUser, aChatMessage[1], aChatMessage[3], result, m_EmoteInfo[aChatMessage[1]].GetAverage());

                    //Determine if fail or sucess
                    if (transaciton == true)
                    {
                        //TODO:
                        //Modify market -> Move this call and the buy call to be in the TradeEmote, or make a function to move it in there

                        //Tell user it was accepted
                        //m_ClientList[aChannelName].SendMessage(aChannelName, aUser + ", your transaction was sucessful!");

                        if (aWhisper == false)
                        {
                            SendMessageToChatGeneric(aUser + " , your transaction was sucessful!", aChannelName);
                        }

                        else if (aWhisper == true)
                        {
                            SendWhisperGeneric("Your transaction was sucessful!", aUser);
                        }
                    }

                    return;
                }

                //TODO:
                //SPLIT THE MESSAGE UP IF GREATER THAN CERTAIN AMOUNT
                else if (aChatMessage[0] == "!EconomyWallet")
                {
                    //HandleUserWallet(m_Database.GetUserWallet(m_Database.GetUserID(e.ChatMessage.Username)), e);
                    //m_ClientList[m_BotChannel].SendWhisper(e.ChatMessage.Username, "This command is currently disabled! It breaks the bot. LUL");
                }

                //Output to whisper
                else if (aChatMessage[0] == "!EconomyMoney")
                {
                    HandleMoneyCommand(true, aUser, aChannelName);
                }

                //Output to all of chat, Special case, this also needs to detect for if the chat is on whisper only mode
                else if (aChatMessage[0] == "!EconomyShowMoney" && m_WhisperOnlyMode == false)
                {
                    HandleMoneyCommand(false, aUser, aChannelName);
                }

                //CHAT MESSAGE FOR BUYING/SELLING
                // 0                  1      2 
                // !EconomyBuy/Sell Emote Amount

                //TODO:
                //CLEAN THIS CODE LATER
                else if (aChatMessage[0] == "!EconomyBuy" && aChatMessage.Length >= 2)
                {

                    bool parse = float.TryParse(aChatMessage[2], out float result);

                    //parse failed
                    if (parse == false)
                    {
                        Console.WriteLine(aUser + " attempted to use a non float/int value for the amount to buy");
                        return;
                    }

                    //TODO:
                    //PUT A CHECK FOR EMOTE LIST IN HERE

                    //Send in awhisper and the funciton does the check there
                    HandleBuySellCommand(true, m_EmoteInfo[aChatMessage[1]], result, aUser, aChannelName, aWhisper);
                }

                else if (aChatMessage[0] == "!EconomySell" && aChatMessage.Length >= 2)
                {
                    bool parse = float.TryParse(aChatMessage[2], out float result);

                    //parse failed
                    if (parse == false)
                    {
                        Console.WriteLine(aUser + " attempted to use a non float/int value for the amount to buy");
                        return;
                    }

                    //Whisper is handled inside the function
                    HandleBuySellCommand(false, m_EmoteInfo[aChatMessage[1]], result, aUser, aChannelName, aWhisper);
                }

                //      0             1
                //!EconomyEmoteValue Kappa
                else if (aChatMessage[0] == "!EconomyEmoteValue" && aChatMessage.Length >= 1)
                {
                    //Whisper is handled inside the function
                    HandleEmoteValueCommand(aChatMessage[1], aUser, aChannelName, aWhisper);
                }

                //Is always a send whisper
                else if (aChatMessage[0] == "!EconomyMyEmoteAmount")
                {
                    HandleWalletEmoteAmount(aChatMessage[1], aUser);
                }

                //Change whisper only and check if mod or broadcaster
                //    0             1
                //!EconomyWhisper On/Off
                else if (aChatMessage[0] == "!EconomyWhisper" && aChatMessage.Length >= 1 && aMod == true && aWhisper == false)
                {
                    if (aChatMessage[1].ToLower() == "on" && m_WhisperOnlyMode == false)
                    {
                        m_Database.UpdateWhisperOnly(aChannelName, true);
                    }

                    else if (aChatMessage[1].ToLower() == "off" && m_WhisperOnlyMode == true)
                    {
                        m_Database.UpdateWhisperOnly(aChannelName, false);
                    }
                }

            }

        }
    }
}
