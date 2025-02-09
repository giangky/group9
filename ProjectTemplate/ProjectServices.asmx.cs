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
		private string getConString() {
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
		public void CreatePost(string title, string content)
		{

			string sqlInsert = "INSERT into posts (title, content, created_at) " +
				"values(@title, @content, NOW());";

			MySqlConnection sqlConnection = new MySqlConnection(getConString());
			MySqlCommand sqlCommand = new MySqlCommand(sqlInsert, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@title", HttpUtility.UrlDecode(title));
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

		[WebMethod(enableSession: true)]
		public Post[] GetPosts()
		{
			
			DataTable sqlDt = new DataTable("posts");

			//Get connection string 
            MySqlConnection sqlConnection = new MySqlConnection(getConString());

			//SQL query to select variables from table
            string sqlSelect = "SELECT title, content FROM posts ORDER BY created_at DESC LIMIT 5";

			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			//Data adapter to fill data table
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			sqlDa.Fill(sqlDt);

			//Lost to hold posts
            List<Post> posts = new List<Post>();

			for (int i = 0; i < sqlDt.Rows.Count; i++)
			{
				posts.Add(new Post
				{
					title = sqlDt.Rows[i]["title"].ToString(),
					content = sqlDt.Rows[i]["content"].ToString()
				});
			}
			//Return array 
			return posts.ToArray();
			

		}
	}
}
