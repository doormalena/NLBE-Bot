using System;

namespace FMWOTB
{
    [Serializable]
    public class JsonNotFoundException : Exception
    {
        private static string message = "Json not found!";
        public JsonNotFoundException() : base(message) { }

        public JsonNotFoundException(string message)
            : base(message) { }

        public JsonNotFoundException(string message, Exception inner)
            : base(message, inner) { }
    }
}
