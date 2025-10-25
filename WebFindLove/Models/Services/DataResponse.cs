using System.Collections.Generic;
using System.Net;

namespace WebFindLove.Models.Services
{
    public class DataResponse<T>
    {
        public bool Success { get; set; } = true;
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string? Message { get; set; }
        public T? Data { get; set; }
        public string? ErrorDetails
        {
            get; set;

        }

  
    }

}
