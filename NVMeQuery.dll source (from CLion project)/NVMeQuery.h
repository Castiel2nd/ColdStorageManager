#ifndef NVMEQUERY_NVMEQUERY_H
#define NVMEQUERY_NVMEQUERY_H

#include <minwindef.h>
#include <winnt.h>
#include <nvme.h>
#include <winioctl.h>
#include <fileapi.h>
#include <errhandlingapi.h>
#include <windef.h>
#include <WinBase.h>
#include <malloc.h>
#include <utility>
#include <atlstr.h>


#ifndef Pinvoke
#define Pinvoke extern "C" __declspec(dllexport)
#endif

//#ifndef DEBUG_PRINT
//#define DEBUG_PRINT
//#endif

#define IOCTL_SCSI_GET_ADDRESS CTL_CODE(IOCTL_SCSI_BASE, 0x0406, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define IOCTL_INTEL_NVME_PASS_THROUGH CTL_CODE(0xf000, 0xA02, METHOD_BUFFERED, FILE_ANY_ACCESS);




typedef void(__stdcall *MessageChangedCallback)(const wchar_t* string);

struct NVME_IDENTIFY_DEVICE
{
    CHAR		Reserved1[4];
    CHAR		SerialNumber[20];
    CHAR		Model[40];
    CHAR		FirmwareRev[8];
    CHAR		Reserved2[9];
    CHAR		MinorVersion;
    SHORT		MajorVersion;
    CHAR		Reserved3[428];
    CHAR		Reserved4[3584];
};

typedef struct _SCSI_ADDRESS {
    ULONG Length;
    UCHAR PortNumber;
    UCHAR PathId;
    UCHAR TargetId;
    UCHAR Lun;
} SCSI_ADDRESS, *PSCSI_ADDRESS;

enum IO_CONTROL_CODE
{
    DFP_SEND_DRIVE_COMMAND	= 0x0007C084,
    DFP_RECEIVE_DRIVE_DATA	= 0x0007C088,
    IOCTL_SCSI_MINIPORT     = 0x0004D008,
    IOCTL_IDE_PASS_THROUGH  = 0x0004D028, // 2000 or later
    IOCTL_ATA_PASS_THROUGH  = 0x0004D02C, // XP SP2 and 2003 or later
};

typedef struct _SRB_IO_CONTROL
{
    ULONG	HeaderLength;
    UCHAR	Signature[8];
    ULONG	Timeout;
    ULONG	ControlCode;
    ULONG	ReturnCode;
    ULONG	Length;
} SRB_IO_CONTROL;

typedef union
{
    struct
    {
        ULONG Opcode : 8;
        ULONG FUSE : 2;
        ULONG _Rsvd : 4;
        ULONG PSDT : 2;
        ULONG CID : 16;
    } DUMMYSTRUCTNAME;
    ULONG AsDWord;
} NVME_CDW0, * PNVME_CDW0;

typedef union
{
    struct
    {
        ULONG   CNS : 2;
        ULONG   _Rsvd : 30;
    } DUMMYSTRUCTNAME;
    ULONG AsDWord;
} NVME_IDENTIFY_CDW10, * PNVME_IDENTIFY_CDW10;

typedef union
{
    struct
    {
        ULONG   LID : 8;
        ULONG   _Rsvd1 : 8;
        ULONG   NUMD : 12;
        ULONG   _Rsvd2 : 4;
    } DUMMYSTRUCTNAME;
    ULONG   AsDWord;
} NVME_GET_LOG_PAGE_CDW10, * PNVME_GET_LOG_PAGE_CDW10;

typedef struct
{
    // Common fields for all commands
    NVME_CDW0           CDW0;

    ULONG               NSID;
    ULONG               _Rsvd[2];
    ULONGLONG           MPTR;
    ULONGLONG           PRP1;
    ULONGLONG           PRP2;

    // Command independent fields from CDW10 to CDW15
    union
    {
        // Admin Command: Identify (6)
        struct
        {
            NVME_IDENTIFY_CDW10 CDW10;
            ULONG   CDW11;
            ULONG   CDW12;
            ULONG   CDW13;
            ULONG   CDW14;
            ULONG   CDW15;
        } IDENTIFY;

        // Admin Command: Get Log Page (2)
        struct
        {
            NVME_GET_LOG_PAGE_CDW10 CDW10;
            //NVME_GET_LOG_PAGE_CDW10_V13 CDW10;
            ULONG   CDW11;
            ULONG   CDW12;
            ULONG   CDW13;
            ULONG   CDW14;
            ULONG   CDW15;
        } GET_LOG_PAGE;
    } u;
} NVME_CMD, * PNVME_CMD;

typedef struct _INTEL_NVME_PAYLOAD
{
    BYTE    Version;        // 0x001C
    BYTE    PathId;         // 0x001D
    BYTE    TargetID;       // 0x001E
    BYTE    Lun;            // 0x001F
    NVME_CMD Cmd;           // 0x0020 ~ 0x005F
    DWORD   CplEntry[4];    // 0x0060 ~ 0x006F
    DWORD   QueueId;        // 0x0070 ~ 0x0073
    DWORD   ParamBufLen;    // 0x0074
    DWORD   ReturnBufferLen;// 0x0078
    BYTE    __rsvd2[0x28];  // 0x007C ~ 0xA3
} INTEL_NVME_PAYLOAD, * PINTEL_NVME_PAYLOAD;

typedef struct _INTEL_NVME_PASS_THROUGH
{
    SRB_IO_CONTROL SRB;     // 0x0000 ~ 0x001B
    INTEL_NVME_PAYLOAD Payload;
    BYTE DataBuffer[0x1000];
} INTEL_NVME_PASS_THROUGH, * PINTEL_NVME_PASS_THROUGH;

///////////////////////////////////////////////////
// from http://naraeon.net/en/archives/1126
///////////////////////////////////////////////////

#define NVME_STORPORT_DRIVER 0xE000
#define NVME_PASS_THROUGH_SRB_IO_CODE \
	CTL_CODE( NVME_STORPORT_DRIVER, 0x800, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define NVME_SIG_STR "NvmeMini"
#define NVME_SIG_STR_LEN 8
#define NVME_FROM_DEV_TO_HOST 2
#define NVME_IOCTL_VENDOR_SPECIFIC_DW_SIZE 6
#define NVME_IOCTL_CMD_DW_SIZE 16
#define NVME_IOCTL_COMPLETE_DW_SIZE 4
#define NVME_PT_TIMEOUT 40

#define IOCTL_SCSI_GET_ADDRESS \
	CTL_CODE(IOCTL_SCSI_BASE, 0x0406, METHOD_BUFFERED, FILE_ANY_ACCESS)

struct NVME_PASS_THROUGH_IOCTL {
    SRB_IO_CONTROL SrbIoCtrl;
    DWORD          VendorSpecific[NVME_IOCTL_VENDOR_SPECIFIC_DW_SIZE];
    DWORD          NVMeCmd[NVME_IOCTL_CMD_DW_SIZE];
    DWORD          CplEntry[NVME_IOCTL_COMPLETE_DW_SIZE];
    DWORD          Direction;
    DWORD          QueueId;
    DWORD          DataBufferLen;
    DWORD          MetaDataLen;
    DWORD          ReturnBufferLen;
    UCHAR          DataBuffer[4096];
};

typedef struct {
    STORAGE_PROPERTY_ID PropertyId;
    STORAGE_QUERY_TYPE QueryType;
} TStoragePropertyQuery;

typedef struct {
    STORAGE_PROTOCOL_TYPE ProtocolType;
    DWORD   DataType;
    DWORD   ProtocolDataRequestValue;
    DWORD   ProtocolDataRequestSubValue;
    DWORD   ProtocolDataOffset;
    DWORD   ProtocolDataLength;
    DWORD   FixedProtocolReturnData;
    DWORD   Reserved[3];
} TStorageProtocolSpecificData;

typedef struct {
    TStoragePropertyQuery Query;
    TStorageProtocolSpecificData ProtocolSpecific;
    BYTE Buffer[4096];
} TStorageQueryWithBuffer;

class __declspec(dllexport) NVMeQuery final
{
public:
    NVMeQuery(MessageChangedCallback managedDelegate);
    ~NVMeQuery();
    template <class ... T>
    auto LogMessage(T&& ... args) -> void;
    std::string GetSerialNumber(const int physicalDriveId);
    BOOL DoIdentifyDeviceNVMeStorageQuery(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data);
    BOOL DoIdentifyDeviceNVMeJMicron(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data);
    BOOL DoIdentifyDeviceNVMeASMedia(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data);
    BOOL DoIdentifyDeviceNVMeRealtek(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data);
    BOOL DoIdentifyDeviceNVMeSamsung(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data);
    BOOL DoIdentifyDeviceNVMeIntelRst(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data, DWORD* diskSize);
    BOOL GetScsiAddress(const TCHAR* Path, BYTE* PortNumber, BYTE* PathId, BYTE* TargetId, BYTE* Lun);
    BOOL DoIdentifyDeviceNVMeIntel(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE *data);
    HANDLE GetIoCtrlHandle(BYTE index);
    CString GetScsiPath(const TCHAR *Path);

    MessageChangedCallback LogMessageChangedCallback{};

//    auto GetTemp(const wchar_t* nvmePath) -> unsigned long;
//    PNVME_HEALTH_INFO_LOG SmartHealthInfo{};
//    PSTORAGE_PROPERTY_QUERY query{};
//    PSTORAGE_PROTOCOL_SPECIFIC_DATA protocolSpecificData{};
//    PSTORAGE_PROTOCOL_DATA_DESCRIPTOR protocolDataDescriptor{};
};

#endif //NVMEQUERY_NVMEQUERY_H
