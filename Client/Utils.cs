using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Client
{
    class Utils
    {
        public static byte[] TrimBytes(byte[] input)
        {
            return input.TakeWhile((v, index) => input.Skip(index).Any(w => w != 0x00)).ToArray();
        }
    }
}
