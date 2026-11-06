using System;

namespace SevenMinutes.Puzzles
{
    [Serializable]
    public class NoteData
    {
        public string title;
        public string body;
        public bool isSecret;

        public NoteData(string title, string body, bool isSecret = false)
        {
            this.title = title;
            this.body = body;
            this.isSecret = isSecret;
        }
    }
}
