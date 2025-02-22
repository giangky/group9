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
using System.Runtime.Remoting.Contexts;
using System.Data.SqlClient;
using System.Net.Mail;
using MySql.Data.MySqlClient.Authentication;

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
        public object LogOn(string userid, string pass)
        {
            // Create an object to hold the response
            var response = new { success = false, isAdmin = false };

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
                Session["uid"] = userid;
                Session["admin"] = sqlDt.Rows[0]["admin"];

                bool isAdmin = (sqlDt.Rows[0]["admin"].ToString() == "1");
                response = new { success = true, isAdmin = isAdmin };

            }
            return response; // Return the object containing both success and admin status
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
            string sqlSelect = @"
                               SELECT p.post_id, p.title,p.content,p.userid,a.admin,p.created_at,
                               COALESCE(SUM(CASE WHEN v.vote_type = 1 THEN 1 ELSE 0 END), 0) AS upvoteCount,
                               COALESCE(SUM(CASE WHEN v.vote_type = -1 THEN 1 ELSE 0 END), 0) AS downvoteCount
                               FROM posts p
                               LEFT JOIN accounts a ON p.userid = a.userid
                               LEFT JOIN votes v ON p.post_id = v.post_id
                               GROUP BY p.post_id, p.title, p.content, p.userid, a.admin, p.created_at
                               ORDER BY p.created_at DESC
                               LIMIT 5";

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
                    content = sqlDt.Rows[i]["content"].ToString(),
                    postId = sqlDt.Rows[i]["post_id"].ToString(),
                    upvoteCount = Convert.ToInt32(sqlDt.Rows[i]["upvoteCount"]),
                    downvoteCount = Convert.ToInt32(sqlDt.Rows[i]["downvoteCount"])
                });

            }
            //Return array 
            return posts.ToArray();

        }

        //Retrieve posts for manager dashboard
        [WebMethod(EnableSession = true)]
        public Post[] GetFeedback()
        {

            DataTable sqlDt = new DataTable("posts");

            //Get connection string 
            MySqlConnection sqlConnection = new MySqlConnection(getConString());

            //SQL query to select variables from table
            string sqlSelect = @"
                               SELECT p.post_id, p.title,p.content,p.userid,a.admin,p.created_at,
                               COALESCE(SUM(CASE WHEN v.vote_type = 1 THEN 1 ELSE 0 END), 0) AS upvoteCount
                               FROM posts p
                               LEFT JOIN accounts a ON p.userid = a.userid
                               LEFT JOIN votes v ON p.post_id = v.post_id
                               GROUP BY p.post_id, p.title, p.content, p.userid, a.admin, p.created_at
                               ORDER BY upvoteCount DESC
                               LIMIT 5";

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
                    content = sqlDt.Rows[i]["content"].ToString(),
                    postId = sqlDt.Rows[i]["post_id"].ToString(),
                    upvoteCount = Convert.ToInt32(sqlDt.Rows[i]["upvoteCount"]),
                    
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

        //gets and automatically updates feedback question weekly
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
            }
            else
            {
                weeklyQuestion = "Check back again for the Weekly Question!";
            }
            return weeklyQuestion;
        }
        [WebMethod(EnableSession = true)]
        public bool logOut()
        {
            HttpContext.Current.Session.Abandon();
            return true;
        }

        //allows original poster to edit their own post
        [WebMethod(EnableSession = true)]
        public String EditComment(string postId, string content)
        {
            if (Session["uid"] == null)
            {
                return "Unauthorized bc null";
            }

            string userId = Session["uid"].ToString();

            string checkQuery = "SELECT userid FROM posts WHERE post_id = @postId";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand checkCommand = new MySqlCommand(checkQuery, sqlConnection);

            checkCommand.Parameters.AddWithValue("@postId", HttpUtility.UrlDecode(postId));
            sqlConnection.Open();
            object result = checkCommand.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return "Comment not found.";
            }

            string postUid = result.ToString();

            if (postUid != userId)
            {
                return "Unauthorized.";
            }

            string updateQuery = "UPDATE posts SET content=@contentValue WHERE post_id=@idValue";
            MySqlCommand updateCommand = new MySqlCommand(updateQuery, sqlConnection);

            updateCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(postId));
            updateCommand.Parameters.AddWithValue("@contentValue", HttpUtility.UrlDecode(content));

            int rowsAffected = updateCommand.ExecuteNonQuery();
            sqlConnection.Close();
            return rowsAffected > 0 ? "Success" : "Failed";

        }

        //allows admin or original poster to delete a comment
        [WebMethod(EnableSession = true)]
        public string DeleteComment(string postId)
        {
            if (Session["admin"] == null || Session["uid"] == null)
            {
                return "Unauthorized bc null";
            }

            string sessionId = Session["uid"].ToString();
            Boolean isAdmin = Convert.ToBoolean(Session["admin"]);

            string checkQuery = "SELECT userid FROM posts WHERE post_id = @postId";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand checkCommand = new MySqlCommand(checkQuery, sqlConnection);

            checkCommand.Parameters.AddWithValue("@postId", HttpUtility.UrlDecode(postId));

            sqlConnection.Open();
            object result = checkCommand.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return "Comment not found.";
            }

            string postUid = result.ToString();

            if (postUid != sessionId && !isAdmin)
            {
                return "Unauthorized. Posts may only be deleted by the original poster or admin.";
            }

            string deleteQuery = "DELETE FROM posts WHERE post_id=@idValue";
            MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, sqlConnection);

            deleteCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(postId));

            int rowsAffected = deleteCommand.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                //sends an email notification if someone other than original poster deletes post
                /*if (postUid != sessionId)
                {
                    string sqlSelect = "SELECT email FROM users WHERE id = '" + postUid + "' AND receive_notifications = 1";

                    MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                    object email = sqlCommand.ExecuteScalar();
                    if (email != null || email != DBNull.Value)
                    {
                        DeletedCommentEmail(email.ToString());
                    }
                }*/ //commented out because unsure how to test email function
                sqlConnection.Close();
                return "Success";
            }
            else
            {
                sqlConnection.Close();
                return "Failed to delete comment.";
            }
        }
        
        private void DeletedCommentEmail(string email)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.yourserver.com");
                mail.From = new MailAddress("no-reply@yourcompany.com");
                mail.To.Add(email);
                mail.Subject = "Your Post Has Been Removed.";
                mail.Body = "Your post has been removed in accordance with our guidelines.";
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

        [WebMethod(EnableSession = true)]
        public void PostVotes(string userId, string postId, int voteType)
        {


            using (MySqlConnection sqlConnection = new MySqlConnection(getConString()))
            {
                sqlConnection.Open();

                // Ensure correct character encoding
                using (MySqlCommand setNamesCmd = new MySqlCommand("SET NAMES utf8mb4;", sqlConnection))
                {
                    setNamesCmd.ExecuteNonQuery();
                }

                // Check if the user has already voted on this post
                string checkVote = "SELECT vote_type FROM votes WHERE userid = @userId AND post_id = @postId";
                using (MySqlCommand sqlSelect = new MySqlCommand(checkVote, sqlConnection))
                {
                    sqlSelect.Parameters.AddWithValue("@userId", userId);
                    sqlSelect.Parameters.AddWithValue("@postId", Convert.ToInt32(postId));
                    sqlSelect.Parameters.AddWithValue("@voteType", voteType);


                    object existingVoteObj = sqlSelect.ExecuteScalar();

                    if (existingVoteObj != null)
                    {
                        int existingVote = Convert.ToInt32(existingVoteObj);
                        if (existingVote != voteType)
                        {
                            // Update existing vote
                            string updateQuery = "UPDATE votes SET vote_type = @voteType WHERE userid = @userId AND post_id = @postId";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, sqlConnection))
                            {
                                updateCmd.Parameters.AddWithValue("@voteType", voteType);
                                updateCmd.Parameters.AddWithValue("@userId", userId);
                                updateCmd.Parameters.AddWithValue("@postId", Convert.ToInt32(postId));
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        // Insert new vote
                        string insertVote = "INSERT INTO votes (userid, post_id, vote_type) VALUES (@userId, @postId, @voteType)";
                        using (MySqlCommand sqlInsert = new MySqlCommand(insertVote, sqlConnection))
                        {
                            sqlInsert.Parameters.AddWithValue("@userId", userId);
                            sqlInsert.Parameters.AddWithValue("@postId", Convert.ToInt32(postId));
                            sqlInsert.Parameters.AddWithValue("@voteType", voteType);
                            sqlInsert.ExecuteNonQuery();
                        }
                    }

                    //// Update the post's vote counts
                    //string updatePostQuery = @"UPDATE posts
                    //                        SET upvotecount = (SELECT COUNT(*) FROM votes WHERE post_id = @postId AND vote_type = 1),
                    //                        downvotecount = (SELECT COUNT(*) FROM votes WHERE post_id = @postId AND vote_type = -1)
                    //                        WHERE post_id = @postId";
                    //using (MySqlCommand updatePostCmd = new MySqlCommand(updatePostQuery, sqlConnection))
                    //{
                    //    updatePostCmd.Parameters.AddWithValue("@postId", postId);
                    //    updatePostCmd.ExecuteNonQuery();
                    //}


                }

            }

        }

    }

}
