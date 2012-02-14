using System;
using System.IO;
using System.Collections.Generic;

namespace Common {
    interface Codec<T> {
        T decode(PackedFile file);
        void encode(Stream stream, T toEncode);
    }
}
