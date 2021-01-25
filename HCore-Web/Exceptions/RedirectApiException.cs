using System;

namespace HCore.Web.Exceptions
{
    public class RedirectApiException : Exception
    {
        public string Location { get; }

        public RedirectApiException(string location) 
        {
            Location = location;
        }
    }
}
