namespace NiflySharp
{
    public class NiBlockPtrShortArray<T> : NiBlockRefShortArray<T> where T : NiObject
    {
        public override NiBlockPtrShortArray<T> Clone()
        {
            var clone = new NiBlockPtrShortArray<T>
            {
                KeepEmptyRefs = KeepEmptyRefs
            };
            clone.Resize(Count);

            for (int i = 0; i < clone.Count; i++)
                clone.SetBlockRef(i, GetBlockRef(i));

            return clone;
        }
    }
}
