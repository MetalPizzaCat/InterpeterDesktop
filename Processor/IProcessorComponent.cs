namespace Emulator
{
    /// <summary>
    /// Basic functions that all processor components must implement in order to work properly
    /// </summary>
    public interface IProcessorComponent
    {
        public void Reset();
    }
}