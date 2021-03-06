﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace EmotePrototypev1
{
    class Database
    {
        public MySqlConnectionStringBuilder m_ConnectionString;
        public MySqlConnection m_Connection;
        public string m_Server;
        public string m_Database;
        public string m_uID;
        public string m_Password;
        public uint m_Port;

        public Database()
        {
            m_Server = "localhost";
            m_Database = "twitchecon";
            m_uID = "TestLogin";
            m_Password = "TestLogin";
            m_Port = 3306;

            m_ConnectionString = new MySqlConnectionStringBuilder();
            m_ConnectionString.Server = m_Server;
            m_ConnectionString.UserID = m_uID;
            m_ConnectionString.Password = m_Password;
            m_ConnectionString.Database = m_Database;
            m_ConnectionString.Port = m_Port;
            m_ConnectionString.SslMode = MySqlSslMode.None;

            //m_Connection = new MySqlConnection(m_ConnectionString.ToString());
            var connectstring = m_ConnectionString.ConnectionString;
            m_Connection = new MySqlConnection(connectstring);

        }

        public bool OpenConnection()
        {
            try
            {
                m_Connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        Console.Write("Cannot connect to server.\n");
                        break;

                    case 1045:
                        Console.Write("Invalid username/password");
                        break;
                }
                return false;
            }

        }

        public bool CloseConnection()
        {
            try
            {
                m_Connection.Close();
                return true;
            }

            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public void Insert()
        {

        }

        public void InsertNewEmote(string aEmoteName)
        {
            string query = "INSERT INTO emoteinfo VALUES(NULL, '" + aEmoteName + "', 1, 1, 1, 0, 0)";

            if (m_Connection.Ping() == false)
            {
                this.OpenConnection();
            }

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            int result = cmd.ExecuteNonQuery();

            if (result < 0)
            {
                Console.WriteLine("Error inserting data");
            }

            this.CloseConnection();

        }

        public void Update()
        {

        }

        public void Delete()
        {

        }

        public Tuple<List<string>, Dictionary<string, EmoteInfo>> GetEmoteInfo()
        {
            string query = "SELECT * FROM emoteinfo";
            Dictionary<string, EmoteInfo> emoteInfo = new Dictionary<string, EmoteInfo>();
            List<string> emoteNames = new List<string>();

            DataTable dataTable = new DataTable();

            //Open connection if none
            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            MySqlDataAdapter adapter = new MySqlDataAdapter(query, m_Connection);

            adapter.Fill(dataTable);

            //Go into the datatable that was recieved from the database
            //Take it and put it into an emoteinfo format and add to the dictionary
            foreach (DataRow dataRow in dataTable.Rows)
            {
                EmoteInfo eData = new EmoteInfo(Convert.ToInt32(dataRow[0]), Convert.ToString(dataRow[1]), Convert.ToSingle(dataRow[2]), Convert.ToSingle(dataRow[3]),
                                                Convert.ToSingle(dataRow[4]), Convert.ToSingle(dataRow[5]), Convert.ToSingle(dataRow[6]), Convert.ToSingle(dataRow[7]));

                emoteInfo.Add((string)dataRow[1], eData);

                emoteNames.Add((string)dataRow[1]);
            }

            this.CloseConnection();

            return Tuple.Create(emoteNames, emoteInfo);
        }

        public int GetUserID(string aUsername)
        {
            string query = "SELECT DatabaseUserID FROM userinfo WHERE Username = '" + aUsername + "'";

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);
            MySqlDataReader dataReader;

            dataReader = cmd.ExecuteReader();

            dataReader.Read();

            if (dataReader.HasRows == true)
            {
                int id = dataReader.GetInt32("DatabaseUserID");

                this.CloseConnection();

                return id;
            }

            //Return -1 to know if false
            else
            {
                this.CloseConnection();

                Console.WriteLine("User is not in the DB");
                return -1;
            }

        }

        public bool TradeEmote(string aUsername, string aEmoteToSell, string aEmoteToBuy, float aAmountToBuy, float aAverage)
        {
            //Need to see if both emotes exist for the user
            //Then need to see if they can buy that amount
            //If they can, then it needs to update that they can buy that amount

            float sellVal = 0;
            float buyVal = 0;
            float sellAmount = 0;
            int ID = GetUserID(aUsername);
            string emoteBeingChecked = aEmoteToBuy;

            string queryCheckIfEmoteExists = "SELECT COUNT(*) FROM emoteinfo WHERE EmoteName = '" + emoteBeingChecked + "'";
            string queryGetAmountOfEmote = "SELECT Amount FROM emotecount WHERE EmoteName = '" + aEmoteToSell + "' AND DatabaseUserID = " + ID;
            string queryGetPriceOfEmote = "SELECT CurrentValue FROM emoteinfo WHERE EmoteName = '" + emoteBeingChecked + "'";

            MySqlCommand cmdCheckEmoteExists = new MySqlCommand(queryCheckIfEmoteExists, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            //Check if emote to buy exists
            int emoteExists = Convert.ToInt32(cmdCheckEmoteExists.ExecuteScalar());

            if (emoteExists == 0)
            {
                Console.WriteLine("The emote " + emoteBeingChecked + " does not exist in the database");
                this.CloseConnection();
                return false;
            }

            else if (emoteExists == 1)
            {
                //check for other emote
                emoteBeingChecked = aEmoteToSell;
                //var was updated, need to update the command
                queryGetAmountOfEmote = "SELECT Amount FROM emotecount WHERE EmoteName = '" + aEmoteToSell + "' AND DatabaseUserID = " + ID;
                cmdCheckEmoteExists.CommandText = queryCheckIfEmoteExists;
                emoteExists = Convert.ToInt32(cmdCheckEmoteExists.ExecuteScalar());

                if (emoteExists == 0)
                {
                    Console.WriteLine("The emote" + emoteBeingChecked + " does not exist in the database");
                    this.CloseConnection();
                    return false;
                }

                else if (emoteExists == 1)
                {
                    Console.WriteLine("Both emotes exist in the database");
                }
            }

            //both emotes exist
            //Check if the user has more than 0 or more than amount they want to buy
            MySqlCommand cmdCheckUserAmountOfEmote = new MySqlCommand(queryGetAmountOfEmote, m_Connection);
            float userAmount = (float)cmdCheckUserAmountOfEmote.ExecuteScalar();

            //If it comes back that they have none
            if (userAmount == 0)
            {
                Console.WriteLine(aUsername + " does not have any amount of the emote they are trying to sell.");

                this.CloseConnection();
                return false;
            }

            else if (userAmount > 0)
            {
                //Get the price of both of the emotes
                emoteBeingChecked = aEmoteToBuy;
                queryGetPriceOfEmote = "SELECT CurrentValue FROM emoteinfo WHERE EmoteName = '" + emoteBeingChecked + "'";
                MySqlCommand cmdGetPriceOfEmote = new MySqlCommand(queryGetPriceOfEmote, m_Connection);

                float buyPrice = Convert.ToSingle(cmdGetPriceOfEmote.ExecuteScalar());

                //Change command to the selling emote
                emoteBeingChecked = aEmoteToSell;
                queryGetAmountOfEmote = "SELECT Amount FROM emotecount WHERE EmoteName = '" + aEmoteToSell + "' AND DatabaseUserID = " + ID;
                queryGetPriceOfEmote = "SELECT CurrentValue FROM emoteinfo WHERE EmoteName = '" + emoteBeingChecked + "'";

                cmdGetPriceOfEmote.CommandText = queryGetPriceOfEmote;
                float sellPrice = Convert.ToSingle(cmdGetPriceOfEmote.ExecuteScalar());

                float tradeConversion = sellPrice / buyPrice;

                //This is horrible
                sellVal = sellPrice;
                buyVal = buyPrice;

                //If they enter an amount that is greater than what they can buy
                if ((userAmount * tradeConversion) < aAmountToBuy)
                {
                    Console.WriteLine(aUsername + " tried to buy more than they can afford");
                    this.CloseConnection();
                    return false;
                }

                float purchaseAmount = 0;

                string queryAddToEmoteAmount = "UPDATE emotecount SET Amount = Amount + " + purchaseAmount + " WHERE DatabaseUserID = " + ID + " AND EmoteName = '" + aEmoteToBuy + "'";
                string querySubToEmoteAmount = "UPDATE emotecount SET Amount = " + userAmount + " WHERE DatabaseUserID = " + ID + " AND EmoteName = '" + aEmoteToSell + "'";

                MySqlCommand cmdAddEmote = new MySqlCommand(queryAddToEmoteAmount, m_Connection);
                MySqlCommand cmdSubEmote = new MySqlCommand(querySubToEmoteAmount, m_Connection);

                //purchase amount is how much is being bought
                //Change the useramount based on that
                //Then, add the purchase amount to the index of the emote they are buying


                //-1 is a special code for spending all
                if (aAmountToBuy == -1)
                {
                    purchaseAmount = userAmount / tradeConversion;
                    userAmount = 0;

                    aAmountToBuy = userAmount * tradeConversion;

                    //update text
                    queryAddToEmoteAmount = "UPDATE emotecount SET Amount = Amount + " + aAmountToBuy + " WHERE DatabaseUserID = " + ID + " AND EmoteName = '" + aEmoteToBuy + "'";
                    querySubToEmoteAmount = "UPDATE emotecount SET Amount = " + userAmount + " WHERE DatabaseUserID = " + ID + " AND EmoteName = '" + aEmoteToSell + "'";

                    cmdAddEmote.CommandText = queryAddToEmoteAmount;
                    cmdSubEmote.CommandText = querySubToEmoteAmount;

                    cmdAddEmote.ExecuteNonQuery();
                    cmdSubEmote.ExecuteNonQuery();
                }

                if ((userAmount * tradeConversion) >= aAmountToBuy)
                {
                    purchaseAmount = aAmountToBuy / tradeConversion;
                    userAmount -= purchaseAmount;
                    sellAmount = purchaseAmount;

                    //update text
                    queryAddToEmoteAmount = "UPDATE emotecount SET Amount = Amount + " + aAmountToBuy + " WHERE DatabaseUserID = " + ID + " AND EmoteName = '" + aEmoteToBuy + "'";
                    querySubToEmoteAmount = "UPDATE emotecount SET Amount = " + userAmount + " WHERE DatabaseUserID = " + ID + " AND EmoteName = '" + aEmoteToSell + "'";

                    cmdAddEmote.CommandText = queryAddToEmoteAmount;
                    cmdSubEmote.CommandText = querySubToEmoteAmount;

                    cmdAddEmote.ExecuteNonQuery();
                    cmdSubEmote.ExecuteNonQuery();
                }
                sellAmount = purchaseAmount;

            }

            //Querys for updating the buy/sell
            string queryUpdateBought = "UPDATE emoteinfo SET AmountBought = AmountBought + " + aAmountToBuy + " WHERE EmoteName = '" + aEmoteToBuy + "'";
            string queryUpdateSold = "UPDATE emoteinfo SET AmountSold = AmountSold + " + userAmount + " WHERE EmoteName = '" + aEmoteToSell + "'";

            MySqlCommand cmdBought = new MySqlCommand(queryUpdateBought, m_Connection);
            MySqlCommand cmdSold = new MySqlCommand(queryUpdateSold, m_Connection);


            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            //Should never be 0
            if (sellVal != 0 && buyVal != 0)
            {
                sellVal = (sellAmount / aAverage) / sellVal;
                buyVal = (aAmountToBuy / aAverage) / buyVal;

                string queryBuy = "UPDATE emoteinfo CurrentValue = CurrentValue + " + buyVal + " WHERE EmoteName = '" + aEmoteToBuy + "'";
                string querySell = "UPDATE emoteinfo CurrentValue = CurrentValue + " + sellVal + " WHERE EmoteName = '" + aEmoteToSell + "'";

                MySqlCommand cmdBuy = new MySqlCommand(queryBuy, m_Connection);
                MySqlCommand cmdSell = new MySqlCommand(querySell, m_Connection);

                cmdBuy.ExecuteNonQuery();
                cmdSell.ExecuteNonQuery();
            }

            cmdBought.ExecuteNonQuery();
            cmdSold.ExecuteNonQuery();

            this.CloseConnection();
            return true;
        }

        //More or less just for testing purposes, but this can be used for something maybe...
        public void AddToEmoteCount(string aUsername, string aEmoteName)
        {
            int ID = GetUserID(aUsername);
            int amountToBuy = 1;

            string queryCheckIfUserExists = "SELECT COUNT(" + ID + ") FROM emotecount WHERE EmoteName = " + aEmoteName;
            string queryInsertNewEntry = "INSERT INTO emotecount VALUES ('" + aEmoteName + "', " + ID + ", 1)";
            string queryUpdateEntry = "UPDATE emotecount SET Amount = Amount + " + amountToBuy + " WHERE DatabaseUserID = " + ID + " AND EmoteName = " + aEmoteName;

            MySqlCommand cmdUserCheck = new MySqlCommand(queryCheckIfUserExists, m_Connection);
            MySqlCommand cmdInsertNewEntry = new MySqlCommand(queryInsertNewEntry, m_Connection);
            MySqlCommand cmdUpdateEntry = new MySqlCommand(queryUpdateEntry, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            //Check if user is in
            int userCount = (int)cmdUserCheck.ExecuteScalar();

            //User isn't in db
            if (userCount == 0)
            {
                int result = cmdInsertNewEntry.ExecuteNonQuery();

                if (result < 0)
                {
                    Console.WriteLine("Error inserting new entry for user emote amount, Database.cs -> AddToEmoteCount");
                }
            }

            //User is in db
            else
            {
                int result = cmdUpdateEntry.ExecuteNonQuery();

                if (result < 0)
                {
                    Console.WriteLine("Error updating entry for user emote amount, Database.cs Line 227 -> AddToEmoteCount");
                }
            }

            this.CloseConnection();

        }

        public void InsertUserToDB(string aUsername)
        {
            bool isInDB = CheckForUserInDB(aUsername, "userinfo");

            if (isInDB == true)
            {
                Console.WriteLine(aUsername + " is already in the userinfo database");
                return;
            }

            else
            {
                string query = "INSERT INTO userinfo VALUES (NULL, '" + aUsername + "', CURRENT_TIMESTAMP, 300)";

                if (m_Connection.Ping() == false)
                {
                    m_Connection.Open();
                }

                MySqlCommand cmd = new MySqlCommand(query, m_Connection);

                int result = cmd.ExecuteNonQuery();

                if (result < 0)
                {
                    Console.WriteLine("Error inserting a new user");
                }

                //Now create a user emote list in the emotecount
                int ID = GetUserID(aUsername);

                string queryEmotes = "SELECT EmoteName FROM emoteinfo";

                MySqlCommand cmdEmoteList = new MySqlCommand(queryEmotes, m_Connection);
                List<string> listOfEmotes = new List<string>();

                if (m_Connection.Ping() == false)
                {
                    m_Connection.Open();
                }

                MySqlDataReader reader;
                reader = cmdEmoteList.ExecuteReader();

                while (reader.Read())
                {
                    listOfEmotes.Add(reader.GetString(0));
                }

                reader.Close();

                //Create the emotes for the users
                int j;

                for (j = 0; j < listOfEmotes.Count; j++)
                {
                    string queryInsertValues = "INSERT INTO emotecount VALUES ('" + listOfEmotes[j] + "', " + ID + ", 0)";
                    MySqlCommand cmdInsert = new MySqlCommand(queryInsertValues, m_Connection);
                    cmdInsert.ExecuteNonQuery();
                }

                this.CloseConnection();
            }
        }

        //This one returns a bool for if you are in the db or not and inserts it just as the other version of this function
        public bool InsertUserToDB_BoolCheck(string aUsername)
        {
            bool isInDB = CheckForUserInDB(aUsername, "userinfo");

            if (isInDB == true)
            {
                Console.WriteLine(aUsername + " is already in the userinfo database");
                return false;
            }

            else
            {
                string query = "INSERT INTO userinfo VALUES (NULL, '" + aUsername + "', CURRENT_TIMESTAMP, 300)";

                if (m_Connection.Ping() == false)
                {
                    m_Connection.Open();
                }

                MySqlCommand cmd = new MySqlCommand(query, m_Connection);

                int result = cmd.ExecuteNonQuery();

                if (result < 0)
                {
                    Console.WriteLine("Error inserting a new user");
                }

                //Now create a user emote list in the emotecount
                int ID = GetUserID(aUsername);

                string queryEmotes = "SELECT EmoteName FROM emoteinfo";

                MySqlCommand cmdEmoteList = new MySqlCommand(queryEmotes, m_Connection);
                List<string> listOfEmotes = new List<string>();

                if (m_Connection.Ping() == false)
                {
                    m_Connection.Open();
                }

                MySqlDataReader reader;
                reader = cmdEmoteList.ExecuteReader();

                while (reader.Read())
                {
                    listOfEmotes.Add(reader.GetString(0));
                }

                reader.Close();

                //Create the emotes for the users
                int j;

                for (j = 0; j < listOfEmotes.Count; j++)
                {
                    string queryInsertValues = "INSERT INTO emotecount VALUES ('" + listOfEmotes[j] + "', " + ID + ", 0)";
                    MySqlCommand cmdInsert = new MySqlCommand(queryInsertValues, m_Connection);
                    cmdInsert.ExecuteNonQuery();
                }

                this.CloseConnection();
                return true;
            }
        }

        public void InsertUserToChatDB(string aUsername, int aID)
        {
            string query = "INSERT INTO chatinfo VALUES ('" + aUsername + "', " + aID + ", FALSE" + ")";

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            int result = cmd.ExecuteNonQuery();

            if (result < 0)
            {
                Console.WriteLine("Error inserting a new user");
            }

            this.CloseConnection();

        }

        public void UpdateWhisperOnly(string aUsername, bool aWhisper)
        {
            string query = "UPDATE chatinfo SET WhisperOnly = " + aWhisper + " WHERE Username = '" + aUsername + "'";

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            cmd.ExecuteNonQuery();

            this.CloseConnection();
        }

        public bool GetWhisperOnly(string aUsername)
        {
            bool whisp;

            string query = "SELECT WhisperOnly FROM chatinfo WHERE Username = '" + aUsername + "'";

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            whisp = Convert.ToBoolean(cmd.ExecuteScalar());

            this.CloseConnection();
            return whisp;
        }

        public bool CheckForUserInDB(string aUsername, string aTableName)
        {
            string query = "SELECT Username FROM " + aTableName + " WHERE Username = '" + aUsername + "'";

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);
            MySqlDataReader dataReader;

            dataReader = cmd.ExecuteReader();

            dataReader.Read();

            if (dataReader.HasRows == true && dataReader.GetString(0) == aUsername)
            {
                this.CloseConnection();

                return true;
            }

            else
            {
                this.CloseConnection();

                Console.WriteLine("User is not in the DB");
                return false;
            }

        }

        public List<string> SelectChannelsToBeIn()
        {
            List<string> channelsToReturn = new List<string>();

            string query = "SELECT Username FROM chatinfo";
            MySqlDataReader dataReader;
            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            dataReader = cmd.ExecuteReader();

            while (dataReader.Read())
            {
                string username = dataReader.GetString(0);
                channelsToReturn.Add(username);
            }

            this.CloseConnection();

            return channelsToReturn;
        }

        public DataTable SelectAllEmoteInfo()
        {
            //Get all from emote info
            string query = "SELECT * FROM emoteinfo";

            //Temp to hold data
            DataTable dataTable = new DataTable();

            //Open connection if none
            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }


            MySqlDataAdapter adapter = new MySqlDataAdapter(query, m_Connection);


            adapter.Fill(dataTable);

            foreach (DataRow dataRow in dataTable.Rows)
            {
                foreach (var item in dataRow.ItemArray)
                {
                    Console.Write(item + " ");
                }
                Console.WriteLine("\n");
            }

            this.CloseConnection();

            return dataTable;
        }

        public int GenericCount(string aTableName)
        {
            int count = -1;
            string query = "SELECT count(*) FROM " + aTableName;

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            //Check connection
            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            count = int.Parse(cmd.ExecuteScalar() + "");

            this.CloseConnection();

            return count;
        }

        public void RecordHistory(EmoteInfo aEmoteInfo)
        {
            string query = "INSERT INTO emoterecords VALUES (" + aEmoteInfo.GetID() + ", CURRENT_TIMESTAMP, " + aEmoteInfo.GetValue() + ", " + aEmoteInfo.GetAverage() + ")";

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            cmd.ExecuteNonQuery();

            this.CloseConnection();
            return;
        }

        public void UploadEmoteValues(EmoteInfo aEmoteInfo)
        {
            string query = "UPDATE emoteinfo SET CurrentValue = " + aEmoteInfo.GetValue() +
                                                ", LowestValue = " + aEmoteInfo.GetLowestValue() +
                                                ", HighestValue = " + aEmoteInfo.GetHighestValue() +
                                                ", AmountBought = " + aEmoteInfo.GetAmountBought() +
                                                ", AmountSold = " + aEmoteInfo.GetAmountSold() +
                                                ", Average = " + aEmoteInfo.GetAverage() +
                                                " WHERE EmoteName = '" + aEmoteInfo.GetName() + "'";

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            cmd.ExecuteNonQuery();

            this.CloseConnection();

            return;
        }

        public List<Tuple<string, string>> GetUserWallet(int aUserID)
        {
            string queryWalletAmount = "SELECT COUNT(*) FROM emotecount WHERE DatabaseUserID = " + aUserID;
            MySqlCommand cmdAmount = new MySqlCommand(queryWalletAmount, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            object amount = cmdAmount.ExecuteScalar();

            if (Convert.ToInt32(amount) == 0)
            {
                List<Tuple<string, string>> errorReturn = new List<Tuple<string, string>>();
                Tuple<string, string> errorTuple = new Tuple<string, string>("Error", "Error");
                errorReturn.Add(errorTuple);

                this.CloseConnection();
                return errorReturn;
            }

            else
            {
                string queryWalletInfo = "SELECT EmoteName, Amount FROM emotecount WHERE DatabaseUserID = " + aUserID;
                MySqlCommand cmdWallet = new MySqlCommand(queryWalletAmount, m_Connection);

                List<Tuple<string, string>> walletInfo = new List<Tuple<string, string>>();

                MySqlDataReader reader;

                reader = cmdWallet.ExecuteReader();

                while (reader.Read())
                {
                    Tuple<string, string> fromDB = new Tuple<string, string>(Convert.ToString(reader[0]), Convert.ToString(reader[1]));
                    walletInfo.Add(fromDB);
                }

                this.CloseConnection();
                return walletInfo;
            }

        }

        public float GetMoney(string aUsername)
        {
            float moneyAmount = 0;
            string query = "SELECT money FROM userinfo WHERE Username = '" + aUsername + "'";

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            //Check if user is in the DB
            bool userInDB = CheckForUserInDB(aUsername, "userinfo");

            //return value -1 for error
            if (userInDB == false)
            {
                return -1;
            } 


            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            moneyAmount = Convert.ToSingle(cmd.ExecuteScalar());

            this.CloseConnection();
            return moneyAmount;
        }

        public void SetMoney(string aUsername, float aAmount)
        {
            //Make sure to set aAmount to 2 decimal places
            aAmount = Convert.ToSingle(Math.Round(aAmount, 2));

            string query = "UPDATE userinfo SET money = money + " + aAmount + " WHERE Username = '" + aUsername + "'" ;

            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            //Check if user in db
            bool userInDB = CheckForUserInDB(aUsername, "userinfo");

            if (userInDB == false)
            {
                return;
            }
            
            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            cmd.ExecuteNonQuery();

            this.CloseConnection();

            return;
        }

        public bool BuyEmote(EmoteInfo aEmoteInfo, float aAmount, string aUsername)
        {

            string queryEmoteCost = "SELECT CurrentValue FROM emoteinfo WHERE EmoteName = '" + aEmoteInfo.GetName() + "'";
            float emoteCost = -1;

            string queryCheckIfEmoteInDB = "SELECT COUNT(EmoteName) FROM emoteinfo WHERE EmoteName = '" + aEmoteInfo.GetName() + "'";

            MySqlCommand cmdCost = new MySqlCommand(queryEmoteCost, m_Connection);
            MySqlCommand cmdCheck = new MySqlCommand(queryCheckIfEmoteInDB, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) == 0)
            {
                Console.WriteLine("Tried looking for an emote, but it does not exist in the database");
                return false;
            }

            emoteCost = Convert.ToSingle(cmdCost.ExecuteScalar());

            float userMoney = GetMoney(aUsername);

            //Check if user is even in the database for moneywise
            if (userMoney == -1)
            {
                return false;
            }

            float totalCost;
            totalCost = aAmount * aEmoteInfo.GetValue();

            //Check for error
            if (totalCost <= 0)
            {
                Console.WriteLine("Error: total cost is less than 0, something went wrong");
                Console.WriteLine("Emote Value: " + aEmoteInfo.GetValue());
                return false;
            }
            
            //Check if user has enough money to buy that amount
            if  (totalCost > userMoney)
            {
                Console.WriteLine("User does not have enough money to buy the amount they want");
                Console.WriteLine("Total Cost: " + totalCost + " UserMoney: " + userMoney);
                return false;
            }

            //Update the amount of that emote that was bought
            aEmoteInfo.SetAmountBought( aEmoteInfo.GetAmountBought() + aAmount);

            userMoney -= totalCost;
            userMoney *= -1;

            SetMoney(aUsername, userMoney);

            //Effect market value
            float valueIncrease;
            valueIncrease = aEmoteInfo.GetValue() / totalCost;

            aEmoteInfo.SetValue(aEmoteInfo.GetValue() + valueIncrease);

            //Update the user amount for that emote

            int ID = GetUserID(aUsername);

            //Make a query to check if we need to put that emote into the db for that user

            string queryCheckIfInEmoteCount = "SELECT COUNT(*) FROM emotecount WHERE EmoteName = '" + aEmoteInfo.GetName() + "' AND DatabaseUserID = " + ID;
            MySqlCommand cmdCheckEmoteCount = new MySqlCommand(queryCheckIfInEmoteCount, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            if (Convert.ToInt32(cmdCheckEmoteCount.ExecuteScalar()) == 0)
            {
                /*
                string queryUpdateEmoteCount = "INSERT INTO emotecount VALUES ('" + aEmoteInfo.GetName() + "', " + ID + ", 0)";
                MySqlCommand cmdUpdateEmoteCount = new MySqlCommand(queryUpdateEmoteCount, m_Connection);

                cmdUpdateEmoteCount.ExecuteNonQuery();
                */

                this.AddToEmoteCount(aUsername, aEmoteInfo.GetName());
            }

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            string updateUserAmount = "UPDATE emotecount SET Amount = Amount + " + aAmount + " WHERE EmoteName = '" + aEmoteInfo.GetName() + "' AND DatabaseUserID = " + ID;
            MySqlCommand cmdUpdateUserEmoteData = new MySqlCommand(updateUserAmount, m_Connection);

            cmdUpdateUserEmoteData.ExecuteNonQuery();

            this.CloseConnection();
            return true;
        }

        public bool SellEmote(EmoteInfo aEmoteInfo, float aAmount, string aUsername)
        {
            //Update the user amount for that emote
            int ID = GetUserID(aUsername);

            string queryEmoteCost = "SELECT CurrentValue FROM emoteinfo WHERE EmoteName = '" + aEmoteInfo.GetName() + "'";
            float emoteCost = -1;

            string queryCheckIfEmoteInDB = "SELECT COUNT(EmoteName) FROM emoteinfo WHERE EmoteName = '" + aEmoteInfo.GetName() + "'";

            MySqlCommand cmdCost = new MySqlCommand(queryEmoteCost, m_Connection);
            MySqlCommand cmdCheck = new MySqlCommand(queryCheckIfEmoteInDB, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) == 0)
            {
                Console.WriteLine("Tried looking for an emote, but it does not exist in the database");
                return false;
            }

            emoteCost = Convert.ToSingle(cmdCost.ExecuteScalar());

            float userMoney = GetMoney(aUsername);

            //Check if user is even in the database for moneywise
            if (userMoney == -1)
            {
                return false;
            }

            float totalSellValue;
            totalSellValue = aAmount * aEmoteInfo.GetValue();

            //Check for error
            if (totalSellValue <= 0)
            {
                Console.WriteLine("Error: sell value is less than 0, something went wrong");
                Console.WriteLine("Emote Value: " + aEmoteInfo.GetValue());
                return false;
            }

            //Check if they have enough in the database
            string queryCheckSellAmount = "SELECT Amount FROM emotecount WHERE EmoteName = '" + aEmoteInfo.GetName() + "' AND DatabaseUserID = " + ID;
            MySqlCommand cmdGetAmountOfEmote = new MySqlCommand(queryCheckSellAmount, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            float emoteAmount = Convert.ToSingle(cmdGetAmountOfEmote.ExecuteScalar());

            if (emoteAmount < aAmount)
            {
                Console.WriteLine("Not enough emotes to sell.");
                this.CloseConnection();
                return false;
            }

            //Update the amount of that emote that was bought
            aEmoteInfo.SetAmountSold(aEmoteInfo.GetAmountSold() + aAmount);

            userMoney += totalSellValue;

            SetMoney(aUsername, userMoney);

            //Effect market value
            float valueDecrease;
            valueDecrease = aEmoteInfo.GetValue() / totalSellValue;

            aEmoteInfo.SetValue(aEmoteInfo.GetValue() - valueDecrease);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            string queryUpdateUserAmount = "UPDATE emotecount SET Amount = Amount - " + aAmount + " WHERE EmoteName = '" + aEmoteInfo.GetName() + "' AND DatabaseUserID = " + ID;
            MySqlCommand cmdUpdateUserEmoteData = new MySqlCommand(queryUpdateUserAmount, m_Connection);

            cmdUpdateUserEmoteData.ExecuteNonQuery();

            this.CloseConnection();

            return true;
        }

        public float GetEmoteValue(string aEmoteName)
        {
            float val = 0;

            string query = "SELECT CurrentValue FROM emoteinfo WHERE EmoteName = '" + aEmoteName + "'";
            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            val = Convert.ToSingle(cmd.ExecuteScalar());

            this.CloseConnection();
            return val;
        }

        public float GetAmountOfEmote(string aEmoteName, string aUsername)
        {
            float val = 0;
            int ID = GetUserID(aUsername);

            string query = "SELECT Amount FROM emotecount WHERE EmoteName = '" + aEmoteName + "' AND DatabaseUserID = " + ID;
            MySqlCommand cmd = new MySqlCommand(query, m_Connection);

            if (m_Connection.Ping() == false)
            {
                m_Connection.Open();
            }

            val = Convert.ToSingle(cmd.ExecuteScalar());

            this.CloseConnection();

            return val;

        }
    }
}
