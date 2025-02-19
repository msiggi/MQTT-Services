using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttServices.Core.Common;

public enum RequestType
{
    Generic,
    GetOne,
    GetAll,
    Upsert,
    Update,
    Insert,
    Delete
}
