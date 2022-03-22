[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=nanoframework_nanoFramework.Device.Bluetooth&metric=alert_status)](https://sonarcloud.io/dashboard?id=nanoframework_nanoFramework.Device.Bluetooth) [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=nanoframework_nanoFramework.Device.Bluetooth&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=nanoframework_nanoFramework.Device.Bluetooth) [![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![NuGet](https://img.shields.io/nuget/dt/nanoFramework.Device.Bluetooth.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.Device.Bluetooth/) [![#yourfirstpr](https://img.shields.io/badge/first--timers--only-friendly-blue.svg)](https://github.com/nanoframework/Home/blob/main/CONTRIBUTING.md) [![Discord](https://img.shields.io/discord/478725473862549535.svg?logo=discord&logoColor=white&label=Discord&color=7289DA)](https://discord.gg/gCyBu8T)

![nanoFramework logo](https://raw.githubusercontent.com/nanoframework/Home/main/resources/logo/nanoFramework-repo-logo.png)

-----

# Welcome to the .NET **nanoFramework** nanoFramework.Device.Bluetooth Library repository

## Build status

| Component | Build Status | NuGet Package |
|:-|---|---|
| nanoFramework.Device.Bluetooth | [![Build Status](https://dev.azure.com/nanoframework/nanoFramework.Device.Bluetooth/_apis/build/status/nanoframework.nanoFramework.Device.Bluetooth?repoName=nanoframework%2FnanoFramework.Device.Bluetooth&branchName=main)](https://dev.azure.com/nanoframework/nanoFramework.Device.Bluetooth/_build/latest?definitionId=85&repoName=nanoframework%2FnanoFramework.Device.Bluetooth&branchName=main) | [![NuGet](https://img.shields.io/nuget/v/nanoFramework.Device.Bluetooth.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.Device.Bluetooth/) |
| nanoFramework.Device.Bluetooth (preview) | [![Build Status](https://dev.azure.com/nanoframework/nanoFramework.Device.Bluetooth/_apis/build/status/nanoframework.nanoFramework.Device.Bluetooth?repoName=nanoframework%2FnanoFramework.Device.Bluetooth&branchName=develop)](https://dev.azure.com/nanoframework/nanoFramework.Device.Bluetooth/_build/latest?definitionId=85&repoName=nanoframework%2FnanoFramework.Device.Bluetooth&branchName=develop) | [![NuGet](https://img.shields.io/nuget/vpre/nanoFramework.Device.Bluetooth.svg?label=NuGet&style=flat&logo=nuget)](https://www.nuget.org/packages/nanoFramework.Device.Bluetooth/) |

## nanoFramework.Device.Bluetooth class Library

Bluetooth Low Energy library.

This library is based on the Windows.Devices.Bluetooth UWP class library but simplified and with all the async related calls removed.
The original .Net assembly depended on Windows.Storage.Streams for DataReader & DataWriter; this library has simplified inbuilt versions. References to IBuffer in .Net UWP examples should now use Buffer instead.

Currently only supported on ESP32 devices with following firmware.

- ESP32_BLE_REV0
- ESP32_BLE_REV3

This restriction is due to IRAM memory space in the firmware image. 
With revision 1 ESP32 devices the PSRAM implementation requires PSRAM fixes which takes space
in IRAM so PSRAM is currently disabled for ESP32_BLE_REV0.  With the revision 3 devices the Bluetooth and 
PSRAM and are both available.

## Samples

A number of Bluetooth LE samples are available in the [nanoFramework samples repo](https://github.com/nanoframework/Samples)

- Nordic Spp Sample. (coming soon)
- Environmental sensor sample. (coming soon)

## Usage

### Overview

The current implementation only supports the Gatt Server calls. 

Also as part of this assembly is the NordicSPP class which implements a Serial Protocol Profile based on 
the Nordic specification. This allows clients to easily connect via Bluetooth LE to send and receive messages via a 
Bluetooth Serial Terminal application. A common use case for provisioning devices.  See SPP section later for usage. 

### Attributes and UUIDs

Each service, characteristic and descriptor is defined by it's own unique 128-bit UUID. These are 
called GUID in this assembly. These are called UUID in the Bluetooth specifications. 

If the attribute is standard UUID defined by the Bluetooth SIG, it will also have a corresponding 16-bit short ID (for example, 
the characteristic **Battery Level** has a UUID of 00002A19-0000-1000-8000-00805F9B34FB and the short ID is 0x2A19). 
The common standard UUIDs can be seen in GattServiceUuids and GattCharacteristicUuids.

If the short ID is not present in GattServiceUuids or GattCharacteristicUuids then create your own short GUID by 
calling the utility function CreateUuidFromShortCode.

```csharp
Guid uuid1 = Utility.CreateUuidFromShortCode(0x2A19);
```

### Defining the service and associated Characteristics

The GattServiceProvider is used to create and advertise the primary service definition. An extra device information service will be automatically created.

```csharp
GattServiceProviderResult result = GattServiceProvider.Create(uuid);
if (result.Error != BluetoothError.Success)
{
    return result.Error;
}

serviceProvider = result.ServiceProvider;
```

Now add to the service all the required characteristics and descriptors. 
Currently only Read, Write, WriteWithoutResponse, Notify and Indicate characteristics are supported.

### Adding a Read Characteristic

If a userDescription is added to the GattLocalCharacteristicParameters then a user description descriptor will be automatically added to the Characteristic. 
For a read Characteristic you will need an associated event handler to provide the data for the read.

```csharp
GattLocalCharacteristicParameters ReadParameters = new GattLocalCharacteristicParameters
{
    CharacteristicProperties = (GattCharacteristicProperties.Read),
    UserDescription = "My Read Characteristic"
};

GattLocalCharacteristicResult characteristicResult = serviceProvider.Service.CreateCharacteristic(uuid1, ReadParameters);
if (characteristicResult.Error != BluetoothError.Success)
{
    // An error occurred.
    return characteristicResult.Error;
}

_readCharacteristic = characteristicResult.Characteristic;
_readCharacteristic.ReadRequested += _readCharacteristic_ReadRequested;
```

You can have a read Characteristics with a constant value by setting the **StaticValue** property.

```csharp
// Setting a Int 16 constant value to the characteristic. 
DataWriter dr = new DataWriter();
dr.WriteInt16(123);

GattLocalCharacteristicParameters ReadParameters = new GattLocalCharacteristicParameters
{
    CharacteristicProperties = (GattCharacteristicProperties.Read),
    UserDescription = "My Read Characteristic",
    StaticValue = dr.DetachBuffer()
};

```
If the **StaticValue** is set the the read event will not be called and doesn't need to be defined.

### Adding a Write or WriteWithoutResponse Characteristic

The Write Characteristic is used for receiving data from the client.  

```csharp
GattLocalCharacteristicParameters WriteParameters = new GattLocalCharacteristicParameters
{
    CharacteristicProperties = GattCharacteristicProperties.Write,
    UserDescription = "My Write Characteristic"
};


characteristicResult = serviceProvider.Service.CreateCharacteristic(uuid2, WriteParameters);
if (characteristicResult.Error != BluetoothError.Success)
{
    // An error occurred.
    return characteristicResult.Error;
}
_writeCharacteristic = characteristicResult.Characteristic;
_writeCharacteristic.WriteRequested += _writeCharacteristic_WriteRequested;
```

### Adding a Notify Characteristic

A notify Characteristic is used to automatically notify subscribed clients when a value has changed.

```csharp
GattLocalCharacteristicParameters NotifyParameters = new GattLocalCharacteristicParameters
{
    CharacteristicProperties = GattCharacteristicProperties.Notify,
    UserDescription = "My Notify Characteristic"
};

characteristicResult = serviceProvider.Service.CreateCharacteristic(uuid3, NotifyParameters);
if (characteristicResult.Error != BluetoothError.Success)
{
    // An error occurred.
    return characteristicResult.Error;
}

_notifyCharacteristic = characteristicResult.Characteristic;
_notifyCharacteristic.SubscribedClientsChanged += _notifyCharacteristic_SubscribedClientsChanged;
```

### Sending data to a Notify Characteristic

Data can be sent to subscribed clients by calling the NotifyValue method on the notify characteristic.
Extra checks can be added to only send values if there are subscribed clients or if the values has changed
since last notified.

```csharp
private static void UpdateNotifyValue(double newValue)
{
    DataWriter dw = new DataWriter();
    dw.WriteDouble(newValue);

    _notifyCharacteristic.NotifyValue(dw.DetachBuffer());
}
```

## Events

### Read requested event

When a client requests to read a characteristic, the managed event will be called assuming a static value hasn't been set.
If no event handler is set or you don't respond in a timely manner an Unlikely bluetooth error will be returned to client.  
If reading the value from a peripheral device takes time then best to put this outside the event handler.

This show the returning of 2 values to client request. 

```csharp
private static void _readCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs ReadRequestEventArgs)
{
    GattReadRequest request = ReadRequestEventArgs.GetRequest();

    // Create DataWriter and write the data into buffer
    DataWriter dw = new DataWriter();
    dw.WriteInt16(1);
    dw.WriteInt32(2);

    request.RespondWithValue(dw.DetachBuffer());

    // If there is some sort of error then response with an error 
    //request.RespondWithProtocolError((byte)BluetoothError.DeviceNotConnected);
}
```

## Write requested event

When data is sent to a write characteristic the managed event is called. If no event handler is 
set or you don't respond in a timely manner an Unlikely bluetooth error will be returned to client.

The data received is a array of bytes and this is formatted as required by characteristic. This could be a single
value of Int16, Int32, string etc. or it could be a number of different values.

This shows the reading of a single Int32 value from buffer and returns an error if the wrong number 
of bytes has been supplied.

```csharp
private static void _writeCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
{
    GattWriteRequest request = WriteRequestEventArgs.GetRequest();

    // Check expected data length
    if (request.Value.Length != 4)
    {
        request.RespondWithProtocolError((byte)BluetoothError.NotSupported);
        return;
    }

    // Read data from buffer of required format
    DataReader rdr = DataReader.FromBuffer(request.Value);
    Int32 data = rdr.ReadInt32();

    // Do something with received data
    Debug.WriteLine($"Rx data::{data}");

    // Respond if Write requires response
    if (request.Option == GattWriteOption.WriteWithResponse)
    {
        request.Respond();
    }
}
```

## Subscribed Clients changed event

For notify characteristics a client can subscribe to receive the notification values. When a client
subscribes the managed event will be called.
The SubscribedClients array of the characteristics contains the connected clients.

```csharp
private static void _notifyCharacteristic_SubscribedClientsChanged(GattLocalCharacteristic sender, object args)
{
    if ( sender.SubscribedClients.Length > 0)
    {
         Debug.WriteLine($"Client connected ");
    }
}
```

# Bluetooth Serial Port Profile(SPP)

This assembly has an implementation of the Nordic SPP which can easily be used to send messages between a Bluetooth client and the device 
running the SPP. This is a simple way of provisioning a device with any extra information like WiFi details.

There are a number of Android and IOS app that support Nordic SPP that can be used to send/receive messages.

## Create instance of the SPP

Create an instance of the SPP and provide event handlers for reading messages and client connection activity.
Start advertising with a device name.

Uses namespace **nanoFramework.Device.Bluetooth.Spp**

```csharp
NordicSpp spp = new NordicSpp();
spp.ReceivedData += Spp_ReceivedData;
spp.ConnectedEvent += Spp_ConnectedEvent;

spp.Start("MySpp");

```

When complete, call the Stop method to stop the SPP.

## Handling Read Data events

Data can be read as either a array of bytes or as a string.

```csharp
private void Spp_ReceivedData(IBluetoothSpp sender, SppReceivedDataEventArgs ReadDataEventArgs)
{
    string message = ReadDataEventArgs.DataString;

    // Do something with incoming message
    Debug.WriteLine($"Message:{message}");

    // For this example lets respond with "OK"
    NordicSpp spp = sender as NordicSpp;
    spp.SendString("OK");
}
```

## Handling connection events

A connection event is thrown when a client connects or disconnects from SPP server.
Here we send a message when a client connects. 

```csharp
private void Spp_ConnectedEvent(IBluetoothSpp sender, EventArgs e)
{
    NordicSpp spp = sender as NordicSpp;

    if (spp.IsConnected)
    {
        spp.SendString("Welcome to nanoFramework");
    }
}
```

## Feedback and documentation

For documentation, providing feedback, issues and finding out how to contribute please refer to the [Home repo](https://github.com/nanoframework/Home).

Join our Discord community [here](https://discord.gg/gCyBu8T).

## Credits

The list of contributors to this project can be found at [CONTRIBUTORS](https://github.com/nanoframework/Home/blob/main/CONTRIBUTORS.md).

## License

The **nanoFramework** Class Libraries are licensed under the [MIT license](LICENSE.md).

## Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behaviour in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

