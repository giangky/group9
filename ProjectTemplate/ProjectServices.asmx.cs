﻿using System;
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
using System.Web.Script.Services;
using System.Web.Script.Serialization;


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
        public void CreatePost(string title, string content, string userId, string category)
        {

            string sqlInsert = "INSERT into posts (title, content, userid, category, created_at) " +
                "values(@title, @content, @userId, @category, NOW());";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand sqlCommand = new MySqlCommand(sqlInsert, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@title", HttpUtility.UrlDecode(title));
            sqlCommand.Parameters.AddWithValue("@content", HttpUtility.UrlDecode(content));
            sqlCommand.Parameters.AddWithValue("@userId", userId);
            sqlCommand.Parameters.AddWithValue("@category", category);


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
                               SELECT p.post_id, p.title,p.content,p.userid,a.admin,p.created_at,p.review_status,
                               COALESCE(SUM(CASE WHEN v.vote_type = 1 THEN 1 ELSE 0 END), 0) AS upvoteCount,
                               COALESCE(SUM(CASE WHEN v.vote_type = -1 THEN 1 ELSE 0 END), 0) AS downvoteCount
                               FROM posts p
                               LEFT JOIN accounts a ON p.userid = a.userid
                               LEFT JOIN votes v ON p.post_id = v.post_id
                               GROUP BY p.post_id, p.title, p.content, p.userid, a.admin, p.created_at
                               ORDER BY p.created_at DESC
                               LIMIT 8";

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
                    downvoteCount = Convert.ToInt32(sqlDt.Rows[i]["downvoteCount"]),
                    status = sqlDt.Rows[i]["review_status"] == DBNull.Value ? null : sqlDt.Rows[i]["review_status"].ToString(),
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
                               SELECT p.post_id, p.title,p.content,p.userid,a.admin,p.created_at,p.review_status,
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
                    status = sqlDt.Rows[i]["review_status"] == DBNull.Value ? null : sqlDt.Rows[i]["review_status"].ToString(),
                });

            }
            //Return array 
            return posts.ToArray();

        }

        // Get Filtered Feedback for manager dashbaord
        [WebMethod(EnableSession = true)]
        public Post[] GetFilteredFeedback(string startDate, string endDate)
        {
            DataTable sqlDt = new DataTable("posts");

            //Get connection string 
            MySqlConnection sqlConnection = new MySqlConnection(getConString());

            // Parse the startDate and endDate strings into DateTime objects
            DateTime startDateTime;
            DateTime endDateTime;

            // Check if dates are valid
            if (!DateTime.TryParse(startDate, out startDateTime) || !DateTime.TryParse(endDate, out endDateTime))
            {
                throw new ArgumentException("Invalid date format.");
            }

            //SQL query to select variables from table
            string sqlSelect = @"
                            SELECT p.post_id, p.title, p.content, p.userid, a.admin, p.created_at, p.review_status,
                            COALESCE(SUM(CASE WHEN v.vote_type = 1 THEN 1 ELSE 0 END), 0) AS upvoteCount
                            FROM posts p
                            LEFT JOIN accounts a ON p.userid = a.userid
                            LEFT JOIN votes v ON p.post_id = v.post_id
                            WHERE p.created_at BETWEEN @startDate AND @endDate
                            GROUP BY p.post_id, p.title, p.content, p.userid, a.admin, p.created_at
                            ORDER BY upvoteCount DESC
                            LIMIT 5";


            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


            sqlCommand.Parameters.AddWithValue("@startDate", startDateTime);
            sqlCommand.Parameters.AddWithValue("@endDate", endDateTime);

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
                    status = sqlDt.Rows[i]["review_status"] == DBNull.Value ? null : sqlDt.Rows[i]["review_status"].ToString(),
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
        public string EditPost(int postId, string newContent)
        {
            // Check if the user is logged in
            if (Session["uid"] == null)
            {
                return "Unauthorized: User not logged in.";
            }

            string userId = Session["uid"].ToString();

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    // Check if the post belongs to the logged-in user
                    string checkQuery = "SELECT userid FROM posts WHERE post_id = @postId";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, con);
                    checkCmd.Parameters.AddWithValue("@postId", postId);

                    object result = checkCmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        return "Error: Post not found.";
                    }

                    string postUserId = result.ToString();
                    if (postUserId != userId)
                    {
                        return "Unauthorized: You can only edit your own posts.";
                    }

                    // Update the post content
                    string updateQuery = "UPDATE posts SET content = @newContent WHERE post_id = @postId";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, con);
                    updateCmd.Parameters.AddWithValue("@newContent", HttpUtility.UrlDecode(newContent));
                    updateCmd.Parameters.AddWithValue("@postId", postId);

                    int rowsAffected = updateCmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return "Successfully updated post!";
                    }
                    else
                    {
                        return "Error: Failed to update post.";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        // submit comments
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public bool SubmitComment(int postId, string content, bool isAnonymous)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(content))
                {
                    throw new ArgumentException("Comment content cannot be empty.");
                }

                // Save the comment to the database
                // Example: db.Comments.Add(new Comment { PostId = postId, Content = content, IsAnonymous = isAnonymous });
                // db.SaveChanges();

                return true; // Success
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine("Error submitting comment: " + ex.Message);
                return false; // Failure
            }
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
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
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




                }

            }

        }


        [WebMethod(EnableSession = true)]
        public string GetStreak(string userId)
        {
            try
            {


                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    string query = "SELECT streakCount, lastPostDate FROM streak WHERE userId = @userId";
                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count > 0)
                    {
                        int streakCount = Convert.ToInt32(table.Rows[0]["streakCount"]);
                        DateTime lastPostDate = Convert.ToDateTime(table.Rows[0]["lastPostDate"]);
                        return $"{streakCount},{lastPostDate:yyyy-MM-dd}"; // Return streak count and last post date
                    }
                    else
                    {
                        return "0,No previous posts"; // No streak data found for user
                    }
                }
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }

        // WebMethod to update the streak
        [WebMethod(EnableSession = true)]
        public string UpdateStreak(int streakCount, string lastPostDate, string userId)
        {
            try
            {

                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    string query;

                    // Check if the user already has a streak record
                    string checkQuery = "SELECT COUNT(*) FROM streak WHERE userId = @userId";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, con);
                    checkCmd.Parameters.AddWithValue("@userId", userId);
                    int recordExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (recordExists > 0)
                    {
                        query = "UPDATE streak SET streakCount = @streakCount, lastPostDate = @lastPostDate WHERE userId = @userId";
                    }
                    else
                    {

                        query = "INSERT INTO streak (userId, streakCount, lastPostDate) VALUES (@userId, @streakCount, @lastPostDate)";
                    }

                    MySqlCommand cmd = new MySqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@streakCount", streakCount);
                    cmd.Parameters.AddWithValue("@lastPostDate", DateTime.Parse(lastPostDate));

                    cmd.ExecuteNonQuery();
                    return "Streak updated successfully!";
                }
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string SetStatus(int postId, string newStatus)
        {
            Boolean isAdmin = Convert.ToBoolean(Session["admin"]);

            if (Session["admin"] == null || !isAdmin)
            {
                return "Unauthorized. Admin access required.";
            }

            string[] validStatuses = { "Unreviewed", "Reviewed", "In Progress", "Resolved" };
            if (!validStatuses.Contains(newStatus))
            {
                return "Invalid status.";
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    string sqlQuery = "UPDATE posts SET review_status = @status WHERE post_id = @postId";
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@postId", postId);
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return "Success";
                        }
                        else
                        {
                            return "Error: Post not found.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                return "Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string GetStatus(int postId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    string sqlQuery = "SELECT review_status FROM posts WHERE post_id = @postId";
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@postId", postId);

                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return "null";
                        }
                        else
                        {
                            string status = result.ToString();
                            return status;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                return "Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string GetLastPostDate(string userId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    string sqlQuery = "SELECT MAX(created_at) FROM posts WHERE userid = @userId ORDER BY created_at DESC LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return "null";  // No posts made by this user
                        }
                        else
                        {
                            DateTime lastPostDate = Convert.ToDateTime(result);
                            return lastPostDate.ToString("yyyy-MM-dd"); // Return just the date portion (yyyy-MM-dd)
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                return "Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string UpdatePoints(string userId, int points)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();

                    // Update the user's points directly
                    string sqlQuery = "SELECT points FROM accounts WHERE userid = @userId";
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return "Error: User does not have a points record.";
                        }
                        else
                        {
                            // Update the points
                            int currentPoints = Convert.ToInt32(result);
                            int updatedPoints = currentPoints + points;

                            string sqlUpdateQuery = "UPDATE accounts SET points = @updatedPoints WHERE userid = @userId";
                            using (MySqlCommand updateCmd = new MySqlCommand(sqlUpdateQuery, con))
                            {
                                updateCmd.Parameters.AddWithValue("@updatedPoints", updatedPoints);
                                updateCmd.Parameters.AddWithValue("@userId", userId);
                                updateCmd.ExecuteNonQuery();
                            }

                            return "Points updated successfully!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                return "Error: " + ex.Message;
            }
        }

        [WebMethod(EnableSession = true)]
        public string GetUserPoints(string userId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(getConString()))
                {
                    con.Open();
                    string sqlQuery = "SELECT points FROM accounts WHERE userid = @userId";
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return "0"; // If no points are found, return 0
                        }
                        else
                        {
                            return result.ToString(); // Return the points as string
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                return "Error: " + ex.Message;
            }
        }

        // Get votes by category data 
        [WebMethod(EnableSession = true)]
        public string GetUpvoteData()
        {

            string sqlSelect = @"
                       SELECT p.category,
                       COALESCE(SUM(CASE WHEN v.vote_type = 1 THEN 1 ELSE 0 END), 0) AS upvoteCount
                       FROM posts p
                       LEFT JOIN votes v ON p.post_id = v.post_id
                       GROUP BY p.category";

            var feedbackData = new List<object>();


            using (MySqlConnection conn = new MySqlConnection(getConString()))
            {

                //SQL query to select variables from table
                using (MySqlCommand cmd = new MySqlCommand(sqlSelect, conn))
                {

                    conn.Open();

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            feedbackData.Add(new
                            {

                                upvoteCount = Convert.ToInt32(reader["upvoteCount"]),
                                category = reader["category"].ToString(),


                            });
                        }

                    }
                }

            }


            //Convert list to JSON 
            var json = new JavaScriptSerializer().Serialize(new { ChartInfo = feedbackData });
            return json;
        }

        // Get total posts per category 
        [WebMethod(EnableSession = true)]
        public string GetCategoryData()
        {

            string sqlSelect = @"
                       SELECT p.category,
                       COUNT(p.post_id) AS totalPosts
                       FROM posts p
                       GROUP BY p.category";

            var feedbackData = new List<object>();

            using (MySqlConnection conn = new MySqlConnection(getConString()))
            {

                //SQL query to select variables from table
                using (MySqlCommand cmd = new MySqlCommand(sqlSelect, conn))
                {

                    conn.Open();

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            feedbackData.Add(new
                            {

                                totalPosts = reader["totalPosts"].ToString(),
                                category = reader["category"].ToString(),

                            });
                        }

                    }
                }

            }

            //Convert list to JSON 
            var json = new JavaScriptSerializer().Serialize(new { ChartInfo = feedbackData });
            return json;

        }
        // Total posts per category in last 6 months
        [WebMethod(EnableSession = true)]
        public string GetCategoryTimeData()
        {

            string sqlSelect = @"
                        SELECT p.category,
                        DATE_FORMAT(p.created_at, '%Y-%m') AS month,
                        COUNT(p.post_id) AS totalPosts
                        FROM posts p
                        WHERE p.created_at >= CURDATE() - INTERVAL 6 MONTH
                        GROUP BY p.category, month
                        ORDER BY month DESC, p.category;";

            var feedbackData = new List<CategoryPostData>(); 


            using (MySqlConnection conn = new MySqlConnection(getConString()))
            {

                //SQL query to select variables from table
                using (MySqlCommand cmd = new MySqlCommand(sqlSelect, conn))
                {

                    conn.Open();

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            feedbackData.Add(new CategoryPostData
                            {

                                Category = reader["category"] != DBNull.Value ? reader["category"].ToString() : string.Empty,
                                Month = reader["month"] != DBNull.Value ? reader["month"].ToString() : string.Empty,
                                TotalPosts = reader["totalPosts"] != DBNull.Value ? Convert.ToInt32(reader["totalPosts"]) : 0

                            });
                        }

                    }
                }

            }

            // Convert list to JSON and group the data by month and category
            var groupedData = feedbackData
                .GroupBy(item => item.Month)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(
                        item => item.Category,
                        item => item.TotalPosts  // Ensure totalPosts is an integer
                    )
                );


            //Convert list to JSON 
            var json = new JavaScriptSerializer().Serialize(new { ChartInfo = groupedData });
            return json;

        }

 
        }

}

