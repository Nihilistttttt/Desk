using System.Collections.Generic;

namespace GeekDesk.Constant
{
    public class DictConst
    {
        public static readonly Dictionary<bool, string> batchMenuHeaderDict = new Dictionary<bool, string>();
        static DictConst() {
            batchMenuHeaderDict.Add(true, "取消批量操作");
            batchMenuHeaderDict.Add(false, "批量操作");
        }
    }
}
