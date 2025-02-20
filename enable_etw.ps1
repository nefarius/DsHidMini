New-EtwTraceSession -Name DsHidMini -LogFileMode 0x8100 -FlushTimer 1 -LocalFilePath "C:\DsHidMini.etl" 2>&1 | Out-Null
Add-EtwTraceProvider -SessionName DsHidMini -Guid '{A56A946C-AC5C-4E2F-9179-6821272856C6}' -MatchAnyKeyword 0x0FFFFFFFFFFFFFFF -Level 0xFF -Property 0x40 2>&1 | Out-Null
