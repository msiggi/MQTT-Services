using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttServices.Core.Client;

public class MqttClientSettings
{
    public string ServiceName { get; set; } = "";
    public string BrokerHost { get; set; } = "";
    public int BrokerPort { get; set; }
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}
