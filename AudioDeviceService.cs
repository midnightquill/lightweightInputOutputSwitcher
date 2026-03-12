using System.Runtime.InteropServices;

namespace InputOutputSwitcher;

internal sealed class AudioDeviceService
{
    private static readonly Guid MmDeviceEnumeratorClsid = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid PolicyConfigClientClsid = new("870af99c-171d-4f9e-af0d-e63df40c2bc9");

    public IReadOnlyList<AudioDevice> GetRenderDevices() => GetDevices(EDataFlow.eRender);

    public IReadOnlyList<AudioDevice> GetCaptureDevices() => GetDevices(EDataFlow.eCapture);

    public string? GetDefaultRenderDeviceId() => GetDefaultDeviceId(EDataFlow.eRender);

    public string? GetDefaultCaptureDeviceId() => GetDefaultDeviceId(EDataFlow.eCapture);

    public void SetDefaultDevice(string deviceId)
    {
        var policyConfig = CreatePolicyConfig();

        try
        {
            Validate(policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole));
            Validate(policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia));
            Validate(policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications));
        }
        finally
        {
            ReleaseComObject(policyConfig);
        }
    }

    private static IReadOnlyList<AudioDevice> GetDevices(EDataFlow flow)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDeviceCollection? collection = null;
        var devices = new List<AudioDevice>();

        try
        {
            enumerator = CreateDeviceEnumerator();
            Validate(enumerator.EnumAudioEndpoints(flow, DEVICE_STATE.ACTIVE, out collection));

            Validate(collection.GetCount(out var count));
            for (var index = 0; index < count; index++)
            {
                IMMDevice? device = null;
                IPropertyStore? propertyStore = null;
                PropVariant friendlyName = default;

                try
                {
                    Validate(collection.Item(index, out device));
                    Validate(device.GetId(out var id));
                    Validate(device.OpenPropertyStore(StorageAccessMode.Read, out propertyStore));
                    var key = PropertyKeys.DeviceFriendlyName;
                    Validate(propertyStore.GetValue(ref key, out friendlyName));

                    var name = friendlyName.GetValue() ?? "Unknown device";
                    devices.Add(new AudioDevice(id, name));
                }
                finally
                {
                    friendlyName.Clear();
                    ReleaseComObject(propertyStore);
                    ReleaseComObject(device);
                }
            }
        }
        finally
        {
            ReleaseComObject(collection);
            ReleaseComObject(enumerator);
        }

        return devices
            .OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string? GetDefaultDeviceId(EDataFlow flow)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;

        try
        {
            enumerator = CreateDeviceEnumerator();
            var hr = enumerator.GetDefaultAudioEndpoint(flow, ERole.eMultimedia, out device);
            if (hr != 0)
            {
                return null;
            }

            Validate(device.GetId(out var id));
            return id;
        }
        finally
        {
            ReleaseComObject(device);
            ReleaseComObject(enumerator);
        }
    }

    private static IMMDeviceEnumerator CreateDeviceEnumerator()
    {
        return CreateComInstance<IMMDeviceEnumerator>(MmDeviceEnumeratorClsid);
    }

    private static IPolicyConfig CreatePolicyConfig()
    {
        return CreateComInstance<IPolicyConfig>(PolicyConfigClientClsid);
    }

    private static T CreateComInstance<T>(Guid clsid) where T : class
    {
        var iid = typeof(T).GUID;
        Validate(CoCreateInstance(ref clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, ref iid, out var instance));
        return (T)instance;
    }

    private static void Validate(int hResult)
    {
        if (hResult != 0)
        {
            Marshal.ThrowExceptionForHR(hResult);
        }
    }

    private static void ReleaseComObject(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
        {
            Marshal.ReleaseComObject(instance);
        }
    }

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(
        ref Guid clsid,
        [MarshalAs(UnmanagedType.IUnknown)] object? outer,
        CLSCTX context,
        ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out object instance);
}

[Flags]
internal enum CLSCTX : uint
{
    CLSCTX_INPROC_SERVER = 0x1,
}

internal enum EDataFlow
{
    eRender,
    eCapture,
    eAll,
    EDataFlow_enum_count
}

internal enum ERole
{
    eConsole,
    eMultimedia,
    eCommunications,
    ERole_enum_count
}

[Flags]
internal enum DEVICE_STATE : uint
{
    ACTIVE = 0x00000001,
    DISABLED = 0x00000002,
    NOTPRESENT = 0x00000004,
    UNPLUGGED = 0x00000008,
    MASK_ALL = 0x0000000F
}

internal enum StorageAccessMode
{
    Read = 0,
    Write = 1,
    ReadWrite = 2
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROPERTYKEY
{
    public Guid fmtid;
    public uint pid;
}

internal static class PropertyKeys
{
    public static readonly PROPERTYKEY DeviceFriendlyName = new()
    {
        fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
        pid = 14
    };
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropVariant
{
    private ushort valueType;
    private ushort reserved1;
    private ushort reserved2;
    private ushort reserved3;
    private IntPtr pointerValue;
    private int intValue;

    public string? GetValue()
    {
        return valueType == 31 ? Marshal.PtrToStringUni(pointerValue) : null;
    }

    public void Clear()
    {
        _ = PropVariantClear(ref this);
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant propVariant);
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(EDataFlow dataFlow, DEVICE_STATE stateMask, out IMMDeviceCollection devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IntPtr client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out int numberOfDevices);

    [PreserveSig]
    int Item(int deviceNumber, out IMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid iid, int dwClsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

    [PreserveSig]
    int OpenPropertyStore(StorageAccessMode stgmAccess, out IPropertyStore properties);

    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

    [PreserveSig]
    int GetState(out DEVICE_STATE state);
}

[ComImport]
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    [PreserveSig]
    int GetCount(out int propertyCount);

    [PreserveSig]
    int GetAt(int propertyIndex, out PROPERTYKEY key);

    [PreserveSig]
    int GetValue(ref PROPERTYKEY key, out PropVariant value);

    [PreserveSig]
    int SetValue(ref PROPERTYKEY key, ref PropVariant value);

    [PreserveSig]
    int Commit();
}

[ComImport]
[Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    [PreserveSig]
    int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr format);

    [PreserveSig]
    int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, bool defaultFormat, IntPtr format);

    [PreserveSig]
    int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [PreserveSig]
    int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr endpointFormat, IntPtr mixFormat);

    [PreserveSig]
    int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string deviceId, bool defaultPeriod, IntPtr defaultPeriodValue, IntPtr minimumPeriodValue);

    [PreserveSig]
    int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref long processingPeriod);

    [PreserveSig]
    int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr mode);

    [PreserveSig]
    int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr mode);

    [PreserveSig]
    int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref PROPERTYKEY key, out PropVariant value);

    [PreserveSig]
    int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref PROPERTYKEY key, ref PropVariant value);

    [PreserveSig]
    int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ERole role);

    [PreserveSig]
    int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string deviceId, bool visible);
}

