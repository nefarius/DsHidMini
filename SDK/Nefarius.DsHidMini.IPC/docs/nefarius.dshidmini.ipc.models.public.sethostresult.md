# SetHostResult

Namespace: Nefarius.DsHidMini.IPC.Models.Public

```csharp
public struct SetHostResult
```

Inheritance [Object](https://learn.microsoft.com/dotnet/api/system.object) → [ValueType](https://learn.microsoft.com/dotnet/api/system.valuetype) → [SetHostResult](./nefarius.dshidmini.ipc.models.public.sethostresult.md)

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

## Properties

### <a id="properties-succeeded"/>**Succeeded**

`true` when both the pairing write and the verify read succeeded.

```csharp
public bool Succeeded { get; }
```

#### Property Value

[Boolean](https://learn.microsoft.com/dotnet/api/system.boolean)<br>

## Methods

### <a id="methods-tostring"/>**ToString()**

```csharp
string ToString()
```

#### Returns

[String](https://learn.microsoft.com/dotnet/api/system.string)
