# Keypad driver using a PCF8574

This driver uses a [PCF8574 I/O port expander](http://www.ti.com/product/PCF8574) to drive/scan a keypad.
The number of rows and columns is user configurable, such as the I2C slave address of the PCF8574 and the GPIO used for the interrupt.

## Solutions

- [Keypad driver](Keypad-Driver.sln)
- [Keypad demo app](Keypad-test.sln)

## NuGet package

A NuGet package with the driver is available [here](https://www.nuget.org/packages/nanoFramework.Hardware.Drivers.Keypad-PCF8574).

## Demo app

A demo application is included to illustrate the configuration of the driver and handling the key pressed/released events.

Hardware used:

- nanoFramework board [PalThree](https://www.orgpal.com/palthree-iot-azure) from OrgPal.
- The PCF8574 used it's assembled in a module from TECNOIOT.
- The [4x4 Matrix Keypad](https://www.az-delivery.com/products/4x4-matrix-keypad?_pos=2&_sid=62d0a0321&_ss=r) is from AZ-Delivery.

Contributor: [OrgPal](https://www.orgpal.com)
