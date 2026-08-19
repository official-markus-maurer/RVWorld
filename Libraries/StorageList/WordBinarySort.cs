using System.Collections.Generic;

namespace StorageList
{

    public class WordBinarySort<T>
    {

        public delegate int getByte(T fileGound);

        private List<T>[] baseArray;

        private getByte bgFunc;
        private SortOn<T> sOn;

        public WordBinarySort(getByte bg, SortOn<T> s, List<T> arrToSort)
        {
            bgFunc = bg;
            sOn = s;
            List<T> tmpOut = FastArraySort.SortList(arrToSort, sOn, 3);

            int arrSize = 256 * 256;

            baseArray = new List<T>[arrSize];
            for (int i = 0; i < arrSize; i++)
                baseArray[i] = new List<T>();

            foreach (T toSort in tmpOut)
                baseArray[bgFunc(toSort)].Add(toSort);
        }

        public int ArrSearch(T var, out List<T> arrB, out int index)
        {
            index = 0;
            int bArray = bgFunc(var);
            arrB = baseArray[bArray];
            if (arrB == null)
                return -1;

            return BinarySearch.ListSearch(baseArray[bArray], var, sOn, out index);
        }
    }
}