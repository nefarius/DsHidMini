# SCP XInput Bridge (Proxy DLL)

## About

This project brings back the [extended XInput API provided by ScpServer/ScpToolkit](https://github.com/nefarius/ScpToolkit/tree/master/ScpXInputBridge) to DsHidMini as a drop-in replacement for the now abandoned [SCP proxy DLL](https://github.com/nefarius/ScpToolkit/tree/9f4076ad6912002687d1824494258607d859c67e/XInput_Scp). In addition to the common XInput functions, it also provides the `XInputGetExtended` API, which returns all possible pressure values.
