using Library.Constant;
using System.Text;

namespace Library.Helper
{
    public static class EncodingHelper
    {
        private static Encoding encoding;

        public static Encoding currentEncoding => encoding ??= Encoding.GetEncoding(EncodingConstant.Encoding);
    }
}
