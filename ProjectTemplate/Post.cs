using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjectTemplate
{
    public class Post
    {
        public string postId { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string userId { get; set; }
        public int upvoteCount { get; set; }
        public int downvoteCount { get; set; }
        public bool isAnonymous { get; set; }
        public DateTime createdAt { get; set; }  // Added to track when the post was created
        public List<Comment> comments { get; set; }  // Added to associate comments with the post
        public string status { get; set; } 

        // Constructor to initialize the comments list
        public Post()
        {
            comments = new List<Comment>();
        }
    }
}