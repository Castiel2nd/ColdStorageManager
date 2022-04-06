//
// Created by User on 2022. 03. 04..
//

#ifndef safeCloseHandle
#define safeCloseHandle(h) { if( h != NULL ) { ::CloseHandle(h); h = NULL; } }
#endif

#include <iostream>
#include <cstdarg>
#include "NVMeQuery.h"
#include "SPTIUtil.h"

void printError(const char* errorMsg,...){
    auto lastErrorID = GetLastError();
    if (lastErrorID != 0)
    {
        va_list args;
                va_start(args, errorMsg);
        LPVOID errorBuffer{};
        FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                      nullptr, lastErrorID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&errorBuffer, 0, nullptr);
        printf_s("ErrorCode: %d, ErrorMsg: %s", lastErrorID, errorBuffer);
        vprintf_s(errorMsg, args);
    }
}

NVMeQuery::NVMeQuery(MessageChangedCallback managedDelegate)
{
}

//auto NVMeQuery::GetTemp(const wchar_t* nvmePath) -> unsigned long
//{
//    auto nvmeHandle = CreateFile("\\\\.\\PhysicalDrive3", 0, 0,
//                                 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
//    {
//        auto lastErrorID = GetLastError();
//        if (lastErrorID != 0)
//        {
//            LPVOID errorBuffer{};
//            FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
//                          nullptr, lastErrorID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&errorBuffer, 0, nullptr);
//            printf_s("Query: ERROR creating handle to NVMe [%s]: %d, %s", nvmePath, lastErrorID, errorBuffer);
//        }
//    }
//
//    unsigned long bufferLength = FIELD_OFFSET(STORAGE_PROPERTY_QUERY, AdditionalParameters)
//                                 + sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA) + NVME_MAX_LOG_SIZE;
//    void* buffer = malloc(bufferLength);
//    ZeroMemory(buffer, bufferLength);
//
//    query = (PSTORAGE_PROPERTY_QUERY)buffer;
//    protocolDataDescriptor = (PSTORAGE_PROTOCOL_DATA_DESCRIPTOR)buffer;
//    protocolSpecificData = (PSTORAGE_PROTOCOL_SPECIFIC_DATA)query->AdditionalParameters;
//
//    query->PropertyId = StorageDeviceProtocolSpecificProperty;
//    query->QueryType = PropertyStandardQuery;
//
//
//    protocolSpecificData->ProtocolType = ProtocolTypeNvme;
//    protocolSpecificData->DataType = NVMeDataTypeLogPage;
//    protocolSpecificData->ProtocolDataRequestValue = NVME_LOG_PAGE_HEALTH_INFO;
//    protocolSpecificData->ProtocolDataRequestSubValue = 0;
//    protocolSpecificData->ProtocolDataOffset = sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA);
//    protocolSpecificData->ProtocolDataLength = sizeof(NVME_HEALTH_INFO_LOG);
//
//    unsigned long returnedLength{};
//
//    auto result = DeviceIoControl(nvmeHandle, IOCTL_STORAGE_QUERY_PROPERTY,
//                                  buffer, bufferLength,
//                                  buffer, bufferLength,
//                                  &returnedLength, nullptr);
//
//    if (!result || returnedLength == 0)
//    {
//        auto lastErrorID = GetLastError();
//        LPVOID errorBuffer{};
//        FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
//                      nullptr, lastErrorID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&errorBuffer, 0, nullptr);
//        printf_s("Query: drive path: %s, nvmeHandle %lu\n", nvmePath, nvmeHandle);
//        printf_s("Query: ERROR DeviceIoControl 0x%x %s\n", lastErrorID, errorBuffer);
//    }
//
//    if (protocolDataDescriptor->Version != sizeof(STORAGE_PROTOCOL_DATA_DESCRIPTOR) ||
//        protocolDataDescriptor->Size != sizeof(STORAGE_PROTOCOL_DATA_DESCRIPTOR))
//    {
//        printf_s("Query: Data descriptor header not valid (size of descriptor: %llu)\n", sizeof(STORAGE_PROTOCOL_DATA_DESCRIPTOR));
//        printf_s("Query: DataDesc: version %lu, size %lu\n", protocolDataDescriptor->Version, protocolDataDescriptor->Size);
//    }
//
//    protocolSpecificData = &protocolDataDescriptor->ProtocolSpecificData;
//    if (protocolSpecificData->ProtocolDataOffset < sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA) ||
//        protocolSpecificData->ProtocolDataLength < sizeof(NVME_HEALTH_INFO_LOG))
//        printf_s("Query: ProtocolData Offset/Length not valid\n");
//
//    SmartHealthInfo = (PNVME_HEALTH_INFO_LOG)((PCHAR)protocolSpecificData + protocolSpecificData->ProtocolDataOffset);
//    CloseHandle(nvmeHandle);
//    auto temp = ((ULONG)SmartHealthInfo->Temperature[1] << 8 | SmartHealthInfo->Temperature[0]) - 273;
//    return temp;
//}

//auto NVMeQuery::GetSerialNumber(const wchar_t* nvmePath) -> char*
//{
//    auto nvmeHandle = CreateFile("\\\\.\\PhysicalDrive3",
//                                 0,
//                                 (FILE_SHARE_DELETE | FILE_SHARE_READ | FILE_SHARE_WRITE),
//                                 nullptr,
//                                 OPEN_EXISTING,
//                                 FILE_ATTRIBUTE_NORMAL,
//                                 nullptr);
//    {
//        auto lastErrorID = GetLastError();
//        if (lastErrorID != 0)
//        {
//            LPVOID errorBuffer{};
//            FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
//                          nullptr, lastErrorID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&errorBuffer, 0, nullptr);
//            printf_s("Query: ERROR creating handle to NVMe [%s]: %d, %s", nvmePath, lastErrorID, errorBuffer);
//        }
//    }
//
//    unsigned long bufferLength = FIELD_OFFSET(STORAGE_PROPERTY_QUERY, AdditionalParameters)
//                                 + sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA) + NVME_MAX_LOG_SIZE;
//    void* buffer = malloc(bufferLength);
//    ZeroMemory(buffer, bufferLength);
//
//    query = (PSTORAGE_PROPERTY_QUERY)buffer;
//    protocolDataDescriptor = (PSTORAGE_PROTOCOL_DATA_DESCRIPTOR)buffer;
//    protocolSpecificData = (PSTORAGE_PROTOCOL_SPECIFIC_DATA)query->AdditionalParameters;
//
//    query->PropertyId = StorageDeviceProtocolSpecificProperty;
//    query->QueryType = PropertyStandardQuery;
//
//
//    protocolSpecificData->ProtocolType = ProtocolTypeNvme;
//    protocolSpecificData->DataType = NVMeDataTypeIdentify;
//    protocolSpecificData->ProtocolDataRequestValue = NVME_IDENTIFY_CNS_CONTROLLER ;
//    protocolSpecificData->ProtocolDataRequestSubValue = 0;
//    protocolSpecificData->ProtocolDataOffset = sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA);
//    protocolSpecificData->ProtocolDataLength = NVME_MAX_LOG_SIZE;
//
//    ULONG returnedLength = 0;
//
//    auto result = DeviceIoControl(nvmeHandle,
//                                  IOCTL_STORAGE_QUERY_PROPERTY,
//                                  buffer,
//                                  bufferLength,
//                                  buffer,
//                                  bufferLength,
//                                  &returnedLength,
//                                  nullptr);
//    std::cout << "result: " << result << std::endl;
//
//    auto lastErrorID = GetLastError();
//    if (lastErrorID != 0)
//    {
//        LPVOID errorBuffer{};
//        FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
//                      nullptr, lastErrorID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&errorBuffer, 0, nullptr);
//        printf_s("Query: ERROR in DeviceIoControl: %s\n", errorBuffer);
//    }
//
//    //
//    // Validate the returned data.
//    //
//    if ((protocolDataDescriptor->Version != sizeof(STORAGE_PROTOCOL_DATA_DESCRIPTOR)) ||
//        (protocolDataDescriptor->Size != sizeof(STORAGE_PROTOCOL_DATA_DESCRIPTOR))) {
//        printf_s("DeviceNVMeQueryProtocolDataTest: Get Identify Controller Data - data descriptor header not valid.\n");
//        return nullptr;
//    }
//
//    protocolSpecificData = &protocolDataDescriptor->ProtocolSpecificData;
//
//    if ((protocolSpecificData->ProtocolDataOffset < sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA)) ||
//        (protocolSpecificData->ProtocolDataLength < NVME_MAX_LOG_SIZE)) {
//        printf_s("DeviceNVMeQueryProtocolDataTest: Get Identify Controller Data - ProtocolData Offset/Length not valid.\n");
//        return nullptr;
//    }
//
//    //
//    // Identify Controller Data
//    //
//    {
//        PNVME_IDENTIFY_CONTROLLER_DATA identifyControllerData = (PNVME_IDENTIFY_CONTROLLER_DATA)((PCHAR)protocolSpecificData + (protocolSpecificData->ProtocolDataOffset));
//        std::cout << (identifyControllerData->SN) << std::endl;
//        if ((identifyControllerData->VID == 0) ||
//            (identifyControllerData->NN == 0)) {
//            printf_s("DeviceNVMeQueryProtocolDataTest: Identify Controller Data not valid.\n");
//            return nullptr;
//        } else {
//            printf_s("DeviceNVMeQueryProtocolDataTest: ***Identify Controller Data succeeded***.\n");
//        }
//    }
//    return nullptr;
//}

/*auto NVMeQuery::GetSerialNumber(const wchar_t* nvmePath) -> char*
{
    auto nvmeHandle = CreateFile("\\\\.\\PhysicalDrive3",
                                 0,
                                 (FILE_SHARE_DELETE | FILE_SHARE_READ | FILE_SHARE_WRITE),
                                 nullptr,
                                 OPEN_EXISTING,
                                 FILE_ATTRIBUTE_NORMAL,
                                 nullptr);
    {
        auto lastErrorID = GetLastError();
        if (lastErrorID != 0)
        {
            LPVOID errorBuffer{};
            FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                          nullptr, lastErrorID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&errorBuffer, 0, nullptr);
            printf_s("Query: ERROR creating handle to NVMe [%s]: %d, %s", nvmePath, lastErrorID, errorBuffer);
        }
    }

    BOOL    result;
    PVOID   buffer = NULL;
    ULONG   bufferLength = 0;
    ULONG   returnedLength = 0;

    PSTORAGE_PROPERTY_QUERY query = NULL;
    PSTORAGE_PROTOCOL_SPECIFIC_DATA protocolData = NULL;
    PSTORAGE_PROTOCOL_DATA_DESCRIPTOR protocolDataDescr = NULL;

    //
    // Allocate buffer for use.
    //
    bufferLength = FIELD_OFFSET(STORAGE_PROPERTY_QUERY, AdditionalParameters) + sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA) + NVME_MAX_LOG_SIZE;
    buffer = malloc(bufferLength);

    if (buffer == NULL) {
        printf_s("DeviceNVMeQueryProtocolDataTest: allocate buffer failed, exit.\n");
        return nullptr;
    }

    //
    // Initialize query data structure to get Identify Controller Data.
    //
    ZeroMemory(buffer, bufferLength);

    query = (PSTORAGE_PROPERTY_QUERY)buffer;
    protocolDataDescr = (PSTORAGE_PROTOCOL_DATA_DESCRIPTOR)buffer;
    protocolData = (PSTORAGE_PROTOCOL_SPECIFIC_DATA)query->AdditionalParameters;

    query->PropertyId = StorageAdapterProtocolSpecificProperty;
    query->QueryType = PropertyStandardQuery;

    protocolData->ProtocolType = ProtocolTypeNvme;
    protocolData->DataType = NVMeDataTypeIdentify;
    protocolData->ProtocolDataRequestValue = NVME_IDENTIFY_CNS_CONTROLLER;
    protocolData->ProtocolDataRequestSubValue = 0;
    protocolData->ProtocolDataOffset = sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA);
    protocolData->ProtocolDataLength = sizeof(NVME_IDENTIFY_CONTROLLER_DATA);

    //
    // Send request down.
    //
    result = DeviceIoControl(nvmeHandle,
                             IOCTL_STORAGE_QUERY_PROPERTY,
                             buffer,
                             bufferLength,
                             buffer,
                             bufferLength,
                             &returnedLength,
                             NULL
    );

    ZeroMemory(buffer, bufferLength);
    query = (PSTORAGE_PROPERTY_QUERY)buffer;
    protocolDataDescr = (PSTORAGE_PROTOCOL_DATA_DESCRIPTOR)buffer;
    protocolData = (PSTORAGE_PROTOCOL_SPECIFIC_DATA)query->AdditionalParameters;

    query->PropertyId = StorageDeviceProtocolSpecificProperty;
    query->QueryType = PropertyStandardQuery;

    protocolData->ProtocolType = ProtocolTypeNvme;
    protocolData->DataType = NVMeDataTypeLogPage;
    protocolData->ProtocolDataRequestValue = NVME_LOG_PAGE_HEALTH_INFO;
    protocolData->ProtocolDataRequestSubValue = 0;
    protocolData->ProtocolDataOffset = sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA);
    protocolData->ProtocolDataLength = NVME_MAX_LOG_SIZE;

    //
    // Send request down.
    //
    result = DeviceIoControl(nvmeHandle,
                             IOCTL_STORAGE_QUERY_PROPERTY,
                             buffer,
                             bufferLength,
                             buffer,
                             bufferLength,
                             &returnedLength,
                             NULL
    );

    //
    // Validate the returned data.
    //
    if ((protocolDataDescr->Version != sizeof(STORAGE_PROTOCOL_DATA_DESCRIPTOR)) ||
        (protocolDataDescr->Size != sizeof(STORAGE_PROTOCOL_DATA_DESCRIPTOR))) {
        printf_s(("DeviceNVMeQueryProtocolDataTest: Get Identify Controller Data - data descriptor header not valid. %d\n"));
        return nullptr;
    }

    protocolData = &protocolDataDescr->ProtocolSpecificData;

    if ((protocolData->ProtocolDataOffset < sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA)) ||
        (protocolData->ProtocolDataLength < NVME_MAX_LOG_SIZE)) {
        printf_s(("DeviceNVMeQueryProtocolDataTest: Get Identify Controller Data - ProtocolData Offset/Length not valid.\n"));
        return nullptr;
    }

    //
    // Identify Controller Data
    //
    {
        PNVME_IDENTIFY_CONTROLLER_DATA identifyControllerData = (PNVME_IDENTIFY_CONTROLLER_DATA)((PCHAR)protocolData + protocolData->ProtocolDataOffset);

        if ((identifyControllerData->VID == 0) ||
            (identifyControllerData->NN == 0)) {
            printf_s(("DeviceNVMeQueryProtocolDataTest: Identify Controller Data not valid.\n"));
            return nullptr;
        } else {
            printf_s(("DeviceNVMeQueryProtocolDataTest: ***Identify Controller Data succeeded***.\n"));
        }
    }
    return nullptr;
}*/

std::string NVMeQuery::GetSerialNumber(const int physicalDriveId){

    BOOL	found = FALSE;
    std::string ret = "NotFound";
    DWORD	diskSize = 0;
    NVME_IDENTIFY_DEVICE dataStruct = {};

    if(DoIdentifyDeviceNVMeStorageQuery(physicalDriveId, 0, 0, &dataStruct)){
#ifdef DEBUG_PRINT
        printf("DEBUG_PRINT: DoIdentifyDeviceNVMeStorageQuery\n");
#endif
        found = TRUE;
    }else if(DoIdentifyDeviceNVMeJMicron(physicalDriveId, 0, 0, &dataStruct)){
#ifdef DEBUG_PRINT
        printf("DEBUG_PRINT: DoIdentifyDeviceNVMeJMicron\n");
#endif
        found = TRUE;
    }else if(DoIdentifyDeviceNVMeASMedia(physicalDriveId, 0, 0, &dataStruct)){
#ifdef DEBUG_PRINT
        printf("DEBUG_PRINT: DoIdentifyDeviceNVMeASMedia\n");
#endif
        found = TRUE;
    }else if(DoIdentifyDeviceNVMeRealtek(physicalDriveId, 0, 0, &dataStruct)){
#ifdef DEBUG_PRINT
        printf("DEBUG_PRINT: DoIdentifyDeviceNVMeRealtek\n");
#endif
        found = TRUE;
    }else if(DoIdentifyDeviceNVMeSamsung(physicalDriveId, 0, 0, &dataStruct)){
#ifdef DEBUG_PRINT
        printf("DEBUG_PRINT: DoIdentifyDeviceNVMeSamsung\n");
#endif
        found = TRUE;
    }else if(DoIdentifyDeviceNVMeIntelRst(physicalDriveId, 0, 0, &dataStruct, &diskSize)){
#ifdef DEBUG_PRINT
        printf("DEBUG_PRINT: DoIdentifyDeviceNVMeIntelRst\n");
#endif
        found = TRUE;
    }else if(DoIdentifyDeviceNVMeIntel(physicalDriveId, 0, 0, &dataStruct)){
#ifdef DEBUG_PRINT
        printf("DEBUG_PRINT: DoIdentifyDeviceNVMeIntel\n");
#endif
        found = TRUE;
    }

    if(found) {
        char sn[21];
        memcpy_s(sn, 20, dataStruct.SerialNumber, 20);
        sn[20] = '\0';
        CString snString(sn);
        snString.Trim();
        ret = snString;
        //printf_s("SerialNumber: %s\n", snString.GetString());
    }

    return ret;
}

/*---------------------------------------------------------------------------*/
// NVMe Storage Query
// Reference: http://naraeon.net/en/archives/1338
/*---------------------------------------------------------------------------*/

BOOL NVMeQuery::DoIdentifyDeviceNVMeStorageQuery(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data)
{
    CString path;
    path.Format("\\\\.\\PhysicalDrive%d", physicalDriveId);

    HANDLE hIoCtrl = CreateFile(path, GENERIC_READ | GENERIC_WRITE,
                                FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);

    TStorageQueryWithBuffer nptwb;
    BOOL bRet = 0;
    ZeroMemory(&nptwb, sizeof(nptwb));

    nptwb.ProtocolSpecific.ProtocolType = ProtocolTypeNvme;
    nptwb.ProtocolSpecific.DataType = NVMeDataTypeIdentify;
    nptwb.ProtocolSpecific.ProtocolDataOffset = sizeof(TStorageProtocolSpecificData);
    nptwb.ProtocolSpecific.ProtocolDataLength = 4096;
    nptwb.ProtocolSpecific.ProtocolDataRequestValue = 1; /*NVME_IDENTIFY_CNS_CONTROLLER*/
    nptwb.ProtocolSpecific.ProtocolDataRequestSubValue = 0;
    nptwb.Query.PropertyId = StorageAdapterProtocolSpecificProperty;
    nptwb.Query.QueryType = PropertyStandardQuery;
    DWORD dwReturned = 0;

    bRet = DeviceIoControl(hIoCtrl, IOCTL_STORAGE_QUERY_PROPERTY,
                           &nptwb, sizeof(nptwb), &nptwb, sizeof(nptwb), &dwReturned, NULL);
    safeCloseHandle(hIoCtrl);

    memcpy_s(data, sizeof(NVME_IDENTIFY_DEVICE), nptwb.Buffer, sizeof(NVME_IDENTIFY_DEVICE));

    return bRet;
}

/*---------------------------------------------------------------------------*/
//  NVMe JMicron
/*---------------------------------------------------------------------------*/

BOOL NVMeQuery::DoIdentifyDeviceNVMeJMicron(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data)
{
    BOOL	bRet = FALSE;
    HANDLE	hIoCtrl = NULL;
    DWORD	dwReturned = 0;
    DWORD	length;

    SCSI_PASS_THROUGH_WITH_BUFFERS24 sptwb = {};

    if (data == NULL)
    {
        return	FALSE;
    }

    ::ZeroMemory(data, sizeof(NVME_IDENTIFY_DEVICE));

    hIoCtrl = GetIoCtrlHandle(physicalDriveId);

    if (! hIoCtrl || hIoCtrl == INVALID_HANDLE_VALUE)
    {
        return	FALSE;
    }

    //::ZeroMemory(&sptwb, sizeof(SCSI_PASS_THROUGH_WITH_BUFFERS24));

    sptwb.Spt.Length = sizeof(SCSI_PASS_THROUGH);
    sptwb.Spt.PathId = 0;
    sptwb.Spt.TargetId = 0;
    sptwb.Spt.Lun = 0;
    sptwb.Spt.SenseInfoLength = 24;
    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_OUT;
    sptwb.Spt.DataTransferLength = 512;
    sptwb.Spt.TimeOutValue = 2;
    sptwb.Spt.DataBufferOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, DataBuf);
    sptwb.Spt.SenseInfoOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, SenseBuf);

    sptwb.Spt.CdbLength = 12;
    sptwb.Spt.Cdb[0] = 0xA1; // NVME PASS THROUGH
    sptwb.Spt.Cdb[1] = 0x80; // ADMIN
    sptwb.Spt.Cdb[2] = 0;
    sptwb.Spt.Cdb[3] = 0;
    sptwb.Spt.Cdb[4] = 2;
    sptwb.Spt.Cdb[5] = 0;
    sptwb.Spt.Cdb[6] = 0;
    sptwb.Spt.Cdb[7] = 0;
    sptwb.Spt.Cdb[8] = 0;
    sptwb.Spt.Cdb[9] = 0;
    sptwb.Spt.Cdb[10]= 0;
    sptwb.Spt.Cdb[11]= 0;
    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_OUT;
    sptwb.DataBuf[0] = 'N';
    sptwb.DataBuf[1] = 'V';
    sptwb.DataBuf[2] = 'M';
    sptwb.DataBuf[3] = 'E';
    sptwb.DataBuf[8] = 0x06; // Identify
    sptwb.DataBuf[0x30] = 0x01;

    length = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, DataBuf) + sptwb.Spt.DataTransferLength;

    bRet = ::DeviceIoControl(hIoCtrl, IOCTL_SCSI_PASS_THROUGH,
                             &sptwb, length,
                             &sptwb, length, &dwReturned, NULL);

    if (bRet == FALSE)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

//	::ZeroMemory(&sptwb, sizeof(SCSI_PASS_THROUGH_WITH_BUFFERS24));
    sptwb.Spt.Length = sizeof(SCSI_PASS_THROUGH);
    sptwb.Spt.PathId = 0;
    sptwb.Spt.TargetId = 0;
    sptwb.Spt.Lun = 0;
    sptwb.Spt.SenseInfoLength = 24;
    sptwb.Spt.DataTransferLength = 4096;
    sptwb.Spt.TimeOutValue = 2;
    sptwb.Spt.DataBufferOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, DataBuf);
    sptwb.Spt.SenseInfoOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, SenseBuf);

    sptwb.Spt.CdbLength = 12;
    sptwb.Spt.Cdb[0] = 0xA1; // NVME PASS THROUGH
    sptwb.Spt.Cdb[1] = 0x82; // ADMIN + DMA-IN
    sptwb.Spt.Cdb[2] = 0;
    sptwb.Spt.Cdb[3] = 0;
    sptwb.Spt.Cdb[4] = 2;
    sptwb.Spt.Cdb[5] = 0;
    sptwb.Spt.Cdb[6] = 0;
    sptwb.Spt.Cdb[7] = 0;
    sptwb.Spt.Cdb[8] = 0;
    sptwb.Spt.Cdb[9] = 0;
    sptwb.Spt.Cdb[10]= 0;
    sptwb.Spt.Cdb[11]= 0;
    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_IN;

    length = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, DataBuf) + sptwb.Spt.DataTransferLength;
    dwReturned = sizeof(sptwb);

    bRet = ::DeviceIoControl(hIoCtrl, IOCTL_SCSI_PASS_THROUGH,
                             &sptwb, length,
                             &sptwb, length, &dwReturned, NULL);

    if (bRet == FALSE)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    DWORD count = 0;
    for (int i = 0; i < 512; i++)
    {
        count += sptwb.DataBuf[i];
    }
    if (count == 0 || count == 317)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    memcpy_s(data, sizeof(NVME_IDENTIFY_DEVICE), sptwb.DataBuf, sizeof(NVME_IDENTIFY_DEVICE));

    safeCloseHandle(hIoCtrl);

    return TRUE;
}

/*---------------------------------------------------------------------------*/
//  NVMe ASMedia
/*---------------------------------------------------------------------------*/

BOOL NVMeQuery::DoIdentifyDeviceNVMeASMedia(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data)
{
    BOOL	bRet = FALSE;
    HANDLE	hIoCtrl = NULL;
    DWORD	dwReturned = 0;
    DWORD	length;

    SCSI_PASS_THROUGH_WITH_BUFFERS sptwb = {};

    if (data == NULL)
    {
        return	FALSE;
    }

    ::ZeroMemory(data, sizeof(NVME_IDENTIFY_DEVICE));

    hIoCtrl = GetIoCtrlHandle(physicalDriveId);

    if (! hIoCtrl || hIoCtrl == INVALID_HANDLE_VALUE)
    {
        return	FALSE;
    }

    //::ZeroMemory(&sptwb, sizeof(SCSI_PASS_THROUGH_WITH_BUFFERS));
    sptwb.Spt.Length = sizeof(SCSI_PASS_THROUGH);
    sptwb.Spt.PathId = 0;
    sptwb.Spt.TargetId = 0;
    sptwb.Spt.Lun = 0;
    sptwb.Spt.SenseInfoLength = 24;
    sptwb.Spt.DataTransferLength = 4096;
    sptwb.Spt.TimeOutValue = 2;
    sptwb.Spt.DataBufferOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS, DataBuf);
    sptwb.Spt.SenseInfoOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS, SenseBuf);

    sptwb.Spt.CdbLength = 16;
    sptwb.Spt.Cdb[0] = 0xE6; // NVME PASS THROUGH
    sptwb.Spt.Cdb[1] = 0x06; // IDENTIFY
    sptwb.Spt.Cdb[3] = 0x01;

    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_IN;

    length = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS, DataBuf) + sptwb.Spt.DataTransferLength;

    bRet = ::DeviceIoControl(hIoCtrl, IOCTL_SCSI_PASS_THROUGH,
                             &sptwb, length,
                             &sptwb, length, &dwReturned, NULL);

    if (bRet == FALSE)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    DWORD count = 0;
    for (int i = 0; i < 512; i++)
    {
        count += sptwb.DataBuf[i];
    }
    if (count == 0)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    memcpy_s(data, sizeof(NVME_IDENTIFY_DEVICE), sptwb.DataBuf, sizeof(NVME_IDENTIFY_DEVICE));

    safeCloseHandle(hIoCtrl);

    return TRUE;
}

/*---------------------------------------------------------------------------*/
//  NVMe Realtek
/*---------------------------------------------------------------------------*/

BOOL NVMeQuery::DoIdentifyDeviceNVMeRealtek(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data)
{
    BOOL	bRet = FALSE;
    HANDLE	hIoCtrl = NULL;
    DWORD	dwReturned = 0;
    DWORD	length;

    SCSI_PASS_THROUGH_WITH_BUFFERS sptwb = {};

    if (data == NULL)
    {
        return	FALSE;
    }

    ::ZeroMemory(data, sizeof(NVME_IDENTIFY_DEVICE));

    hIoCtrl = GetIoCtrlHandle(physicalDriveId);

    if (! hIoCtrl || hIoCtrl == INVALID_HANDLE_VALUE)
    {
        return	FALSE;
    }

    //::ZeroMemory(&sptwb, sizeof(SCSI_PASS_THROUGH_WITH_BUFFERS));
    sptwb.Spt.Length = sizeof(SCSI_PASS_THROUGH);
    sptwb.Spt.PathId = 0;
    sptwb.Spt.TargetId = 0;
    sptwb.Spt.Lun = 0;
    sptwb.Spt.CdbLength = 16;
    sptwb.Spt.SenseInfoLength = 32;
    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_IN;
    sptwb.Spt.DataTransferLength = 4096;
    sptwb.Spt.TimeOutValue = 2;
    sptwb.Spt.DataBufferOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS, DataBuf);
    sptwb.Spt.SenseInfoOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS, SenseBuf);

    sptwb.Spt.Cdb[0] = 0xE4; // NVME READ
    sptwb.Spt.Cdb[1] = BYTE(4096);
    sptwb.Spt.Cdb[2] = BYTE(4096 >> 8);
    sptwb.Spt.Cdb[3] = 0x06; // IDENTIFY
    sptwb.Spt.Cdb[4] = 0x01;

    length = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS, DataBuf) + sptwb.Spt.DataTransferLength;

    bRet = ::DeviceIoControl(hIoCtrl, IOCTL_SCSI_PASS_THROUGH,
                             &sptwb, length,
                             &sptwb, length, &dwReturned, NULL);

    if (bRet == FALSE)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    DWORD count = 0;
    for (int i = 0; i < 512; i++)
    {
        count += sptwb.DataBuf[i];
    }
    if (count == 0)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    memcpy_s(data, sizeof(NVME_IDENTIFY_DEVICE), sptwb.DataBuf, sizeof(NVME_IDENTIFY_DEVICE));

    safeCloseHandle(hIoCtrl);

    return TRUE;
}

/*---------------------------------------------------------------------------*/
//  NVMe SAMSUNG
/*---------------------------------------------------------------------------*/

HANDLE NVMeQuery::GetIoCtrlHandle(BYTE index)
{
    CString	strDevice;
    strDevice.Format(_T("\\\\.\\PhysicalDrive%d"), index);

    return ::CreateFile(strDevice, GENERIC_READ | GENERIC_WRITE,
                        FILE_SHARE_READ | FILE_SHARE_WRITE,
                        NULL, OPEN_EXISTING, 0, NULL);
}

BOOL NVMeQuery::DoIdentifyDeviceNVMeSamsung(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data)
{
    BOOL	bRet = FALSE;
    HANDLE	hIoCtrl = NULL;
    DWORD	dwReturned = 0;
    DWORD	length;

    SCSI_PASS_THROUGH_WITH_BUFFERS24 sptwb = {};

    if (data == NULL)
    {
        return	FALSE;
    }

    ::ZeroMemory(data, sizeof(NVME_IDENTIFY_DEVICE));

    hIoCtrl = GetIoCtrlHandle(physicalDriveId);

    if (! hIoCtrl || hIoCtrl == INVALID_HANDLE_VALUE)
    {
        return	FALSE;
    }

    //::ZeroMemory(&sptwb, sizeof(SCSI_PASS_THROUGH_WITH_BUFFERS24));
    sptwb.Spt.Length = sizeof(SCSI_PASS_THROUGH);
    sptwb.Spt.PathId = 0;
    sptwb.Spt.TargetId = 0;
    sptwb.Spt.Lun = 0;
    sptwb.Spt.SenseInfoLength = 24;
    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_IN;
    sptwb.Spt.DataTransferLength = 4096;
    sptwb.Spt.TimeOutValue = 2;
    sptwb.Spt.DataBufferOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, DataBuf);
    sptwb.Spt.SenseInfoOffset = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, SenseBuf);

    sptwb.Spt.CdbLength = 16;
    sptwb.Spt.Cdb[0] = 0xB5; // SECURITY PROTOCOL OUT
    sptwb.Spt.Cdb[1] = 0xFE; // SAMSUNG PROTOCOL
    sptwb.Spt.Cdb[2] = 0;
    sptwb.Spt.Cdb[3] = 5;
    sptwb.Spt.Cdb[4] = 0;
    sptwb.Spt.Cdb[5] = 0;
    sptwb.Spt.Cdb[6] = 0;
    sptwb.Spt.Cdb[7] = 0;
    sptwb.Spt.Cdb[8] = 0;
    sptwb.Spt.Cdb[9] = 0x40;
    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_OUT;
    sptwb.DataBuf[0] = 1;


    length = offsetof(SCSI_PASS_THROUGH_WITH_BUFFERS24, DataBuf) + sptwb.Spt.DataTransferLength;

    bRet = ::DeviceIoControl(hIoCtrl, IOCTL_SCSI_PASS_THROUGH,
                             &sptwb, length,
                             &sptwb, length, &dwReturned, NULL);

    if (bRet == FALSE)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    sptwb.Spt.CdbLength = 16;
    sptwb.Spt.Cdb[0] = 0xA2; // SECURITY PROTOCOL IN
    sptwb.Spt.Cdb[1] = 0xFE; // SAMSUNG PROTOCOL
    sptwb.Spt.Cdb[2] = 0;
    sptwb.Spt.Cdb[3] = 5;
    sptwb.Spt.Cdb[4] = 0;
    sptwb.Spt.Cdb[5] = 0;
    sptwb.Spt.Cdb[6] = 0;
    sptwb.Spt.Cdb[7] = 0;
    sptwb.Spt.Cdb[8] = 1;
    sptwb.Spt.Cdb[9] = 0;

    sptwb.Spt.DataIn = SCSI_IOCTL_DATA_IN;
    sptwb.DataBuf[0] = 0;

    bRet = ::DeviceIoControl(hIoCtrl, IOCTL_SCSI_PASS_THROUGH,
                             &sptwb, length,
                             &sptwb, length, &dwReturned, NULL);

    if (bRet == FALSE)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    DWORD count = 0;
    for (int i = 0; i < 512; i++)
    {
        count += sptwb.DataBuf[i];
    }
    if(count == 0)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    memcpy_s(data, sizeof(NVME_IDENTIFY_DEVICE), sptwb.DataBuf, sizeof(NVME_IDENTIFY_DEVICE));

    safeCloseHandle(hIoCtrl);

    return TRUE;
}

/*---------------------------------------------------------------------------*/
// NVMe Intel RST
/*---------------------------------------------------------------------------*/

BOOL NVMeQuery::GetScsiAddress(const TCHAR* Path, BYTE* PortNumber, BYTE* PathId, BYTE* TargetId, BYTE* Lun)
{
    HANDLE hDevice = CreateFile(Path,
                                GENERIC_READ | GENERIC_WRITE,
                                FILE_SHARE_READ | FILE_SHARE_WRITE,
                                nullptr,
                                OPEN_EXISTING,
                                FILE_ATTRIBUTE_NORMAL,
                                nullptr);

    printError("Query: ERROR creating handle to NVMe [%s]\n", Path);

    DWORD dwReturned = 0;
    SCSI_ADDRESS ScsiAddr = {};
    BOOL bRet = DeviceIoControl(hDevice, IOCTL_SCSI_GET_ADDRESS,
                                nullptr, 0, &ScsiAddr, sizeof(ScsiAddr), &dwReturned, NULL);

    safeCloseHandle(hDevice);

    *PortNumber = ScsiAddr.PortNumber;
    *PathId = ScsiAddr.PathId;
    *TargetId = ScsiAddr.TargetId;
    *Lun = ScsiAddr.Lun;

    return bRet == TRUE;
}

BOOL NVMeQuery::DoIdentifyDeviceNVMeIntelRst(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data, DWORD* diskSize)
{
    CString path;
    BYTE portNumber = 0, pathId = 0, targetId = 0, lun = 0;
    CString drive;

    if (physicalDriveId == -1)
    {
        portNumber = scsiPort;
        pathId = scsiTargetId;
    }
    else
    {
        path.Format("\\\\.\\PhysicalDrive%d", physicalDriveId);
        GetScsiAddress(path, &portNumber, &pathId, &targetId, &lun);
    }

    drive.Format("\\\\.\\Scsi%d:", portNumber);

    HANDLE hIoCtrl = CreateFile(drive,
                                GENERIC_READ | GENERIC_WRITE,
                                FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr,
                                OPEN_EXISTING, 0, nullptr);

    printError("Query: ERROR creating handle to NVMe [%d]\n", physicalDriveId);

    if (hIoCtrl != INVALID_HANDLE_VALUE)
    {
        INTEL_NVME_PASS_THROUGH NVMeData;
        memset(&NVMeData, 0, sizeof(NVMeData));

        NVMeData.SRB.HeaderLength = sizeof(SRB_IO_CONTROL);
        memcpy(NVMeData.SRB.Signature, "IntelNvm", 8);
        NVMeData.SRB.Timeout = 10;
        NVMeData.SRB.ControlCode = IOCTL_INTEL_NVME_PASS_THROUGH;
        NVMeData.SRB.Length = sizeof(INTEL_NVME_PASS_THROUGH) - sizeof(SRB_IO_CONTROL);

        NVMeData.Payload.Version = 1;
        NVMeData.Payload.PathId = pathId;
        NVMeData.Payload.Cmd.CDW0.Opcode = 0x06; // ADMIN_IDENTIFY
        NVMeData.Payload.Cmd.NSID = 1;
        NVMeData.Payload.Cmd.u.IDENTIFY.CDW10.CNS = 0;
        NVMeData.Payload.ParamBufLen = sizeof(INTEL_NVME_PAYLOAD) + sizeof(SRB_IO_CONTROL); //0xA4;
        NVMeData.Payload.ReturnBufferLen = 0x1000;
        NVMeData.Payload.CplEntry[0] = 0;

        DWORD dummy = 0;
        if (DeviceIoControl(hIoCtrl, IOCTL_SCSI_MINIPORT,
                            &NVMeData,
                            sizeof(NVMeData),
                            &NVMeData,
                            sizeof(NVMeData),
                            &dummy, nullptr))
        {
            ULONG64 totalLBA = *(ULONG64*)&NVMeData.DataBuffer[0];
            /*	 (((ULONG64)NVMeData.DataBuffer[7] << 56)
                + ((ULONG64)NVMeData.DataBuffer[6] << 48)
                + ((ULONG64)NVMeData.DataBuffer[5] << 40)
                + ((ULONG64)NVMeData.DataBuffer[4] << 32)
                + ((ULONG64)NVMeData.DataBuffer[3] << 24)
                + ((ULONG64)NVMeData.DataBuffer[2] << 16)
                + ((ULONG64)NVMeData.DataBuffer[1] << 8)
                + ((ULONG64)NVMeData.DataBuffer[0]));*/
            int sectorSize = 1 << NVMeData.DataBuffer[130];

            *diskSize = (DWORD)(totalLBA * sectorSize / 1000 / 1000);
        }

        NVMeData.Payload.Cmd.NSID = 0;
        NVMeData.Payload.Cmd.u.IDENTIFY.CDW10.CNS = 1;
        if (DeviceIoControl(hIoCtrl, IOCTL_SCSI_MINIPORT,
                            &NVMeData,
                            sizeof(NVMeData),
                            &NVMeData,
                            sizeof(NVMeData),
                            &dummy, nullptr))
        {
            memcpy_s(data, sizeof(NVME_IDENTIFY_DEVICE), NVMeData.DataBuffer, sizeof(NVME_IDENTIFY_DEVICE));

            safeCloseHandle(hIoCtrl);
            return TRUE;
        }

        safeCloseHandle(hIoCtrl);
    }
    return FALSE;
}

/*---------------------------------------------------------------------------*/
// NVMe Intel
// Reference: http://naraeon.net/en/archives/1126
/*---------------------------------------------------------------------------*/


CString NVMeQuery::GetScsiPath(const TCHAR* Path)
{
    HANDLE hIoCtrl = CreateFile(Path, GENERIC_READ | GENERIC_WRITE,
                                FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);

    printError("Query: ERROR creating handle to NVMe [%s]\n", Path);

    SCSI_ADDRESS sadr = {};
    BOOL bRet = 0;
    DWORD dwReturned;

    bRet = DeviceIoControl(hIoCtrl, IOCTL_SCSI_GET_ADDRESS,
                           NULL, 0, &sadr, sizeof(sadr), &dwReturned, NULL);

    CString result;
    result.Format("\\\\.\\SCSI%d:", sadr.PortNumber);

    safeCloseHandle(hIoCtrl);
    return result;
}

BOOL NVMeQuery::DoIdentifyDeviceNVMeIntel(INT physicalDriveId, INT scsiPort, INT scsiTargetId, NVME_IDENTIFY_DEVICE* data)
{
    CString path;
    path.Format("\\\\.\\PhysicalDrive%d", physicalDriveId);

    HANDLE hIoCtrl = CreateFile(GetScsiPath((TCHAR*)path.GetString()), GENERIC_READ | GENERIC_WRITE,
                                FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);

    printError("Query: ERROR creating handle to NVMe [%d]\n", physicalDriveId);

    BOOL bRet = 0;
    NVME_PASS_THROUGH_IOCTL nptwb = {};
    DWORD length = sizeof(nptwb);
    DWORD dwReturned;

    //ZeroMemory(&nptwb, sizeof(NVME_PASS_THROUGH_IOCTL));

    nptwb.SrbIoCtrl.ControlCode = NVME_PASS_THROUGH_SRB_IO_CODE;
    nptwb.SrbIoCtrl.HeaderLength = sizeof(SRB_IO_CONTROL);
    memcpy((UCHAR*)(&nptwb.SrbIoCtrl.Signature[0]), NVME_SIG_STR, NVME_SIG_STR_LEN);
    nptwb.SrbIoCtrl.Timeout = NVME_PT_TIMEOUT;
    nptwb.SrbIoCtrl.Length = length - sizeof(SRB_IO_CONTROL);
    nptwb.DataBufferLen = sizeof(nptwb.DataBuffer);
    nptwb.ReturnBufferLen = sizeof(nptwb);
    nptwb.Direction = NVME_FROM_DEV_TO_HOST;

    nptwb.NVMeCmd[0] = 6;  // Identify
    nptwb.NVMeCmd[10] = 1; // Return to Host

    bRet = DeviceIoControl(hIoCtrl, IOCTL_SCSI_MINIPORT,
                           &nptwb, length, &nptwb, length, &dwReturned, NULL);

    if (bRet == FALSE)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    DWORD count = 0;
    for (int i = 0; i < 512; i++)
    {
        count += nptwb.DataBuffer[i];
    }
    if (count == 0)
    {
        safeCloseHandle(hIoCtrl);
        return	FALSE;
    }

    memcpy_s(data, sizeof(NVME_IDENTIFY_DEVICE), nptwb.DataBuffer, sizeof(NVME_IDENTIFY_DEVICE));

    safeCloseHandle(hIoCtrl);
    return bRet;
}

template<class ...T>
auto NVMeQuery::LogMessage(T&&... args) -> void
{
    wchar_t updatedMessage[256];
    swprintf_s(updatedMessage, 256, std::forward<T>(args)...);
    //std::wcout << updatedMessage << std::endl;
    if (LogMessageChangedCallback != nullptr)
        LogMessageChangedCallback(updatedMessage);
}

NVMeQuery::~NVMeQuery() {

}

Pinvoke auto __stdcall New(MessageChangedCallback managedDelegate) -> NVMeQuery*
{
    return new NVMeQuery(managedDelegate);
}

Pinvoke auto GetSerialNumber(NVMeQuery* p, int physicalDriveId, char* serialNumber) -> void
{
    strcpy(serialNumber , (p->GetSerialNumber(physicalDriveId)).c_str());
}
