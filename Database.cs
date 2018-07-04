using System;
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

        public Tuple<List<string>,Dictionary<string, EmoteInfo>> GetEmoteInfo()
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
                EmoteInfo eData = new EmoteInfo((int)dataRow[0], (string)dataRow[1], (float)dataRow[2], (float)dataRow[3], (float)dataRow[4], (float)dataRow[5], (float)dataRow[6], (float)dataRow[7]);
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

        public void AddToEmountCount(string aUsername, string aEmoteToBuy, string aEmoteToSell, int aAmountToBuy)
        {
            //Need to see if both emotes exist for the user
            //Then need to see if they can buy that amount
            //If they can, then it needs to update that they can buy that amount

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
                string query = "INSERT INTO userinfo VALUES (NULL, '" + aUsername + "', CURRENT_TIMESTAMP)";

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
                string query = "INSERT INTO userinfo VALUES (NULL, '" + aUsername + "', CURRENT_TIMESTAMP)";

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
                return true;
            }
        }

        public void InsertUserToChatDB(string aUsername, int aID)
        {
            string query = "INSERT INTO chatinfo VALUES ('" + aUsername + "', " + aID + ")";

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

            while(dataReader.Read())
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

    }
}
