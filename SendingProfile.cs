using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeySenderLib.KeySenderAdvanced;

namespace KeysSendingApplication2
{
    public class SendingProfile
    {
        public IEnumerable<KeyToSend> Keys { get; set; }
        public bool Repeat { get; set; }
        public string Name { get; set; }
    }
}
