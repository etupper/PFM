using System;
using System.IO;
using System.Collections.Generic;

namespace Common {
    public interface Codec<T> {
        T Decode(PackedFile file);
        void Encode(Stream stream, T toEncode);
    }
}
