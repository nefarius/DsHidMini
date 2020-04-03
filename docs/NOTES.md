# Notes

## Ideas

### cemuhook

- [cemuhook-protocol](https://v1993.github.io/cemuhook-protocol/) - Mostly complete cemuhook protocol reference
- [UDP Server Output Packet Information](https://github.com/Ryochan7/DS4Windows/wiki/UDP-Server-Output-Packet-Information)
- [How to setup your input software to provide motion sensor data](https://cemuhook.sshnuke.net/padudpserver.html)

# Issues

## Output report not working

### Working example (through Shibari)

```text
0000   1b 00 10 64 ca 6b 82 de ff ff 00 00 00 00 09 00   ...d.k..........
0010   00 03 00 02 00 02 03 3a 00 00 00 2c 00 36 00 32   .......:...,.6.2
0020   00 40 00 52 01 01 ff 00 ff 00 00 00 00 00 02 ff   .@.R............
0030   27 10 00 32 ff 27 10 00 32 ff 27 10 00 32 ff 27   '..2.'..2.'..2.'
0040   10 00 32 00 00 00 00 00 00 00 00 00 00 00 00 00   ..2.............
0050   00 00 00 00 00                                    .....
```

### Non-working

```text
0000   1b 00 10 50 f9 6a 82 de ff ff 00 00 00 00 09 00   ...P.j..........
0010   00 03 00 02 00 02 03 3a 00 00 00 2b 00 36 00 32   .......:...+.6.2
0020   00 40 00 52 01 01 ff 00 ff 00 00 00 00 00 00 ff   .@.R............
0030   27 10 00 32 ff 27 10 00 32 ff 27 10 00 32 ff 27   '..2.'..2.'..2.'
0040   10 00 32 00 00 00 00 00 00 00 00 00 00 00 00 00   ..2.............
0050   00 00 00 00 00                                    .....
```
