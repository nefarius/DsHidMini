using System.Runtime.InteropServices;

namespace DsHidMiniModule.DS
{
    public static class DsUtil
    {
        public static byte[] StructureToByteArray(object obj)
        {
            var len = Marshal.SizeOf(obj);

            var arr = new byte[len];

            var ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static void ByteArrayToStructure(byte[] bytearray, ref object obj)
        {
            var len = Marshal.SizeOf(obj);

            var i = Marshal.AllocHGlobal(len);

            Marshal.Copy(bytearray, 0, i, len);

            obj = Marshal.PtrToStructure(i, obj.GetType());

            Marshal.FreeHGlobal(i);
        }
    }
}