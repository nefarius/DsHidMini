# Physical Interface Device Implementation

To be filled...

## Description of files

A fully featured PID descriptor section can grow quite large, therefore logically grouped sections (Set Effect Report, Effect Operation Report, PID Pool Report, etc.) have been split into individual header files which may or may not be included in the final implementation.

The header files are purposely kept without include guard and contain the raw byte array (and comments) in a format that allows them to be easily included in an existing HID Report Descriptor structure. The number prefix on the file names is just for specifying include order, it has no correlation to report IDs.

### 01_PIDStateReport.h

### 02_SetEffectReport.h

### 03_SetEnvelopeReport.h

### 04_SetConditionReport.h

### 05_SetPeriodicReport.h

### 06_SetConstantForceReport.h

### 07_SetRampForceReport.h

### 08_CustomForceDataReport.h

### 09_DownloadForceSample.h

### 10_EffectOperationReport.h

### 11_PIDBlockFreeReport.h

### 12_PIDDeviceControl.h

### 13_DeviceGainReport.h

### 14_SetCustomForceReport.h

### 15_CreateNewEffectReport.h

### 16_PIDBlockLoadReport.h

### 17_PIDPoolReport.h

### PIDTypes.h

Contains supporting types, macros and structures to properly cast and populate the request packet buffer.
