namespace Awaken.TG.MVC.Serialization {
    public interface IARStream {
        void AboutToWrite(int byteCount);
        void WriteByte(byte value);
        void Flush();
    }
}