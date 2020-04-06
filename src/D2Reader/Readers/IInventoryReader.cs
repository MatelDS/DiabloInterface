using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zutatensuppe.D2Reader.Struct;
using Zutatensuppe.D2Reader.Struct.Item;

namespace Zutatensuppe.D2Reader.Readers
{
    public interface IInventoryReader
    {
        IEnumerable<D2Unit> Filter(IEnumerable<D2Unit> enumerable, Func<D2ItemData, D2Unit, bool> filter);
        IEnumerable<D2Unit> EnumerateInventoryBackward(D2Unit unit);
        IEnumerable<D2Unit> EnumerateInventoryForward(D2Unit unit);
    }
}
