namespace Library
{
    public class BaseObject
    {
        public static implicit operator bool(BaseObject o) => o != null;
    }
}
