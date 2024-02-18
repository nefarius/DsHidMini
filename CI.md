# CI Notes

## `vcpkg` binary cache

Cache the most heavy packages:

```PowerShell
vcpkg export abseil:x64-windows abseil:x64-windows-static c-ares:x64-windows c-ares:x64-windows-static curl:x64-windows-static grpc:x64-windows grpc:x64-windows-static hidapi:x64-windows-static nlohmann-json:x64-windows-static openssl:x64-windows openssl:x64-windows-static opentelemetry-cpp:x64-windows-static protobuf:x64-windows protobuf:x64-windows-static re2:x64-windows re2:x64-windows-static upb:x64-windows upb:x64-windows-static vcpkg-cmake:x64-windows vcpkg-cmake-config:x64-windows vcpkg-cmake-get-vars:x64-windows zlib:x64-windows-static zlib:x64-windows --raw --output-dir=C:\tools\vcpkg\binary-cache
```

Update the resulting path for `VCPKG_DEFAULT_BINARY_CACHE` in `appveyor.yaml` file.
