#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("NgMdPsughIAJZAJnM15ti8MO+HtzZqDgmlS6HKWF/4nqHiFBt6bHP9DYWcsrcvQxsj7XkYyJLxrTcOfiA12UMumU47I6jgvYx6FRIeG1BmNyDQpb9IKAKCWDE++IwG4Lu2FuiXLcWyXDS5uK6G9DZu/TxGSI0xoeNbkBx88cwbFDcu/KHc34a7KhehI+WqPHqG3YNXU1hwm6g6R/U52ZCY0OAA8/jQ4FDY0ODg+kRzchljHX8ULeL3GBHChWA5JNCVL40xmxaoZhpkw0PEW9THdxuOs88nnlBNetNUijPfZvHExbLZm0OMR/n+c+2Q+/lGNxyt8nw8tsdwkWY91mIv7enRY/jQ4tPwIJBiWJR4n4Ag4ODgoPDKlQItSEZktqkA0MDg8O");
        private static int[] order = new int[] { 11,12,9,13,7,6,9,10,12,12,10,12,13,13,14 };
        private static int key = 15;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
#endif
