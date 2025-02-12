using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjectTemplate
{
    public class Post
    {
        //this is just a container for all info related
        //to an post.  We'll simply create public class-level
        //variables representing each piece of information!
        public int postId { get; set; }
        public string title { get; set; }
        public string content { get; set; }

        public int upVote { get; set; } = 0;
        public int downVote { get; set; } = 0;
        
        public bool isAnonymous { get; set; }

    }
}