using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Messaging;

    public sealed class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";

        public string Host {get; set;} = "localhost";
        public int Port {get; set;} = 5672;
        public string UserName {get; set;} = "guest";
        public string Password {get; set;} = "guest";
        public string VirtualHost {get; set;} = "/";
    }
