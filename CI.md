# CI Notes

## `vcpkg` binary cache

Cache the most heavy packages:

```PowerShell
vcpkg export opentelemetry-cpp:x64-windows-static hidapi:x64-windows-static protobuf:x64-windows-static protobuf:x64-windows curl:x64-windows-static curl:x64-windows abseil:x64-windows-static abseil:x64-windows openssl:x64-windows-static openssl:x64-windows --raw --output-dir=C:\tools\vcpkg\binary-cache
```

Update the resulting path for `VCPKG_DEFAULT_BINARY_CACHE` in `appveyor.yaml` file.
