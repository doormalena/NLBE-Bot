using System;
using System.Collections.Generic;
using System.Text;

namespace FMWOTB
{
    public class TooManyResultsException : Exception
    {
        private static string message = "Too many results were found!";
        public TooManyResultsException() : base(TooManyResultsException.message) { }

        public TooManyResultsException(string message)
            : base(message) { }

        public TooManyResultsException(string message, Exception inner)
            : base(message, inner) { }

    }
}
