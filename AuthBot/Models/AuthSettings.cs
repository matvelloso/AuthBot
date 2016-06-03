using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthBot.Models
{
    public class AuthSettings
    {
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string EndpointUrl { get; set; }
        public static string Tenant { get; set; }
        public static string RedirectUrl { get; set; }
        public static string Mode { get; set; }
        public static string ResourceId { get; set; }
        public static string[] Scopes { get; set; }


    }
}
