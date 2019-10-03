namespace FitsCs.Keys
{
    public class KeyUpdater<T>
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public Maybe.Maybe Value { get; set; }
        public KeyType Type { get; set; }
    }
}
