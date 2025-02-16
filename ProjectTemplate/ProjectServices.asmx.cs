using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Principal;
using System.Web.UI.WebControls;
using System.Runtime.Remoting.Messaging;
using System.Configuration;
using System.Xml.Linq;

namespace ProjectTemplate
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]

    public class ProjectServices : System.Web.Services.WebService
    {
        ////////////////////////////////////////////////////////////////////////
        ///replace the values of these variables with your database credentials
        ////////////////////////////////////////////////////////////////////////
        private string dbID = "cis440springA2025team9";
        private string dbPass = "cis440springA2025team9";
        private string dbName = "cis440springA2025team9";
        ////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////
        ///call this method anywhere that you need the connection string!
        ////////////////////////////////////////////////////////////////////////
        private string getConString()
        {
            return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName + "; UID=" + dbID + "; PASSWORD=" + dbPass;
        }
        ////////////////////////////////////////////////////////////////////////



        /////////////////////////////////////////////////////////////////////////
        //don't forget to include this decoration above each method that you want
        //to be exposed as a web service!
        [WebMethod(EnableSession = true)]
        /////////////////////////////////////////////////////////////////////////
        public string TestConnection()
        {
            try
            {
                string testQuery = "select * from accounts";

                ////////////////////////////////////////////////////////////////////////
                ///here's an example of using the getConString method!
                ////////////////////////////////////////////////////////////////////////
                MySqlConnection con = new MySqlConnection(getConString());
                ////////////////////////////////////////////////////////////////////////

                MySqlCommand cmd = new MySqlCommand(testQuery, con);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return "Success!";
            }
            catch (Exception e)
            {
                return "Something went wrong, please check your credentials and db name and try again.  Error: " + e.Message;
            }
        }
        // LogOn method to allow users to access system
        [WebMethod(EnableSession = true)]
        public bool LogOn(string userid, string pass)
        {
            //we return this flag to tell them if they logged in or not
            bool success = false;

            MySqlConnection sqlConnection = new MySqlConnection(getConString());

            string sqlSelect = "SELECT id, admin FROM accounts WHERE userid=@userid and pass=@pass";

            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //tell our command to replace the @parameters with real values
            //we decode them because they came to us via the web so they were encoded
            //for transmission (funky characters escaped, mostly)
            sqlCommand.Parameters.AddWithValue("@userid", HttpUtility.UrlDecode(userid));
            sqlCommand.Parameters.AddWithValue("@pass", HttpUtility.UrlDecode(pass));

         
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            //here's the table we want to fill with the results from our query
            DataTable sqlDt = new DataTable();
            sqlDa.Fill(sqlDt);
            //check to see if any rows were returned.  If they were, it means it's 
            //a legit account
            if (sqlDt.Rows.Count > 0)
            {

                Session["id"] = sqlDt.Rows[0]["id"];
                Session["admin"] = sqlDt.Rows[0]["admin"];
                success = true;
                
            }
            // Return an object containing both success and isAdmin
            return success;
        }
        //Insert query for creating a post
        [WebMethod(EnableSession = true)]
        public void CreatePost(string title, string content, string userId)
        {

            string sqlInsert = "INSERT into posts (title, content, userid, created_at) " +
                "values(@title, @content, @userId, NOW());";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand sqlCommand = new MySqlCommand(sqlInsert, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@title", HttpUtility.UrlDecode(title));
            sqlCommand.Parameters.AddWithValue("@content", HttpUtility.UrlDecode(content));
            sqlCommand.Parameters.AddWithValue("@userId", userId);
            

            sqlConnection.Open();

            //we're using a try/catch so that if the query errors out we can handle it 
            try
            {
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

        //Retrieve posts
        [WebMethod(EnableSession = true)]
        public Post[] GetPosts()
        {

            DataTable sqlDt = new DataTable("posts");

            //Get connection string 
            MySqlConnection sqlConnection = new MySqlConnection(getConString());

            //SQL query to select variables from table
            string sqlSelect = "SELECT p.post_id, p.title, p.content, p.userid, a.admin " +
                                "FROM posts p " +
                                "LEFT JOIN accounts a ON p.userid = a.userid " + // Join to check if poster is an admin
                                "ORDER BY p.created_at DESC LIMIT 5";

            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //Data adapter to fill data table
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            //List to hold posts
            List<Post> posts = new List<Post>();
           

            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                bool isPostFromAdmin = sqlDt.Rows[i]["admin"] != DBNull.Value && Convert.ToBoolean(sqlDt.Rows[i]["admin"]);
                string displayedUserId = isPostFromAdmin ? sqlDt.Rows[i]["userid"].ToString() : "Anonymous"; // Show only if admin

                posts.Add(new Post
                {
                    title = sqlDt.Rows[i]["title"].ToString(),
                    userId = displayedUserId, // Display only if admin, otherwise "Anonymous"
                    content = sqlDt.Rows[i]["content"].ToString()
                });

            }
            //Return array 
            return posts.ToArray();

        }

        //Insert query for creating a comment (Anonymous or Not)
        [WebMethod(EnableSession = true)]
        public void CreateComment(int postId, string content)
        {

            string sqlInsert = "INSERT into comments (post_id, content, created_at) " +
                "values(@postId, @content, NOW());";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand sqlCommand = new MySqlCommand(sqlInsert, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@postId", postId);
            sqlCommand.Parameters.AddWithValue("@content", HttpUtility.UrlDecode(content));
            

            sqlConnection.Open();

            //we're using a try/catch so that if the query errors out we can handle it 
            try
            {
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

        //Retrieve comments for a post
        [WebMethod(EnableSession = true)]
        public Comment[] GetComments(int postId)
        {

            DataTable sqlDt = new DataTable("comments");

            //Get connection string 
            MySqlConnection sqlConnection = new MySqlConnection(getConString());

            //SQL query to select variables from table
            string sqlSelect = "SELECT content, is_anonymous FROM comments WHERE post_id = @postId ORDER BY created_at DESC";

            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
            sqlCommand.Parameters.AddWithValue("@postId", postId);

            //Data adapter to fill data table
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            //List to hold comments
            List<Comment> comments = new List<Comment>();

            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                bool isAnon = sqlDt.Rows[i]["is_anonymous"].ToString() == "1";
                comments.Add(new Comment
                {
                    content = isAnon ? "Anonymous: " + sqlDt.Rows[i]["content"].ToString() : sqlDt.Rows[i]["content"].ToString(),
                    isAnonymous = isAnon
                });
            }
            //Return array 
            return comments.ToArray();

        }
         //weekly question web method
        [WebMethod(EnableSession = true)]
        public string GetQuestion()
        {
            //gets the current date
            string date = DateTime.Today.ToString("yyyy-MM-dd");

            string sqlSelect = "SELECT question FROM questions WHERE week<=@weekValue ORDER BY id DESC LIMIT 1";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@weekValue", HttpUtility.UrlDecode(date));

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            string weeklyQuestion = string.Empty;

            DataTable sqlDt = new DataTable();
            sqlDa.Fill(sqlDt);

            if (sqlDt.Rows.Count > 0)
            {
                weeklyQuestion = (string)sqlDt.Rows[0]["question"];
            } else
            {
                weeklyQuestion = "Check back again for the Weekly Question!";
            }
            return weeklyQuestion;
        }
        [WebMethod(EnableSession = true)]
        public bool logOut ()
        {
            HttpContext.Current.Session.Abandon();
            return true;
        }

        //NEW delete-edit comment BRANCH
        [WebMethod(EnableSession = true)]
        public void EditComment(string postID, string content)
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
        
                string sqlSelect = "update posts set content=@contentValue where post_id=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(getConString());
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(postID));
                sqlCommand.Parameters.AddWithValue("@contentValue", HttpUtility.UrlDecode(content));

                sqlConnection.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                }
                sqlConnection.Close();
            }
        }

        [WebMethod(EnableSession = true)]
        public void DeleteComment(string postID)
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                string sqlSelect = "delete from posts where post_id=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(getConString());
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(postID));

                sqlConnection.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                }
                sqlConnection.Close();
            }
        }


        [WebMethod(EnableSession = true)]
        public void SetNotificationPreference(int userId, bool enableNotifications)
        {
            string sqlUpdate = "UPDATE users SET receive_notifications = @enable WHERE id = @userId";
            using (MySqlConnection sqlConnection = new MySqlConnection(getConString()))
            {
                MySqlCommand sqlCommand = new MySqlCommand(sqlUpdate, sqlConnection);
                sqlCommand.Parameters.AddWithValue("@enable", enableNotifications ? 1 : 0);
                sqlCommand.Parameters.AddWithValue("@userId", userId);
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
        }

        
        [WebMethod(EnableSession = true)]
        public void SendWeeklyFeedbackReminders()
        {
            List<(string email, string question)> reminders = new List<(string, string)>();

            using (MySqlConnection sqlConnection = new MySqlConnection(getConString()))
            {
                string sqlSelect = @"
                    SELECT u.email, q.question 
                    FROM users u, (SELECT question FROM questions ORDER BY week DESC LIMIT 1) q 
                    WHERE u.receive_notifications = 1";

                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
                sqlConnection.Open();
                using (MySqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reminders.Add((reader["email"].ToString(), reader["question"].ToString()));
                    }
                }
            }

            foreach (var reminder in reminders)
            {
                SendEmailReminder(reminder.email, reminder.question);
            }
        }

        private void SendEmailReminder(string email, string question)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.yourserver.com");
                mail.From = new MailAddress("no-reply@yourcompany.com");
                mail.To.Add(email);
                mail.Subject = "Weekly Feedback Reminder";
                mail.Body = "Don't forget to submit your weekly feedback! Click here to respond: [LINK]";
                smtpServer.Port = 587;
                smtpServer.Credentials = new System.Net.NetworkCredential("your_email@yourcompany.com", "yourpassword");
                smtpServer.EnableSsl = true;
                smtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                // Log error
            }

        }
    }
}
