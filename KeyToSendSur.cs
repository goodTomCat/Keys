using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeySenderLib.KeySenderAdvanced;

namespace KeysSendingApplication2
{
    public class KeyToSendSur
    {
        public KeyToSendSur()
        {
            
        }

        public KeyToSendSur(KeyToSend key)
        {
            if (key == null)
                return;

            DelayBeforeAsMSeconds = key.DelayBeforeAsMSeconds;
            DelayAfterAsMSeconds = key.DelayAfterAsMSeconds;
            IsKeyUp = key.IsKeyUp;
            IsVirtualKeyCode = key.IsVirtualKeyCode;
            KeyCode = key.KeyCode;
        }


        public int DelayAfterAsMSeconds { get; set; }
        public int DelayBeforeAsMSeconds { get; set; }
        public bool IsKeyUp { get; set; }
        public bool IsVirtualKeyCode { get; set; }
        public byte KeyCode { get; set; }


        public static implicit operator KeyToSendSur(KeyToSend key)
        {
            if (key == null)
                return null;

            return new KeyToSendSur(key);
        }
        public static implicit operator KeyToSend(KeyToSendSur keySur)
        {
            if (keySur == null)
                return null;

            var key = new KeyToSend(keySur.KeyCode, keySur.IsVirtualKeyCode, keySur.IsKeyUp);
            if (keySur.DelayBeforeAsMSeconds != 0)
                key.DelayBeforeAsMSeconds = keySur.DelayBeforeAsMSeconds;
            if (keySur.DelayAfterAsMSeconds != 0)
                key.DelayAfterAsMSeconds = keySur.DelayAfterAsMSeconds;
            return key;
        }
    }
}
