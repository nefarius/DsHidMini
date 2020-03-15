/*++

Module Name:

    public.h

Abstract:

    This module contains the common declarations shared by driver
    and user applications.

Environment:

    driver and application

--*/

//
// Define an Interface Guid so that apps can find the device and talk to it.
//

DEFINE_GUID (GUID_DEVINTERFACE_dshidmini,
    0xb1190313,0x955b,0x478e,0x9b,0x5e,0x97,0xb3,0x0b,0xd5,0x96,0x86);
// {b1190313-955b-478e-9b5e-97b30bd59686}
