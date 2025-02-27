using System;

namespace ProjectTemplate
{
    public class Comment
    {
        internal string userid { get; set; }
        internal DateTime createdAt { get; set; }

        public string content { get; set; }
        public bool isAnonymous { get; set; }

    }

}