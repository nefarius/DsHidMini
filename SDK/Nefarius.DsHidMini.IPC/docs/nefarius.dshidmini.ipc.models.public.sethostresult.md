# SetHostResult

Namespace: Nefarius.DsHidMini.IPC.Models.Public

```csharp
public struct SetHostResult
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [ValueType](https://docs.microsoft.com/en-us/dotnet/api/system.valuetype) → [SetHostResult](./nefarius.dshidmini.ipc.models.public.sethostresult.md)

## Fields

### <a id="fields-readstatus"/>**ReadStatus**

The NTSTATUS value of the address read/query action to verify the new address.

```csharp
public uint ReadStatus;
```

### <a id="fields-writestatus"/>**WriteStatus**

The NTSTATUS value of the "pairing" or address overwrite action.

```csharp
public uint WriteStatus;
```

## Methods

### <a id="methods-tostring"/>**ToString()**

```csharp
string ToString()
```

#### Returns

[String](https://docs.microsoft.com/en-us/dotnet/api/system.string)
