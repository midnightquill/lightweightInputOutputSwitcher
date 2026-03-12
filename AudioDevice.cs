namespace InputOutputSwitcher;

internal sealed class AudioDevice
{
    public AudioDevice(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; }
}