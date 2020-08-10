# MCP23017 I/O Expander Driver

A driver for controlling the MCP23017 16 Bit I/O Expander over I2C.

Datasheet: https://ww1.microchip.com/downloads/en/DeviceDoc/20001952C.pdf

## Usage

After creating an instance and initializing it, you can access it's I/O pins like standard GPIO pins.

The MCP23017 has to be initialized with at least one interrupt pin to be able to use the ValueChanged event.
When both interrupt pins are used, ValueChanged events are raised quicker.

## Pinout

![Pinout](pinout.png | width=300)

## Example Program

The included example demonstrates how access the IO pins and the use of the ValueChanged event.

![Breadboard](breadboard.png | width=800)
