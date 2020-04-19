using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bootdemo.Controllers
{
    public class News
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string UrlToImage { get; set; }
        public DateTime PublishedAt { get; set; }

    }


}
