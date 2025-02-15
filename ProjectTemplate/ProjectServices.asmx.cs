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

                //if we found an account, store the id and admin status in the session
                //so we can check those values later on other method calls to see if they 
                //are 1) logged in at all, and 2) and admin or not
                Session["id"] = sqlDt.Rows[0]["id"];
                Session["admin"] = sqlDt.Rows[0]["admin"];
                success = true;
            }
            //return the result!
            return success;
        }
        //Insert query for creating a post
        [WebMethod(EnableSession = true)]
        public void CreatePost(string title, string content, bool isAnonymous)
        {

            string sqlInsert = "INSERT into posts (title, content, is_anonymous, created_at) " +
                "values(@title, @content, @isAnonymous, NOW());";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand sqlCommand = new MySqlCommand(sqlInsert, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@title", HttpUtility.UrlDecode(title));
            sqlCommand.Parameters.AddWithValue("@content", HttpUtility.UrlDecode(content));
            sqlCommand.Parameters.AddWithValue("@isAnonymous", isAnonymous ? 1 : 0);

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
            string sqlSelect = "SELECT id, title, content, is_anonymous FROM posts ORDER BY created_at DESC LIMIT 5";

            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //Data adapter to fill data table
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            //List to hold posts
            List<Post> posts = new List<Post>();

            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                posts.Add(new Post
                {
                    postId = Convert.ToInt32(sqlDt.Rows[i]["id"]),
                    title = sqlDt.Rows[i]["is_anonymous"].ToString() == "1" ? "Anonymous" : sqlDt.Rows[i]["title"].ToString(),
                    content = sqlDt.Rows[i]["content"].ToString()
                });
            }
            //Return array 
            return posts.ToArray();

        }

        //Insert query for creating a comment (Anonymous or Not)
        [WebMethod(EnableSession = true)]
        public void CreateComment(int postId, string content, bool isAnonymous)
        {

            string sqlInsert = "INSERT into comments (post_id, content, is_anonymous, created_at) " +
                "values(@postId, @content, @isAnonymous, NOW());";

            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            MySqlCommand sqlCommand = new MySqlCommand(sqlInsert, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@postId", postId);
            sqlCommand.Parameters.AddWithValue("@content", HttpUtility.UrlDecode(content));
            sqlCommand.Parameters.AddWithValue("@isAnonymous", isAnonymous ? 1 : 0);

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

            //select query gets the question that belongs to the current week
            string sqlSelect = "SELECT question FROM questions WHERE week<=@weekValue ORDER BY id DESC LIMIT 1";

            //sets up connection object
            MySqlConnection sqlConnection = new MySqlConnection(getConString());
            //sets up command object to use connection and query
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //tell our command to replace the @parameters with real values
            //we decode them because they came to us via the web so they were encoded
            //for transmission (funky characters escaped, mostly)
            sqlCommand.Parameters.AddWithValue("@weekValue", HttpUtility.UrlDecode(date));

            //a data adapter acts like a bridge between our command object and 
            //the data we are trying to get back and put in a table object
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            //here's the table we want to fill with the results from our query
            string weeklyQuestion = string.Empty;

            DataTable sqlDt = new DataTable();
            //here we go filling it!
            sqlDa.Fill(sqlDt);
            if (sqlDt.Rows.Count > 0)
            {
                weeklyQuestion = (string)sqlDt.Rows[0]["question"];
            } else
            {
                weeklyQuestion = "Check back again for the Weekly Question!";
            }
            //return the result!
            return weeklyQuestion;
        }
        [WebMethod(EnableSession = true)]
        public bool logOut ()
        {
            HttpContext.Current.Session.Abandon();
            return true;
        }

        //NEW delete-edit comment BRANCH
        //EXAMPLE OF AN UPDATE QUERY
        [WebMethod(EnableSession = true)]
        public void EditComment(string postID, string content)
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                //string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
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

        //EXAMPLE OF A DELETE QUERY
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
    }
}
