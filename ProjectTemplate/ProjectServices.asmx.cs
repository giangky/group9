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
                string testQuery = "select * from test";

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
    }
}
